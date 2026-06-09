using UnityEngine;

namespace TurnBasedBattle
{
    public class DamageCalculationInput
    {
        public BattleUnit caster;
        public BattleUnit target;
        public SpellData spellData;
        public float levelAttackCoefficientBonus;
        public float useCountAttackCoefficientBonus;
        public float carriedSequenceAttackCoefficientBonus;
        public float customAttackCoefficientBonus;
        public float customDefenseCoefficientBonus;
        public float finalDamageMultiplier = 1f;
        public int flatDamageBonus;
        public int orderIndex;
        public BattleTuning tuning;
    }

    public class DamageCalculationResult
    {
        public int damage;
        public bool isCritical;
        public float randomValue;
        public float finalAttackCoefficient;
        public float finalDefenseCoefficient;
        public float elementMultiplier;
        public int sourceAttackValue;
    }

    /// <summary>
    /// 魔法や属性スタックに応じた最終的なダメージ計算ロジックを提供する静的クラス。
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageCalculationResult Calculate(DamageCalculationInput input)
        {
            SpellData spell = input.spellData;
            BattleUnit caster = input.caster;
            BattleUnit target = input.target;
            BattleTuning tuning = input.tuning;

            int sourceAttack = spell.UseCurrentHpAsAttackSource ? caster.CurrentHP : caster.GetCurrentAttack();
            float missingHpRatio = 1f - ((float)caster.CurrentHP / Mathf.Max(1, caster.MaxHP));

            float attackCoefficient = spell.AttackCoefficient
                + input.levelAttackCoefficientBonus
                + input.useCountAttackCoefficientBonus
                + input.carriedSequenceAttackCoefficientBonus
                + input.customAttackCoefficientBonus
                + tuning.GetCommonOrderAttackCoefficientBonus(input.orderIndex)
                + spell.GetOrderSpecificAttackCoefficientBonus(input.orderIndex)
                + caster.GetAttackCoefficientModifierTotal()
                + (spell.MissingHpAttackCoefficientBonusAtZeroHp * missingHpRatio)
                - tuning.GetWaterAttackCoefficientPenalty(caster.GetElementStacks(ElementType.Water));

            attackCoefficient = Mathf.Max(0.01f, attackCoefficient);

            float defenseCoefficient = spell.DefenseCoefficient
                + input.customDefenseCoefficientBonus
                + target.GetDefenseCoefficientModifierTotal()
                - spell.DefensePenetration
                - tuning.GetEarthDefenseCoefficientPenalty(target.GetElementStacks(ElementType.Earth));

            defenseCoefficient = Mathf.Max(0f, defenseCoefficient);

            float elementMultiplier = 1f;
            if (spell.Element != ElementType.None)
            {
                elementMultiplier += target.GetElementStacks(spell.Element) * tuning.DamageBonusPerSameElementStack;
            }

            float critChance = spell.AllowCritical ? tuning.GetThunderCriticalChance(caster.GetElementStacks(ElementType.Thunder)) : 0f;
            bool critical = critChance > 0f && Random.value < critChance;
            float critMultiplier = critical ? tuning.CriticalMultiplier : 1f;

            float randomValue = Random.Range(tuning.RandomMin, tuning.RandomMax);
            float raw = (sourceAttack * attackCoefficient * elementMultiplier) - (target.GetCurrentDefense() * defenseCoefficient);
            float multiplier = Mathf.Max(0f, input.finalDamageMultiplier);
            int damage = Mathf.Max(1, Mathf.RoundToInt((raw * critMultiplier * randomValue * multiplier) + input.flatDamageBonus));

            return new DamageCalculationResult
            {
                damage = damage,
                isCritical = critical,
                randomValue = randomValue,
                finalAttackCoefficient = attackCoefficient,
                finalDefenseCoefficient = defenseCoefficient,
                elementMultiplier = elementMultiplier,
                sourceAttackValue = sourceAttack,
            };
        }
    }
}
