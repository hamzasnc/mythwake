package apihttp

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"log"
	"net/http"
	"runtime/debug"
	"strings"
	"time"
)

type requestIDContextKey struct{}

type statusRecorder struct {
	http.ResponseWriter
	statusCode int
	bytes      int
}

func (recorder *statusRecorder) WriteHeader(statusCode int) {
	if recorder.statusCode != 0 {
		return
	}

	recorder.statusCode = statusCode
	recorder.ResponseWriter.WriteHeader(statusCode)
}

func (recorder *statusRecorder) Write(bytes []byte) (int, error) {
	if recorder.statusCode == 0 {
		recorder.statusCode = http.StatusOK
	}

	written, err := recorder.ResponseWriter.Write(bytes)
	recorder.bytes += written
	return written, err
}

func (recorder *statusRecorder) StatusCode() int {
	if recorder.statusCode == 0 {
		return http.StatusOK
	}

	return recorder.statusCode
}

func withRequestID(next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		requestID := strings.TrimSpace(request.Header.Get("X-Request-ID"))
		if !validRequestID(requestID) {
			requestID = newRequestID()
		}

		response.Header().Set("X-Request-ID", requestID)
		ctx := context.WithValue(request.Context(), requestIDContextKey{}, requestID)
		next.ServeHTTP(response, request.WithContext(ctx))
	})
}

func recoverPanic(logger *log.Logger, next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		defer func() {
			if recovered := recover(); recovered != nil {
				requestID := requestIDFromContext(request.Context())
				logger.Printf("panic request_id=%s method=%s path=%s error=%v stack=%s", requestID, request.Method, request.URL.Path, recovered, string(debug.Stack()))
				writeError(response, request, http.StatusInternalServerError, "internal_error", "Internal server error.")
			}
		}()

		next.ServeHTTP(response, request)
	})
}

func logRequests(logger *log.Logger, next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		startedAt := time.Now()
		recorder := &statusRecorder{ResponseWriter: response}
		next.ServeHTTP(recorder, request)
		logger.Printf(
			"request_id=%s method=%s path=%s status=%d bytes=%d duration=%s",
			requestIDFromContext(request.Context()),
			request.Method,
			request.URL.Path,
			recorder.StatusCode(),
			recorder.bytes,
			time.Since(startedAt),
		)
	})
}

func requestIDFromContext(ctx context.Context) string {
	requestID, ok := ctx.Value(requestIDContextKey{}).(string)
	if !ok || requestID == "" {
		return "unknown"
	}

	return requestID
}

func validRequestID(requestID string) bool {
	if len(requestID) < 8 || len(requestID) > 128 {
		return false
	}

	for _, char := range requestID {
		if char >= 'a' && char <= 'z' {
			continue
		}
		if char >= 'A' && char <= 'Z' {
			continue
		}
		if char >= '0' && char <= '9' {
			continue
		}
		if char == '.' || char == '_' || char == ':' || char == '-' {
			continue
		}
		return false
	}

	return true
}

func newRequestID() string {
	bytes := make([]byte, 12)
	if _, err := rand.Read(bytes); err != nil {
		return "req_fallback"
	}

	return "req_" + base64.RawURLEncoding.EncodeToString(bytes)
}
