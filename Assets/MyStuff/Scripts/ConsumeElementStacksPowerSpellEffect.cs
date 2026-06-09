using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "ConsumeElementStacksPowerSpellEffect", menuName = "TurnBattle/Spell Effects/Consume Element Stacks Power")]
    public class ConsumeElementStacksPowerSpellEffect : SpellEffectBase
    {
        [SerializeField] private ElementType consumedElement = ElementType.Earth;
        [SerializeField] private bool consumeFromTarget = true;
        [SerializeField, Min(0f)] private float attackCoefficientBonusPerStack = 0.2f;
        [SerializeField] private bool consumeOnlyOnce = true;

        private bool consumed;
        private int consumedStacks;

        public override void BeforeResolve(SpellEffectContext context)
        {
            consumed = false;
            consumedStacks = 0;
        }

        public override void BeforeDamageCalculation(SpellEffectContext context)
        {
            BattleUnit stackOwner = consumeFromTarget ? context.Target : context.Caster;
            if (stackOwner == null || consumedElement == ElementType.None)
            {
                return;
            }

            if (!consumed)
            {
                consumedStacks = stackOwner.GetElementStacks(consumedElement);
            }

            if (consumedStacks <= 0)
            {
                return;
            }

            context.AttackCoefficientBonus += consumedStacks * attackCoefficientBonusPerStack;

            if (!consumeOnlyOnce || !consumed)
            {
                stackOwner.ElementStacks.ClearStacks(consumedElement);
                consumed = true;
                context.Log?.Invoke($"  {consumedElement}スタックを{consumedStacks}消費して威力上昇");
            }
        }
    }
}
