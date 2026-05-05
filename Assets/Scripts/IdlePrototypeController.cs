using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int gold;
    [SerializeField] private int damage = 1;
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private int enemyHp = 10;
    [SerializeField] private int enemyMaxHp = 10;
    [SerializeField] private int upgradeCost = 10;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button upgradeButton;

    private void Awake()
    {
        if (fightButton != null)
        {
            fightButton.onClick.AddListener(Fight);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeDamage);
        }

        RefreshUi();
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
    }

    public void Fight()
    {
        enemyHp -= damage;

        if (enemyHp <= 0)
        {
            gold += GetEnemyReward();
            enemyLevel++;
            enemyMaxHp = 10 + ((enemyLevel - 1) * 5);
            enemyHp = enemyMaxHp;
        }

        RefreshUi();
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

        RefreshUi();
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

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"Upgrade Damage ({upgradeCost} Gold)";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = gold >= upgradeCost;
        }
    }
}
