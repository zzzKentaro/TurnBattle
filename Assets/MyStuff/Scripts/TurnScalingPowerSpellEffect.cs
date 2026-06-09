using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "TurnScalingPowerSpellEffect", menuName = "TurnBattle/Spell Effects/Turn Scaling Power")]
    public class TurnScalingPowerSpellEffect : SpellEffectBase
    {
        [SerializeField, Min(0f)] private float attackCoefficientBonusPerTurn = 0.15f;
        [SerializeField, Min(1)] private int countFromTurn = 1;
        [SerializeField, Min(0f)] private float maxBonus = 3f;

        public override void BeforeDamageCalculation(SpellEffectContext context)
        {
            int elapsedTurns = Mathf.Max(0, context.TurnNumber - countFromTurn);
            float bonus = Mathf.Min(maxBonus, elapsedTurns * attackCoefficientBonusPerTurn);
            context.AttackCoefficientBonus += bonus;

            if (bonus > 0f && context.HitIndex == 0)
            {
                context.Log?.Invoke($"  経過ターンにより威力上昇: +{bonus:0.##}");
            }
        }
    }
}
