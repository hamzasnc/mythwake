package api

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"net/http"
	"regexp"
	"strings"
	"unicode/utf8"

	"mythwake/api/internal/auth"
	"mythwake/api/internal/rules"
	"mythwake/api/internal/store"
)

var (
	deviceIDPattern = regexp.MustCompile(`^[A-Za-z0-9_-]{16,128}$`)
	uuidPattern     = regexp.MustCompile(`^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$`)
)

// ------------------------------------------------------------------ auth

type deviceLoginRequest struct {
	DeviceID     string `json:"deviceId"`
	DeviceSecret string `json:"deviceSecret"`
}

type deviceLoginResponse struct {
	Token     string `json:"token"`
	AccountID string `json:"accountId"`
	Created   bool   `json:"created"`
}

// deviceLogin signs a device in, creating the account on its first login.
func (s *Server) deviceLogin(w http.ResponseWriter, r *http.Request) {
	var req deviceLoginRequest
	if err := readJSON(r, &req); err != nil {
		writeError(w, http.StatusBadRequest, "bad_request", err.Error())
		return
	}
	if !deviceIDPattern.MatchString(req.DeviceID) || len(req.DeviceSecret) < 24 || len(req.DeviceSecret) > 128 {
		writeError(w, http.StatusBadRequest, "bad_request", "deviceId (16-128 [A-Za-z0-9_-]) and deviceSecret (24-128 chars) required")
		return
	}
	ctx := r.Context()
	created := false
	device, err := s.store.Device(ctx, req.DeviceID)
	switch {
	case errors.Is(err, store.ErrNotFound):
		hash, err := auth.HashSecret(req.DeviceSecret)
		if err != nil {
			s.internal(w, err)
			return
		}
		device = store.Device{AccountID: newUUID(), SecretHash: hash}
		if err := s.store.CreateAccount(ctx, device.AccountID, req.DeviceID, hash); err != nil {
			s.internal(w, err)
			return
		}
		created = true
	case err != nil:
		s.internal(w, err)
		return
	default:
		if !auth.CheckSecret(device.SecretHash, req.DeviceSecret) {
			writeError(w, http.StatusUnauthorized, "unauthorized", "wrong device secret")
			return
		}
		_ = s.store.TouchDevice(ctx, req.DeviceID)
	}
	token, err := s.tokens.Issue(device.AccountID)
	if err != nil {
		s.internal(w, err)
		return
	}
	writeJSON(w, http.StatusOK, deviceLoginResponse{Token: token, AccountID: device.AccountID, Created: created})
}

// ------------------------------------------------------------------ player

type playerResponse struct {
	State          json.RawMessage `json:"state"`
	StateHash      string          `json:"stateHash"`
	Version        int64           `json:"version"`
	ContentVersion string          `json:"contentVersion"`
	DisplayName    string          `json:"displayName"`
	NowMs          int64           `json:"nowMs"`
}

func (s *Server) getPlayer(w http.ResponseWriter, r *http.Request) {
	p, err := s.loadOrCreatePlayer(r.Context(), accountID(r))
	if err != nil {
		s.internal(w, err)
		return
	}
	writeJSON(w, http.StatusOK, s.playerResponse(p))
}

func (s *Server) playerResponse(p *store.Player) playerResponse {
	return playerResponse{State: json.RawMessage(p.State), StateHash: p.StateHash, Version: p.Version,
		ContentVersion: p.ContentVersion, DisplayName: p.DisplayName, NowMs: s.nowMs()}
}

func (s *Server) loadOrCreatePlayer(ctx context.Context, account string) (*store.Player, error) {
	p, err := s.store.Player(ctx, account)
	if !errors.Is(err, store.ErrNotFound) {
		return p, err
	}
	content, err := s.rules.Health(ctx)
	if err != nil {
		return nil, err
	}
	created, err := s.rules.Create(ctx, randomUint64(), s.nowMs())
	if err != nil {
		return nil, err
	}
	p = &store.Player{AccountID: account, DisplayName: defaultName(account), State: created.State,
		StateHash: created.StateHash, Version: 1, ContentVersion: content}
	if err := s.store.InsertPlayer(ctx, p); err != nil {
		return nil, err
	}
	// a concurrent request may have created it first: always return what is stored
	return s.store.Player(ctx, account)
}

func defaultName(account string) string {
	return "Spieler-" + strings.ToUpper(strings.ReplaceAll(account, "-", "")[:6])
}

type nameRequest struct {
	Name string `json:"name"`
}

var namePattern = regexp.MustCompile(`^[\p{L}\p{N} _.-]+$`)

func (s *Server) setName(w http.ResponseWriter, r *http.Request) {
	var req nameRequest
	if err := readJSON(r, &req); err != nil {
		writeError(w, http.StatusBadRequest, "bad_request", err.Error())
		return
	}
	name := strings.TrimSpace(req.Name)
	if n := utf8.RuneCountInString(name); n < 3 || n > 20 || !namePattern.MatchString(name) {
		writeError(w, http.StatusBadRequest, "bad_name", "3-20 letters, digits, space, _ . -")
		return
	}
	if _, err := s.loadOrCreatePlayer(r.Context(), accountID(r)); err != nil {
		s.internal(w, err)
		return
	}
	err := s.store.WithPlayer(r.Context(), accountID(r), func(tx *store.Tx) error {
		tx.Player.DisplayName = name
		return tx.SavePlayer(r.Context())
	})
	if err != nil {
		s.internal(w, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]string{"displayName": name})
}

// ------------------------------------------------------------------ commands

type commandsRequest struct {
	BaseVersion int64           `json:"baseVersion"`
	Commands    []rules.Command `json:"commands"`
}

type commandsResponse struct {
	Version   int64  `json:"version"`
	StateHash string `json:"stateHash"`
	// Duplicate is true when every command was applied before (a retried request).
	Duplicate bool `json:"duplicate,omitempty"`
}

type commandsConflict struct {
	Error   string          `json:"error"`
	Index   int             `json:"index"`
	State   json.RawMessage `json:"state"`
	Version int64           `json:"version"`
	Hash    string          `json:"stateHash"`
}

const maxBatch = 50

// postCommands applies a batch of client commands with the server clock. The batch is all or nothing.
func (s *Server) postCommands(w http.ResponseWriter, r *http.Request) {
	var req commandsRequest
	if err := readJSON(r, &req); err != nil {
		writeError(w, http.StatusBadRequest, "bad_request", err.Error())
		return
	}
	if len(req.Commands) == 0 || len(req.Commands) > maxBatch {
		writeError(w, http.StatusBadRequest, "bad_request", "1-50 commands per request")
		return
	}
	ids := make([]string, len(req.Commands))
	for i, c := range req.Commands {
		if !uuidPattern.MatchString(c.ID) || c.Type == "" {
			writeError(w, http.StatusBadRequest, "bad_request", "every command needs a uuid id and a type")
			return
		}
		ids[i] = strings.ToLower(c.ID)
		req.Commands[i].ID = ids[i]
	}

	ctx := r.Context()
	account := accountID(r)
	if _, err := s.loadOrCreatePlayer(ctx, account); err != nil {
		s.internal(w, err)
		return
	}
	var resp commandsResponse
	var conflict *commandsConflict
	err := s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		p := tx.Player
		applied, err := tx.AppliedCommands(ctx, ids)
		if err != nil {
			return err
		}
		if applied == len(ids) {
			resp = commandsResponse{Version: p.Version, StateHash: p.StateHash, Duplicate: true}
			return nil
		}
		if applied > 0 || req.BaseVersion != p.Version {
			conflict = &commandsConflict{Error: "version_conflict", Index: -1, State: json.RawMessage(p.State), Version: p.Version, Hash: p.StateHash}
			return nil
		}
		result, err := s.rules.Apply(ctx, p.State, s.nowMs(), req.Commands, false)
		if re, ok := rules.IsRuleError(err); ok {
			conflict = &commandsConflict{Error: re.Code, Index: re.Index, State: json.RawMessage(p.State), Version: p.Version, Hash: p.StateHash}
			return nil
		}
		if err != nil {
			return err
		}
		first := p.Version + 1
		p.State, p.StateHash, p.Version = result.State, result.StateHash, p.Version+int64(len(req.Commands))
		logged := make([]store.LoggedCommand, len(req.Commands))
		for i, c := range req.Commands {
			logged[i] = store.LoggedCommand{ID: c.ID, Type: c.Type, Data: c.Data}
		}
		if err := tx.SavePlayer(ctx); err != nil {
			return err
		}
		if err := tx.LogCommands(ctx, logged, first); err != nil {
			return err
		}
		resp = commandsResponse{Version: p.Version, StateHash: p.StateHash}
		return nil
	})
	switch {
	case err != nil:
		s.internal(w, err)
	case conflict != nil && conflict.Error == "version_conflict":
		writeJSON(w, http.StatusConflict, conflict)
	case conflict != nil:
		writeJSON(w, http.StatusUnprocessableEntity, conflict)
	default:
		writeJSON(w, http.StatusOK, resp)
	}
}

func (s *Server) internal(w http.ResponseWriter, err error) {
	s.log.Error("internal error", "err", err)
	writeError(w, http.StatusInternalServerError, "internal", "internal error")
}

func newUUID() string {
	var b [16]byte
	_, _ = rand.Read(b[:])
	b[6] = b[6]&0x0f | 0x40 // version 4
	b[8] = b[8]&0x3f | 0x80 // RFC 4122 variant
	h := hex.EncodeToString(b[:])
	return h[0:8] + "-" + h[8:12] + "-" + h[12:16] + "-" + h[16:20] + "-" + h[20:32]
}
