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
                + tuning.GetCommonOrderAttackCoefficientBonus(input.orderIndex)
                + spell.GetOrderSpecificAttackCoefficientBonus(input.orderIndex)
                + caster.GetAttackCoefficientModifierTotal()
                + (spell.MissingHpAttackCoefficientBonusAtZeroHp * missingHpRatio)
                - tuning.GetWaterAttackCoefficientPenalty(caster.GetElementStacks(ElementType.Water));

            attackCoefficient = Mathf.Max(0.01f, attackCoefficient);

            float defenseCoefficient = spell.DefenseCoefficient
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
            int damage = Mathf.Max(1, Mathf.RoundToInt(raw * critMultiplier * randomValue));

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
