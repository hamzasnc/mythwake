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

type Reward struct {
	RewardID    string `json:"rewardId"`
	Gold        int    `json:"gold"`
	Gems        int    `json:"gems"`
	MythEssence int    `json:"mythEssence"`
	PassXP      int    `json:"passXp"`
}

type ActionResult struct {
	Success     bool        `json:"success"`
	ActionID    string      `json:"actionId"`
	ErrorCode   string      `json:"errorCode,omitempty"`
	Message     string      `json:"message"`
	PlayerState PlayerState `json:"playerState"`
	Reward      Reward      `json:"reward"`
}
