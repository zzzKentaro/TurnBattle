using System.Text;
using TMPro;
using UnityEngine;
using TurnBasedBattle;

public class BattleStatusWorldView : MonoBehaviour
{
    [Header("Units")]
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
}
