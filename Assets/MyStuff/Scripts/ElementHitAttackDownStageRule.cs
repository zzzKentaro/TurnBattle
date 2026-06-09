using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "ElementHitAttackDownStageRule", menuName = "TurnBattle/Stage Rules/Element Hit Attack Down")]
    public class ElementHitAttackDownStageRule : StageRuleBase
    {
        [SerializeField] private ElementType triggerElement = ElementType.Water;
        [SerializeField] private bool onlyPlayerAttacks = true;
        [SerializeField] private bool onlyWhenDamageDealt = true;
        [SerializeField] private int attackDelta = -1;
        [SerializeField, Min(1)] private int durationTurns = 1;

        public override void AfterSpellResolved(StageRuleContext context)
        {
            if (context == null || context.SpellData == null || context.Target == null)
            {
                return;
            }

            if (onlyPlayerAttacks && (context.Caster == null || !context.Caster.IsPlayer))
            {
                return;
            }

            if (context.SpellData.Element != triggerElement)
            {
                return;
            }

            if (onlyWhenDamageDealt && (context.Report == null || context.Report.totalDamage <= 0))
            {
                return;
            }

            context.Target.ApplyStatModifier(StatType.Attack, attackDelta, durationTurns);
            context.Log?.Invoke($"  ステージ効果: {triggerElement}属性により {context.Target.UnitName} の攻撃力が低下");
        }
    }
}
