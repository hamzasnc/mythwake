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

type DefinitionSnapshot struct {
	SchemaVersion     int                          `json:"schemaVersion"`
	APIVersion        string                       `json:"apiVersion"`
	ContentHash       string                       `json:"contentHash"`
	Dungeons          []DungeonDefinition          `json:"dungeons"`
	ProgressionCosts  []ProgressionCostDefinition  `json:"progressionCosts"`
	SummonBanners     []SummonBannerDefinition     `json:"summonBanners"`
	DailyMissions     []DailyMissionDefinition     `json:"dailyMissions"`
	BattlePassRewards []BattlePassRewardDefinition `json:"battlePassRewards"`
	GameplayActions   []GameplayActionDefinition   `json:"gameplayActions"`
}

type DungeonDefinition struct {
	DungeonID             string `json:"dungeonId"`
	DisplayName           string `json:"displayName"`
	RewardCurrencyID      string `json:"rewardCurrencyId,omitempty"`
	BaseRequiredPower     int    `json:"baseRequiredPower"`
	RequiredPowerPerFloor int    `json:"requiredPowerPerFloor"`
	BaseRewardAmount      int    `json:"baseRewardAmount"`
	RewardPerFloor        int    `json:"rewardPerFloor"`
}

type ProgressionCostDefinition struct {
	CostID         string `json:"costId"`
	Domain         string `json:"domain"`
	TargetID       string `json:"targetId"`
	CostCurrencyID string `json:"costCurrencyId"`
	BaseAmount     int    `json:"baseAmount"`
	AmountPerLevel int    `json:"amountPerLevel"`
	Formula        string `json:"formula"`
}

type SummonBannerDefinition struct {
	BannerID       string            `json:"bannerId"`
	DisplayName    string            `json:"displayName"`
	CostCurrencyID string            `json:"costCurrencyId"`
	CostAmount     int               `json:"costAmount"`
	ResolutionMode string            `json:"resolutionMode"`
	ShardDrops     []SummonShardDrop `json:"shardDrops"`
}

type SummonShardDrop struct {
	HeroID   string `json:"heroId"`
	Shards   int    `json:"shards"`
	RewardID string `json:"rewardId"`
}

type DailyMissionDefinition struct {
	MissionID    string `json:"missionId"`
	DisplayName  string `json:"displayName"`
	ProgressType string `json:"progressType"`
	Target       int    `json:"target"`
	Reward       Reward `json:"reward"`
}

type BattlePassRewardDefinition struct {
	RewardID       string `json:"rewardId"`
	RequiredPassXP int    `json:"requiredPassXp"`
	Reward         Reward `json:"reward"`
}

type GameplayActionDefinition struct {
	ActionID            string `json:"actionId"`
	Domain              string `json:"domain"`
	RequiresIdempotency bool   `json:"requiresIdempotency"`
	MaterializedByFlush bool   `json:"materializedByFlush"`
}
