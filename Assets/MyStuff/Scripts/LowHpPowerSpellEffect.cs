using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "LowHpPowerSpellEffect", menuName = "TurnBattle/Spell Effects/Low HP Power")]
    public class LowHpPowerSpellEffect : SpellEffectBase
    {
        [SerializeField, Min(0f)] private float attackCoefficientBonusAtZeroHp = 1.0f;
        [SerializeField] private bool logWhenApplied = true;

        public override void BeforeDamageCalculation(SpellEffectContext context)
        {
            if (context.Caster == null)
            {
                return;
            }

            float hpRatio = (float)context.Caster.CurrentHP / Mathf.Max(1, context.Caster.MaxHP);
            float missingHpRatio = 1f - Mathf.Clamp01(hpRatio);
            float bonus = attackCoefficientBonusAtZeroHp * missingHpRatio;
            context.AttackCoefficientBonus += bonus;

            if (logWhenApplied && bonus > 0f && context.HitIndex == 0)
            {
                context.Log?.Invoke($"  HP低下により威力上昇: +{bonus:0.##}");
            }
        }
    }
}
