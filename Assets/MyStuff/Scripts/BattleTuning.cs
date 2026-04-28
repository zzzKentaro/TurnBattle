using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// バトルのダメージ倍率やパラメータ調整用のScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "BattleTuning", menuName = "TurnBattle/Battle Tuning")]
    public class BattleTuning : ScriptableObject
    {
        [Header("Damage Random")]
        [SerializeField, Range(0.5f, 1.5f)] private float randomMin = 0.95f;
        [SerializeField, Range(0.5f, 1.5f)] private float randomMax = 1.05f;

        [Header("Critical")]
        [SerializeField, Min(1f)] private float criticalMultiplier = 1.5f;

        [Header("Order Bonus (additive attack coefficient)")]
        [SerializeField] private float firstOrderAttackCoefficientBonus = 0.00f;
        [SerializeField] private float secondOrderAttackCoefficientBonus = 0.05f;
        [SerializeField] private float thirdOrderAttackCoefficientBonus = 0.10f;

        [Header("Element: Same element stack damage bonus")]
        [SerializeField] private float damageBonusPerSameElementStack = 0.01f;

        [Header("Fire Stack -> DoT")]
        [SerializeField] private int fireDotBaseDamage = 10;
        [SerializeField] private int fireDotExtraDamagePerStackAfterFive = 5;

        [Header("Wind Stack -> Follow-up")]
        [SerializeField] private float windFollowUpBaseCoefficient = 0.20f;
        [SerializeField] private float windFollowUpExtraCoefficientPerStackAfterFive = 0.02f;

        [Header("Water Stack -> Attack coefficient down")]
        [SerializeField] private float waterAttackCoefficientPenaltyBase = 0.05f;
        [SerializeField] private float waterAttackCoefficientPenaltyPerStackAfterFive = 0.01f;

        [Header("Earth Stack -> Defense coefficient down")]
        [SerializeField] private float earthDefenseCoefficientPenaltyBase = 0.05f;
        [SerializeField] private float earthDefenseCoefficientPenaltyPerStackAfterFive = 0.01f;

        [Header("Thunder Stack -> Critical chance")]
        [SerializeField, Range(0f, 1f)] private float thunderCriticalChanceBase = 0.30f;
        [SerializeField, Range(0f, 1f)] private float thunderCriticalChancePerStackAfterFive = 0.05f;
        [SerializeField, Range(0f, 1f)] private float thunderCriticalChanceCap = 0.95f;

        public float RandomMin => randomMin;
        public float RandomMax => randomMax;
        public float CriticalMultiplier => criticalMultiplier;
        public float DamageBonusPerSameElementStack => damageBonusPerSameElementStack;

        public int GetFireDotDamage(int fireStacks)
        {
            if (fireStacks < 5)
            {
                return 0;
            }

            return fireDotBaseDamage + Mathf.Max(0, fireStacks - 5) * fireDotExtraDamagePerStackAfterFive;
        }

        public float GetWindFollowUpCoefficient(int windStacks)
        {
            if (windStacks < 5)
            {
                return 0f;
            }

            return windFollowUpBaseCoefficient + Mathf.Max(0, windStacks - 5) * windFollowUpExtraCoefficientPerStackAfterFive;
        }

        public float GetWaterAttackCoefficientPenalty(int waterStacks)
        {
            if (waterStacks < 5)
            {
                return 0f;
            }

            return waterAttackCoefficientPenaltyBase + Mathf.Max(0, waterStacks - 5) * waterAttackCoefficientPenaltyPerStackAfterFive;
        }

        public float GetEarthDefenseCoefficientPenalty(int earthStacks)
        {
            if (earthStacks < 5)
            {
                return 0f;
            }

            return earthDefenseCoefficientPenaltyBase + Mathf.Max(0, earthStacks - 5) * earthDefenseCoefficientPenaltyPerStackAfterFive;
        }

        public float GetThunderCriticalChance(int thunderStacks)
        {
            if (thunderStacks < 5)
            {
                return 0f;
            }

            float chance = thunderCriticalChanceBase + Mathf.Max(0, thunderStacks - 5) * thunderCriticalChancePerStackAfterFive;
            return Mathf.Clamp(chance, 0f, thunderCriticalChanceCap);
        }

        public float GetCommonOrderAttackCoefficientBonus(int orderIndex)
        {
            switch (orderIndex)
            {
                case 0:
                    return firstOrderAttackCoefficientBonus;
                case 1:
                    return secondOrderAttackCoefficientBonus;
                case 2:
                    return thirdOrderAttackCoefficientBonus;
                default:
                    return 0f;
            }
        }
    }
}
