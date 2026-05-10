package postgres

import (
	"context"
	"database/sql"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/definitions"
)

type DefinitionStore struct {
	db *sql.DB
}

func NewDefinitionStore(db *sql.DB) *DefinitionStore {
	return &DefinitionStore{db: db}
}

func (store *DefinitionStore) Snapshot(ctx context.Context, apiVersion string) (api.DefinitionSnapshot, error) {
	snapshot := definitions.Snapshot(apiVersion)
	var err error

	if snapshot.Currencies, err = store.currencyDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.Heroes, err = store.heroDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.Equipment, err = store.equipmentDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.Rewards, err = store.rewardDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.AFKRewards, err = store.afkRewardDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.Campaigns, err = store.campaignDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.CampaignStages, err = store.campaignStageDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.Dungeons, err = store.dungeonDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.AccessorySlots, err = store.accessorySlotDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.AccessoryRarities, err = store.accessoryRarityDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.Accessories, err = store.accessoryDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.ProgressionCosts, err = store.progressionCostDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.SummonBanners, err = store.summonBannerDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.DailyMissions, err = store.dailyMissionDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}
	if snapshot.BattlePassRewards, err = store.battlePassRewardDefinitions(ctx); err != nil {
		return api.DefinitionSnapshot{}, err
	}

	snapshot.ContentHash = definitions.ContentHash(snapshot)
	return snapshot, nil
}

func (store *DefinitionStore) currencyDefinitions(ctx context.Context) ([]api.CurrencyDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT id, display_name, is_premium
		FROM common.currency_definitions
		ORDER BY id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.CurrencyDefinition{}
	for rows.Next() {
		var definition api.CurrencyDefinition
		if err := rows.Scan(&definition.CurrencyID, &definition.DisplayName, &definition.IsPremium); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) heroDefinitions(ctx context.Context) ([]api.HeroDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			id,
			display_name,
			sort_order,
			starter_owned,
			max_level,
			max_ascension,
			base_attack,
			attack_per_level,
			attack_per_ascension,
			base_health,
			health_per_level,
			health_per_ascension
		FROM common.hero_definitions
		ORDER BY sort_order, id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.HeroDefinition{}
	for rows.Next() {
		var definition api.HeroDefinition
		if err := rows.Scan(
			&definition.HeroID,
			&definition.DisplayName,
			&definition.SortOrder,
			&definition.StarterOwned,
			&definition.MaxLevel,
			&definition.MaxAscension,
			&definition.BaseAttack,
			&definition.AttackPerLevel,
			&definition.AttackPerAscension,
			&definition.BaseHealth,
			&definition.HealthPerLevel,
			&definition.HealthPerAscension,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) equipmentDefinitions(ctx context.Context) ([]api.EquipmentDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			id,
			display_name,
			sort_order,
			starter_owned,
			max_level,
			attack_per_level,
			health_per_level
		FROM common.equipment_definitions
		ORDER BY sort_order, id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.EquipmentDefinition{}
	for rows.Next() {
		var definition api.EquipmentDefinition
		if err := rows.Scan(
			&definition.EquipmentID,
			&definition.DisplayName,
			&definition.SortOrder,
			&definition.StarterOwned,
			&definition.MaxLevel,
			&definition.AttackPerLevel,
			&definition.HealthPerLevel,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) rewardDefinitions(ctx context.Context) ([]api.RewardDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT id, display_name, reward_type, gold, gems, myth_essence, pass_xp
		FROM common.reward_definitions
		ORDER BY reward_type, id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.RewardDefinition{}
	for rows.Next() {
		var definition api.RewardDefinition
		if err := rows.Scan(
			&definition.RewardID,
			&definition.DisplayName,
			&definition.RewardType,
			&definition.Reward.Gold,
			&definition.Reward.Gems,
			&definition.Reward.MythEssence,
			&definition.Reward.PassXP,
		); err != nil {
			return nil, err
		}
		definition.Reward.RewardID = definition.RewardID
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) afkRewardDefinitions(ctx context.Context) ([]api.AFKRewardDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			id,
			reward_id,
			display_name,
			min_claim_seconds,
			max_claim_seconds,
			tick_seconds,
			base_myth_essence_per_tick,
			myth_essence_per_stage,
			gold_per_myth_essence_divisor
		FROM common.afk_reward_definitions
		WHERE active = true
		ORDER BY id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.AFKRewardDefinition{}
	for rows.Next() {
		var definition api.AFKRewardDefinition
		if err := rows.Scan(
			&definition.AFKRewardID,
			&definition.RewardID,
			&definition.DisplayName,
			&definition.MinClaimSeconds,
			&definition.MaxClaimSeconds,
			&definition.TickSeconds,
			&definition.BaseMythEssencePerTick,
			&definition.MythEssencePerStage,
			&definition.GoldPerMythEssenceDivisor,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) campaignDefinitions(ctx context.Context) ([]api.CampaignDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			id,
			display_name,
			base_required_power,
			required_power_per_stage,
			base_myth_essence_reward,
			myth_essence_reward_per_stage,
			milestone_every_stages,
			milestone_base_gems,
			milestone_gems_per_stage,
			milestone_pass_xp,
			enemy_base_hp,
			enemy_hp_per_power,
			enemy_hp_per_stage_squared,
			enemy_base_damage,
			enemy_damage_per_stage,
			enemy_damage_power_divisor,
			max_combat_seconds
		FROM common.campaign_curve_definitions
		ORDER BY id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.CampaignDefinition{}
	for rows.Next() {
		var definition api.CampaignDefinition
		if err := rows.Scan(
			&definition.CampaignID,
			&definition.DisplayName,
			&definition.BaseRequiredPower,
			&definition.RequiredPowerPerStage,
			&definition.BaseMythEssenceReward,
			&definition.MythEssenceRewardPerStage,
			&definition.MilestoneEveryStages,
			&definition.MilestoneBaseGems,
			&definition.MilestoneGemsPerStage,
			&definition.MilestonePassXP,
			&definition.EnemyBaseHP,
			&definition.EnemyHPPerPower,
			&definition.EnemyHPPerStageSquared,
			&definition.EnemyBaseDamage,
			&definition.EnemyDamagePerStage,
			&definition.EnemyDamagePowerDivisor,
			&definition.MaxCombatSeconds,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) campaignStageDefinitions(ctx context.Context) ([]api.CampaignStageDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			id,
			campaign_id,
			stage_number,
			display_name,
			required_power,
			reward_id,
			enemy_profile_id,
			enemy_max_hp,
			enemy_damage,
			max_combat_seconds
		FROM common.campaign_stage_definitions
		ORDER BY campaign_id, stage_number
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.CampaignStageDefinition{}
	for rows.Next() {
		var definition api.CampaignStageDefinition
		if err := rows.Scan(
			&definition.StageID,
			&definition.CampaignID,
			&definition.StageNumber,
			&definition.DisplayName,
			&definition.RequiredPower,
			&definition.RewardID,
			&definition.EnemyProfileID,
			&definition.EnemyMaxHP,
			&definition.EnemyDamage,
			&definition.MaxCombatSeconds,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) dungeonDefinitions(ctx context.Context) ([]api.DungeonDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			id,
			display_name,
			reward_currency_id,
			base_required_power,
			required_power_per_floor,
			base_reward_amount,
			reward_per_floor,
			enemy_base_hp,
			enemy_hp_per_power,
			enemy_hp_per_floor,
			enemy_base_damage,
			enemy_damage_per_floor,
			enemy_damage_power_divisor,
			max_combat_seconds
		FROM common.dungeon_definitions
		ORDER BY id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.DungeonDefinition{}
	for rows.Next() {
		var definition api.DungeonDefinition
		var rewardCurrencyID sql.NullString
		if err := rows.Scan(
			&definition.DungeonID,
			&definition.DisplayName,
			&rewardCurrencyID,
			&definition.BaseRequiredPower,
			&definition.RequiredPowerPerFloor,
			&definition.BaseRewardAmount,
			&definition.RewardPerFloor,
			&definition.EnemyBaseHP,
			&definition.EnemyHPPerPower,
			&definition.EnemyHPPerFloor,
			&definition.EnemyBaseDamage,
			&definition.EnemyDamagePerFloor,
			&definition.EnemyDamagePowerDivisor,
			&definition.MaxCombatSeconds,
		); err != nil {
			return nil, err
		}
		if rewardCurrencyID.Valid {
			definition.RewardCurrencyID = rewardCurrencyID.String
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) accessorySlotDefinitions(ctx context.Context) ([]api.AccessorySlotDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT id, display_name, sort_order
		FROM common.accessory_slot_definitions
		ORDER BY sort_order, id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.AccessorySlotDefinition{}
	for rows.Next() {
		var definition api.AccessorySlotDefinition
		if err := rows.Scan(&definition.SlotID, &definition.DisplayName, &definition.SortOrder); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) accessoryRarityDefinitions(ctx context.Context) ([]api.AccessoryRarityDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT id, rarity_index, display_name, max_level, fuse_copy_cost
		FROM common.accessory_rarity_definitions
		ORDER BY rarity_index, id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.AccessoryRarityDefinition{}
	for rows.Next() {
		var definition api.AccessoryRarityDefinition
		if err := rows.Scan(
			&definition.RarityID,
			&definition.RarityIndex,
			&definition.DisplayName,
			&definition.MaxLevel,
			&definition.FuseCopyCost,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) accessoryDefinitions(ctx context.Context) ([]api.AccessoryDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			accessory.id,
			accessory.slot_id,
			accessory.rarity_id,
			accessory.attack_per_level,
			accessory.health_per_level,
			accessory.drop_weight,
			accessory.fuse_target_id
		FROM common.accessory_definitions accessory
		JOIN common.accessory_slot_definitions slot ON slot.id = accessory.slot_id
		JOIN common.accessory_rarity_definitions rarity ON rarity.id = accessory.rarity_id
		ORDER BY slot.sort_order, rarity.rarity_index, accessory.id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.AccessoryDefinition{}
	for rows.Next() {
		var definition api.AccessoryDefinition
		var fuseTargetID sql.NullString
		if err := rows.Scan(
			&definition.AccessoryID,
			&definition.SlotID,
			&definition.RarityID,
			&definition.AttackPerLevel,
			&definition.HealthPerLevel,
			&definition.DropWeight,
			&fuseTargetID,
		); err != nil {
			return nil, err
		}
		if fuseTargetID.Valid {
			definition.FuseTargetID = fuseTargetID.String
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) progressionCostDefinitions(ctx context.Context) ([]api.ProgressionCostDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT id, domain, target_id, cost_currency_id, base_amount, amount_per_level, formula
		FROM common.progression_cost_definitions
		ORDER BY domain, target_id, id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.ProgressionCostDefinition{}
	for rows.Next() {
		var definition api.ProgressionCostDefinition
		if err := rows.Scan(
			&definition.CostID,
			&definition.Domain,
			&definition.TargetID,
			&definition.CostCurrencyID,
			&definition.BaseAmount,
			&definition.AmountPerLevel,
			&definition.Formula,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) summonBannerDefinitions(ctx context.Context) ([]api.SummonBannerDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT id, display_name, cost_currency_id, cost_amount, resolution_mode
		FROM common.summon_banner_definitions
		ORDER BY id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.SummonBannerDefinition{}
	for rows.Next() {
		var definition api.SummonBannerDefinition
		if err := rows.Scan(
			&definition.BannerID,
			&definition.DisplayName,
			&definition.CostCurrencyID,
			&definition.CostAmount,
			&definition.ResolutionMode,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}
	if err := rows.Err(); err != nil {
		return nil, err
	}

	drops, err := store.summonShardDrops(ctx)
	if err != nil {
		return nil, err
	}
	for index := range response {
		response[index].ShardDrops = drops[response[index].BannerID]
	}

	return response, nil
}

func (store *DefinitionStore) summonShardDrops(ctx context.Context) (map[string][]api.SummonShardDrop, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT banner_id, hero_id, shard_amount, reward_id
		FROM common.summon_pool_definitions
		ORDER BY banner_id, rotation_order
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := map[string][]api.SummonShardDrop{}
	for rows.Next() {
		var bannerID string
		var drop api.SummonShardDrop
		if err := rows.Scan(&bannerID, &drop.HeroID, &drop.Shards, &drop.RewardID); err != nil {
			return nil, err
		}
		response[bannerID] = append(response[bannerID], drop)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) dailyMissionDefinitions(ctx context.Context) ([]api.DailyMissionDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			mission.id,
			mission.display_name,
			mission.progress_type,
			mission.target,
			reward.id,
			reward.gold,
			reward.gems,
			reward.myth_essence,
			reward.pass_xp
		FROM common.mission_definitions mission
		JOIN common.reward_definitions reward ON reward.id = mission.reward_id
		WHERE mission.active = true
		ORDER BY mission.id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.DailyMissionDefinition{}
	for rows.Next() {
		var definition api.DailyMissionDefinition
		if err := rows.Scan(
			&definition.MissionID,
			&definition.DisplayName,
			&definition.ProgressType,
			&definition.Target,
			&definition.Reward.RewardID,
			&definition.Reward.Gold,
			&definition.Reward.Gems,
			&definition.Reward.MythEssence,
			&definition.Reward.PassXP,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}

func (store *DefinitionStore) battlePassRewardDefinitions(ctx context.Context) ([]api.BattlePassRewardDefinition, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT
			track.id,
			track.required_pass_xp,
			reward.id,
			reward.gold,
			reward.gems,
			reward.myth_essence,
			reward.pass_xp
		FROM common.battle_pass_track_definitions track
		JOIN common.reward_definitions reward ON reward.id = track.reward_id
		WHERE track.active = true
		ORDER BY track.sort_order, track.id
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	response := []api.BattlePassRewardDefinition{}
	for rows.Next() {
		var definition api.BattlePassRewardDefinition
		if err := rows.Scan(
			&definition.RewardID,
			&definition.RequiredPassXP,
			&definition.Reward.RewardID,
			&definition.Reward.Gold,
			&definition.Reward.Gems,
			&definition.Reward.MythEssence,
			&definition.Reward.PassXP,
		); err != nil {
			return nil, err
		}
		response = append(response, definition)
	}

	return response, rows.Err()
}
