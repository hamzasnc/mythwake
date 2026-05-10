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
	LastAFKClaimUTC   string              `json:"lastAfkClaimUtc,omitempty"`
	DailyDate         string              `json:"dailyDate,omitempty"`
	DailyProgress     []DailyProgress     `json:"dailyProgress"`
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

type DailyProgress struct {
	MissionID string `json:"missionId"`
	Progress  int    `json:"progress"`
	Target    int    `json:"target"`
	Claimed   bool   `json:"claimed"`
}

type Reward struct {
	RewardID    string `json:"rewardId"`
	Gold        int    `json:"gold"`
	Gems        int    `json:"gems"`
	MythEssence int    `json:"mythEssence"`
	PassXP      int    `json:"passXp"`
}

type CombatResult struct {
	Mode             string `json:"mode"`
	TargetID         string `json:"targetId"`
	TargetLevel      int    `json:"targetLevel"`
	Won              bool   `json:"won"`
	Rounds           int    `json:"rounds"`
	MaxRounds        int    `json:"maxRounds"`
	TeamAttack       int    `json:"teamAttack"`
	TeamMaxHP        int    `json:"teamMaxHp"`
	TeamHPRemaining  int    `json:"teamHpRemaining"`
	EnemyMaxHP       int    `json:"enemyMaxHp"`
	EnemyHPRemaining int    `json:"enemyHpRemaining"`
	EnemyDamage      int    `json:"enemyDamage"`
	DamageDealt      int    `json:"damageDealt"`
	DamageTaken      int    `json:"damageTaken"`
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
	Combat         *CombatResult  `json:"combat,omitempty"`
}

type GuestAuthResponse struct {
	PlayerID       string         `json:"playerId"`
	SessionToken   string         `json:"sessionToken"`
	PlayerState    PlayerState    `json:"playerState"`
	PlayerSnapshot PlayerSnapshot `json:"playerSnapshot"`
}

type ErrorResponse struct {
	ErrorCode string `json:"errorCode"`
	Message   string `json:"message"`
	RequestID string `json:"requestId,omitempty"`
}

type ServerClockResponse struct {
	ServerTimeUTC           string `json:"serverTimeUtc"`
	ServerUnixMs            int64  `json:"serverUnixMs"`
	DailyResetUTC           string `json:"dailyResetUtc"`
	WeeklyResetUTC          string `json:"weeklyResetUtc"`
	SecondsUntilDailyReset  int64  `json:"secondsUntilDailyReset"`
	SecondsUntilWeeklyReset int64  `json:"secondsUntilWeeklyReset"`
}

type AccessoryRequest struct {
	AccessoryID string `json:"accessoryId"`
}

type DefinitionSnapshot struct {
	SchemaVersion     int                          `json:"schemaVersion"`
	APIVersion        string                       `json:"apiVersion"`
	ContentHash       string                       `json:"contentHash"`
	AuthProviders     []AuthProviderDefinition     `json:"authProviders"`
	Currencies        []CurrencyDefinition         `json:"currencies"`
	Heroes            []HeroDefinition             `json:"heroes"`
	Rewards           []RewardDefinition           `json:"rewards"`
	Campaigns         []CampaignDefinition         `json:"campaigns"`
	CampaignStages    []CampaignStageDefinition    `json:"campaignStages"`
	Dungeons          []DungeonDefinition          `json:"dungeons"`
	AccessorySlots    []AccessorySlotDefinition    `json:"accessorySlots"`
	AccessoryRarities []AccessoryRarityDefinition  `json:"accessoryRarities"`
	Accessories       []AccessoryDefinition        `json:"accessories"`
	ProgressionCosts  []ProgressionCostDefinition  `json:"progressionCosts"`
	SummonBanners     []SummonBannerDefinition     `json:"summonBanners"`
	DailyMissions     []DailyMissionDefinition     `json:"dailyMissions"`
	BattlePassRewards []BattlePassRewardDefinition `json:"battlePassRewards"`
	GameplayActions   []GameplayActionDefinition   `json:"gameplayActions"`
}

type AuthProviderDefinition struct {
	ProviderID        string `json:"providerId"`
	DisplayName       string `json:"displayName"`
	ExternalProvider  bool   `json:"externalProvider"`
	SupportsLinking   bool   `json:"supportsLinking"`
	SupportsMobileSSO bool   `json:"supportsMobileSso"`
}

type CurrencyDefinition struct {
	CurrencyID  string `json:"currencyId"`
	DisplayName string `json:"displayName"`
	IsPremium   bool   `json:"isPremium"`
}

type HeroDefinition struct {
	HeroID       string `json:"heroId"`
	DisplayName  string `json:"displayName"`
	SortOrder    int    `json:"sortOrder"`
	StarterOwned bool   `json:"starterOwned"`
}

type RewardDefinition struct {
	RewardID    string `json:"rewardId"`
	DisplayName string `json:"displayName"`
	RewardType  string `json:"rewardType"`
	Reward      Reward `json:"reward"`
}

type CampaignDefinition struct {
	CampaignID                string `json:"campaignId"`
	DisplayName               string `json:"displayName"`
	BaseRequiredPower         int    `json:"baseRequiredPower"`
	RequiredPowerPerStage     int    `json:"requiredPowerPerStage"`
	BaseMythEssenceReward     int    `json:"baseMythEssenceReward"`
	MythEssenceRewardPerStage int    `json:"mythEssenceRewardPerStage"`
	MilestoneEveryStages      int    `json:"milestoneEveryStages"`
	MilestoneBaseGems         int    `json:"milestoneBaseGems"`
	MilestoneGemsPerStage     int    `json:"milestoneGemsPerStage"`
	MilestonePassXP           int    `json:"milestonePassXp"`
	EnemyBaseHP               int    `json:"enemyBaseHp"`
	EnemyHPPerPower           int    `json:"enemyHpPerPower"`
	EnemyHPPerStageSquared    int    `json:"enemyHpPerStageSquared"`
	EnemyBaseDamage           int    `json:"enemyBaseDamage"`
	EnemyDamagePerStage       int    `json:"enemyDamagePerStage"`
	EnemyDamagePowerDivisor   int    `json:"enemyDamagePowerDivisor"`
	MaxCombatRounds           int    `json:"maxCombatRounds"`
}

type CampaignStageDefinition struct {
	StageID         string `json:"stageId"`
	CampaignID      string `json:"campaignId"`
	StageNumber     int    `json:"stageNumber"`
	DisplayName     string `json:"displayName"`
	RequiredPower   int    `json:"requiredPower"`
	RewardID        string `json:"rewardId"`
	EnemyProfileID  string `json:"enemyProfileId"`
	EnemyMaxHP      int    `json:"enemyMaxHp"`
	EnemyDamage     int    `json:"enemyDamage"`
	MaxCombatRounds int    `json:"maxCombatRounds"`
}

type DungeonDefinition struct {
	DungeonID               string `json:"dungeonId"`
	DisplayName             string `json:"displayName"`
	RewardCurrencyID        string `json:"rewardCurrencyId,omitempty"`
	BaseRequiredPower       int    `json:"baseRequiredPower"`
	RequiredPowerPerFloor   int    `json:"requiredPowerPerFloor"`
	BaseRewardAmount        int    `json:"baseRewardAmount"`
	RewardPerFloor          int    `json:"rewardPerFloor"`
	EnemyBaseHP             int    `json:"enemyBaseHp"`
	EnemyHPPerPower         int    `json:"enemyHpPerPower"`
	EnemyHPPerFloor         int    `json:"enemyHpPerFloor"`
	EnemyBaseDamage         int    `json:"enemyBaseDamage"`
	EnemyDamagePerFloor     int    `json:"enemyDamagePerFloor"`
	EnemyDamagePowerDivisor int    `json:"enemyDamagePowerDivisor"`
	MaxCombatRounds         int    `json:"maxCombatRounds"`
}

type AccessorySlotDefinition struct {
	SlotID      string `json:"slotId"`
	DisplayName string `json:"displayName"`
	SortOrder   int    `json:"sortOrder"`
}

type AccessoryRarityDefinition struct {
	RarityID     string `json:"rarityId"`
	RarityIndex  int    `json:"rarityIndex"`
	DisplayName  string `json:"displayName"`
	MaxLevel     int    `json:"maxLevel"`
	FuseCopyCost int    `json:"fuseCopyCost"`
}

type AccessoryDefinition struct {
	AccessoryID    string `json:"accessoryId"`
	SlotID         string `json:"slotId"`
	RarityID       string `json:"rarityId"`
	AttackPerLevel int    `json:"attackPerLevel"`
	HealthPerLevel int    `json:"healthPerLevel"`
	DropWeight     int    `json:"dropWeight"`
	FuseTargetID   string `json:"fuseTargetId,omitempty"`
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
