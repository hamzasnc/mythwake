package api

type PlayerState struct {
	SaveVersion         int `json:"saveVersion"`
	Gold                int `json:"gold"`
	Gems                int `json:"gems"`
	MythEssence         int `json:"mythEssence"`
	PassXP              int `json:"passXp"`
	CampaignStage       int `json:"campaignStage"`
	GoldDungeonFloor    int `json:"goldDungeonFloor"`
	EssenceDungeonFloor int `json:"essenceDungeonFloor"`
	GearDungeonFloor    int `json:"gearDungeonFloor"`
	TeamPower           int `json:"teamPower"`
	TeamAttack          int `json:"teamAttack"`
	TeamHealth          int `json:"teamHealth"`
}

type PlayerSnapshot struct {
	PlayerID          string              `json:"playerId"`
	State             PlayerState         `json:"state"`
	Heroes            []HeroState         `json:"heroes"`
	HeroShards        []HeroShardState    `json:"heroShards"`
	Equipment         []EquipmentState    `json:"equipment"`
	Accessories       []AccessoryState    `json:"accessories"`
	EquippedAccessory []EquippedAccessory `json:"equippedAccessories"`
	DailyClaims       []ClaimState        `json:"dailyClaims"`
	BattlePassClaims  []ClaimState        `json:"battlePassClaims"`
	SummonCount       int                 `json:"summonCount"`
}

type HeroState struct {
	HeroID    string `json:"heroId"`
	Level     int    `json:"level"`
	Ascension int    `json:"ascension"`
}

type HeroShardState struct {
	HeroID string `json:"heroId"`
	Shards int    `json:"shards"`
}

type EquipmentState struct {
	EquipmentID string `json:"equipmentId"`
	Level       int    `json:"level"`
}

type AccessoryState struct {
	AccessoryID string `json:"accessoryId"`
	Copies      int    `json:"copies"`
	Level       int    `json:"level"`
}

type EquippedAccessory struct {
	SlotID      string `json:"slotId"`
	AccessoryID string `json:"accessoryId"`
}

type ClaimState struct {
	ClaimID string `json:"claimId"`
	Claimed bool   `json:"claimed"`
}

type Reward struct {
	RewardID    string `json:"rewardId"`
	Gold        int    `json:"gold"`
	Gems        int    `json:"gems"`
	MythEssence int    `json:"mythEssence"`
	PassXP      int    `json:"passXp"`
}

type ActionResult struct {
	Success        bool           `json:"success"`
	ActionID       string         `json:"actionId"`
	IdempotencyKey string         `json:"idempotencyKey,omitempty"`
	Replay         bool           `json:"replay,omitempty"`
	ErrorCode      string         `json:"errorCode,omitempty"`
	Message        string         `json:"message"`
	PlayerState    PlayerState    `json:"playerState"`
	PlayerSnapshot PlayerSnapshot `json:"playerSnapshot"`
	Reward         Reward         `json:"reward"`
}

type GuestAuthResponse struct {
	PlayerID       string         `json:"playerId"`
	SessionToken   string         `json:"sessionToken"`
	PlayerState    PlayerState    `json:"playerState"`
	PlayerSnapshot PlayerSnapshot `json:"playerSnapshot"`
}

type AccessoryRequest struct {
	AccessoryID string `json:"accessoryId"`
}
