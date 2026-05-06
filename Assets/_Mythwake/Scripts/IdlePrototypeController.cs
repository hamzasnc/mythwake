using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour
{
    private enum AppScreen
    {
        Home,
        Battle,
        Heroes,
        Summon,
        Shop
    }

    private const string GoldKey = "Mythwake.Prototype.Gold";
    private const string DamageKey = "Mythwake.Prototype.Damage";
    private const string EnemyLevelKey = "Mythwake.Prototype.EnemyLevel";
    private const string EnemyHpKey = "Mythwake.Prototype.EnemyHp";
    private const string EnemyMaxHpKey = "Mythwake.Prototype.EnemyMaxHp";
    private const string UpgradeCostKey = "Mythwake.Prototype.UpgradeCost";
    private const string LastSeenUtcKey = "Mythwake.Prototype.LastSeenUtc";

    [Header("Stats")]
    [SerializeField] private int gold;
    [SerializeField] private int damage = 1;
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private int enemyHp = 10;
    [SerializeField] private int enemyMaxHp = 10;
    [SerializeField] private int upgradeCost = 10;

    [Header("Idle")]
    [SerializeField] private bool autoAttackEnabled = true;
    [SerializeField] private float autoAttackInterval = 1f;
    [SerializeField] private int maxOfflineSeconds = 8 * 60 * 60;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text homeGoldText;
    [SerializeField] private TMP_Text homeStageText;
    [SerializeField] private TMP_Text homePowerText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text autoAttackText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button resetButton;

    [Header("Navigation")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject heroesPanel;
    [SerializeField] private GameObject summonPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button battleTabButton;
    [SerializeField] private Button heroesTabButton;
    [SerializeField] private Button summonTabButton;
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Color activeTabColor = new Color(0.22f, 0.48f, 0.86f);
    [SerializeField] private Color inactiveTabColor = new Color(0.11f, 0.14f, 0.2f);

    private float autoAttackTimer;
    private int lastOfflineReward;
    private int lastOfflineSeconds;
    private AppScreen activeScreen = AppScreen.Home;

    private void Awake()
    {
        LoadProgress();
        ClaimOfflineRewards();
        RegisterNavigation();

        if (fightButton != null)
        {
            fightButton.onClick.AddListener(Fight);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeDamage);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetProgress);
        }

        RefreshUi();
        ShowScreen(activeScreen);
    }

    private void Update()
    {
        if (!autoAttackEnabled)
        {
            return;
        }

        autoAttackTimer += Time.deltaTime;

        if (autoAttackTimer < autoAttackInterval)
        {
            RefreshAutoAttackUi();
            return;
        }

        autoAttackTimer -= autoAttackInterval;
        Fight(saveProgress: true);
    }

    private void OnDestroy()
    {
        if (fightButton != null)
        {
            fightButton.onClick.RemoveListener(Fight);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(UpgradeDamage);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetProgress);
        }

        UnregisterNavigation();
    }

    public void ShowHome()
    {
        ShowScreen(AppScreen.Home);
    }

    public void ShowBattle()
    {
        ShowScreen(AppScreen.Battle);
    }

    public void ShowHeroes()
    {
        ShowScreen(AppScreen.Heroes);
    }

    public void ShowSummon()
    {
        ShowScreen(AppScreen.Summon);
    }

    public void ShowShop()
    {
        ShowScreen(AppScreen.Shop);
    }

    public void Fight()
    {
        Fight(saveProgress: true);
    }

    public void UpgradeDamage()
    {
        if (gold < upgradeCost)
        {
            RefreshUi();
            return;
        }

        gold -= upgradeCost;
        damage++;
        upgradeCost = Mathf.CeilToInt(upgradeCost * 1.35f);

        SaveProgress();
        RefreshUi();
    }

    public void ResetProgress()
    {
        gold = 0;
        damage = 1;
        enemyLevel = 1;
        enemyMaxHp = 10;
        enemyHp = enemyMaxHp;
        upgradeCost = 10;
        autoAttackTimer = 0f;
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;

        SaveProgress();
        RefreshUi();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            SaveProgress();
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void Fight(bool saveProgress)
    {
        enemyHp -= damage;

        if (enemyHp <= 0)
        {
            gold += GetEnemyReward();
            enemyLevel++;
            enemyMaxHp = 10 + ((enemyLevel - 1) * 5);
            enemyHp = enemyMaxHp;
        }

        if (saveProgress)
        {
            SaveProgress();
        }

        RefreshUi();
    }

    private void LoadProgress()
    {
        gold = PlayerPrefs.GetInt(GoldKey, gold);
        damage = Mathf.Max(1, PlayerPrefs.GetInt(DamageKey, damage));
        enemyLevel = Mathf.Max(1, PlayerPrefs.GetInt(EnemyLevelKey, enemyLevel));
        enemyMaxHp = Mathf.Max(1, PlayerPrefs.GetInt(EnemyMaxHpKey, enemyMaxHp));
        enemyHp = Mathf.Clamp(PlayerPrefs.GetInt(EnemyHpKey, enemyHp), 1, enemyMaxHp);
        upgradeCost = Mathf.Max(1, PlayerPrefs.GetInt(UpgradeCostKey, upgradeCost));
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(GoldKey, gold);
        PlayerPrefs.SetInt(DamageKey, damage);
        PlayerPrefs.SetInt(EnemyLevelKey, enemyLevel);
        PlayerPrefs.SetInt(EnemyHpKey, enemyHp);
        PlayerPrefs.SetInt(EnemyMaxHpKey, enemyMaxHp);
        PlayerPrefs.SetInt(UpgradeCostKey, upgradeCost);
        PlayerPrefs.SetString(LastSeenUtcKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private void ClaimOfflineRewards()
    {
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;

        var rawTicks = PlayerPrefs.GetString(LastSeenUtcKey, string.Empty);
        if (!long.TryParse(rawTicks, out var ticks))
        {
            SaveProgress();
            return;
        }

        var lastSeenUtc = new DateTime(ticks, DateTimeKind.Utc);
        var elapsedSeconds = Mathf.FloorToInt((float)(DateTime.UtcNow - lastSeenUtc).TotalSeconds);
        lastOfflineSeconds = Mathf.Clamp(elapsedSeconds, 0, maxOfflineSeconds);

        if (lastOfflineSeconds <= 0)
        {
            SaveProgress();
            return;
        }

        lastOfflineReward = CalculateOfflineReward(lastOfflineSeconds);
        gold += lastOfflineReward;
        SaveProgress();
    }

    private int CalculateOfflineReward(int offlineSeconds)
    {
        var attacks = Mathf.FloorToInt(offlineSeconds / Mathf.Max(0.1f, autoAttackInterval));
        var enemyClearSeconds = Mathf.Max(1, Mathf.CeilToInt(enemyMaxHp / (float)Mathf.Max(1, damage)));
        var enemyKills = Mathf.Max(0, attacks / enemyClearSeconds);

        return enemyKills * GetEnemyReward();
    }

    private int GetEnemyReward()
    {
        return 5 + (enemyLevel * 2);
    }

    private void RefreshUi()
    {
        if (titleText != null)
        {
            titleText.text = "Mythwake";
        }

        if (goldText != null)
        {
            goldText.text = $"Gold: {gold}";
        }

        if (homeGoldText != null)
        {
            homeGoldText.text = $"{gold} Gold";
        }

        if (homeStageText != null)
        {
            homeStageText.text = $"Campaign {enemyLevel}";
        }

        if (homePowerText != null)
        {
            homePowerText.text = $"Power {GetPrototypePower()}";
        }

        if (damageText != null)
        {
            damageText.text = $"Damage: {damage}";
        }

        if (enemyText != null)
        {
            enemyText.text = $"Enemy Lv. {enemyLevel}";
        }

        if (enemyHpText != null)
        {
            enemyHpText.text = $"HP: {Mathf.Max(enemyHp, 0)} / {enemyMaxHp}";
        }

        RefreshAutoAttackUi();
        RefreshOfflineRewardUi();

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"Upgrade Damage ({upgradeCost} Gold)";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = gold >= upgradeCost;
        }
    }

    private void RegisterNavigation()
    {
        if (homeTabButton != null)
        {
            homeTabButton.onClick.AddListener(ShowHome);
        }

        if (battleTabButton != null)
        {
            battleTabButton.onClick.AddListener(ShowBattle);
        }

        if (heroesTabButton != null)
        {
            heroesTabButton.onClick.AddListener(ShowHeroes);
        }

        if (summonTabButton != null)
        {
            summonTabButton.onClick.AddListener(ShowSummon);
        }

        if (shopTabButton != null)
        {
            shopTabButton.onClick.AddListener(ShowShop);
        }
    }

    private void UnregisterNavigation()
    {
        if (homeTabButton != null)
        {
            homeTabButton.onClick.RemoveListener(ShowHome);
        }

        if (battleTabButton != null)
        {
            battleTabButton.onClick.RemoveListener(ShowBattle);
        }

        if (heroesTabButton != null)
        {
            heroesTabButton.onClick.RemoveListener(ShowHeroes);
        }

        if (summonTabButton != null)
        {
            summonTabButton.onClick.RemoveListener(ShowSummon);
        }

        if (shopTabButton != null)
        {
            shopTabButton.onClick.RemoveListener(ShowShop);
        }
    }

    private void ShowScreen(AppScreen screen)
    {
        activeScreen = screen;

        SetPanel(homePanel, screen == AppScreen.Home);
        SetPanel(battlePanel, screen == AppScreen.Battle);
        SetPanel(heroesPanel, screen == AppScreen.Heroes);
        SetPanel(summonPanel, screen == AppScreen.Summon);
        SetPanel(shopPanel, screen == AppScreen.Shop);

        SetTabState(homeTabButton, screen == AppScreen.Home);
        SetTabState(battleTabButton, screen == AppScreen.Battle);
        SetTabState(heroesTabButton, screen == AppScreen.Heroes);
        SetTabState(summonTabButton, screen == AppScreen.Summon);
        SetTabState(shopTabButton, screen == AppScreen.Shop);
    }

    private void SetPanel(GameObject panel, bool isVisible)
    {
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }

    private void SetTabState(Button button, bool isActive)
    {
        if (button == null || button.targetGraphic == null)
        {
            return;
        }

        button.targetGraphic.color = isActive ? activeTabColor : inactiveTabColor;
    }

    private int GetPrototypePower()
    {
        return (damage * 10) + (enemyLevel * 4);
    }

    private void RefreshAutoAttackUi()
    {
        if (autoAttackText == null)
        {
            return;
        }

        if (!autoAttackEnabled)
        {
            autoAttackText.text = "Auto Attack: Off";
            return;
        }

        var remaining = Mathf.Max(0f, autoAttackInterval - autoAttackTimer);
        autoAttackText.text = $"Auto Attack: {remaining:0.0}s";
    }

    private void RefreshOfflineRewardUi()
    {
        if (offlineRewardText == null)
        {
            return;
        }

        if (lastOfflineReward <= 0)
        {
            offlineRewardText.text = "Offline: no reward yet";
            return;
        }

        offlineRewardText.text = $"Offline: +{lastOfflineReward} Gold ({FormatDuration(lastOfflineSeconds)})";
    }

    private string FormatDuration(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }

        if (time.TotalMinutes >= 1)
        {
            return $"{time.Minutes}m {time.Seconds}s";
        }

        return $"{time.Seconds}s";
    }
}
