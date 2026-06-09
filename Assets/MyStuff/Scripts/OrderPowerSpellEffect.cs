using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "OrderPowerSpellEffect", menuName = "TurnBattle/Spell Effects/Order Power")]
    public class OrderPowerSpellEffect : SpellEffectBase
    {
        [SerializeField, Range(1, 3)] private int requiredOrder = 1;
        [SerializeField, Min(0f)] private float attackCoefficientBonus = 0.75f;

        public override void BeforeDamageCalculation(SpellEffectContext context)
        {
            if (context.Action == null || context.Action.OrderIndex != requiredOrder - 1)
            {
                return;
            }

            context.AttackCoefficientBonus += attackCoefficientBonus;
            if (context.HitIndex == 0)
            {
                context.Log?.Invoke($"  {requiredOrder}番目の発動により威力上昇");
            }
        }
    }
}
