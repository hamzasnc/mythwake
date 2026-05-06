using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour
{
    private const string GoldKey = "Mythwake.Prototype.Gold";
    private const string DamageKey = "Mythwake.Prototype.Damage";
    private const string EnemyLevelKey = "Mythwake.Prototype.EnemyLevel";
    private const string EnemyHpKey = "Mythwake.Prototype.EnemyHp";
    private const string EnemyMaxHpKey = "Mythwake.Prototype.EnemyMaxHp";
    private const string UpgradeCostKey = "Mythwake.Prototype.UpgradeCost";

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

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text autoAttackText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button resetButton;

    private float autoAttackTimer;

    private void Awake()
    {
        LoadProgress();

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
        PlayerPrefs.Save();
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

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"Upgrade Damage ({upgradeCost} Gold)";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = gold >= upgradeCost;
        }
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
}
