using System.Text;
using TMPro;
using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// ワールド空間におけるユニットステータスの表示を制御する。
/// </summary>
public class BattleStatusWorldView : MonoBehaviour
{
    [Header("Units")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI playerMpText;
    [SerializeField] private TextMeshProUGUI enemyHpText;
    [SerializeField] private TextMeshProUGUI playerDetailText;
    [SerializeField] private TextMeshProUGUI enemyDetailText;
    [SerializeField] private bool showPlayerStacksInDetailText = false;
    [SerializeField] private bool showEnemyStacksInDetailText = false;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<BattleManager>();
        }
    }

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.OnEnemyUnitChanged += HandleEnemyUnitChanged;
            HandleEnemyUnitChanged(battleManager.CurrentEnemyUnit);
        }
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnEnemyUnitChanged -= HandleEnemyUnitChanged;
        }
    }

    private void Update()
    {
        if (playerUnit != null)
        {
            if (playerHpText != null)
            {
                playerHpText.text = $"{playerUnit.CurrentHP} / {playerUnit.MaxHP}";
            }

            if (playerMpText != null)
            {
                playerMpText.text = $"{playerUnit.CurrentMP} / {playerUnit.MaxMP}";
            }

            if (playerDetailText != null)
            {
                playerDetailText.text = BuildDetailText(playerUnit, true, showPlayerStacksInDetailText);
            }
        }

        if (enemyUnit != null)
        {
            if (enemyHpText != null)
            {
                enemyHpText.text = $"{enemyUnit.CurrentHP} / {enemyUnit.MaxHP}";
            }

            if (enemyDetailText != null)
            {
                enemyDetailText.text = BuildDetailText(enemyUnit, false, showEnemyStacksInDetailText);
            }
        }
    }

    private string BuildDetailText(BattleUnit unit, bool showMp, bool showStacks)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(unit.UnitName);
        sb.AppendLine($"ATK {unit.GetCurrentAttack()}");
        sb.AppendLine($"DEF {unit.GetCurrentDefense()}");

        if (showMp)
        {
            sb.AppendLine($"MP {unit.CurrentMP}/{unit.MaxMP}");
        }

        if (showStacks)
        {
            sb.AppendLine(
                $"Stack F:{unit.GetElementStacks(ElementType.Fire)} " +
                $"W:{unit.GetElementStacks(ElementType.Water)} " +
                $"A:{unit.GetElementStacks(ElementType.Wind)} " +
                $"E:{unit.GetElementStacks(ElementType.Earth)} " +
                $"T:{unit.GetElementStacks(ElementType.Thunder)}");
        }

        sb.AppendLine(
            $"Coef ATK:{unit.GetAttackCoefficientModifierTotal():+0.00;-0.00;0.00} " +
            $"DEF:{unit.GetDefenseCoefficientModifierTotal():+0.00;-0.00;0.00}");

        return sb.ToString();
    }

    private void HandleEnemyUnitChanged(BattleUnit nextEnemyUnit)
    {
        if (nextEnemyUnit != null)
        {
            enemyUnit = nextEnemyUnit;
            TextMeshProUGUI nextEnemyHpText = FindEnemyHpText(nextEnemyUnit);
            if (nextEnemyHpText != null)
            {
                enemyHpText = nextEnemyHpText;
            }
        }
    }

    private TextMeshProUGUI FindEnemyHpText(BattleUnit unit)
    {
        TextMeshProUGUI[] texts = unit.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].text.Contains("/"))
            {
                return texts[i];
            }
        }

        return null;
    }
}
