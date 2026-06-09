using System;
using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// Stores the learned spell state, including level and use count.
    /// </summary>
    [Serializable]
    public class RememberedSpell
    {
        [SerializeField] private SpellData spellData;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int currentExpToNextLevel = 0;
        [SerializeField, Min(0)] private int totalUseCount = 0;

        public SpellData SpellData => spellData;
        public int Level => level;
        public int TotalUseCount => totalUseCount;

        public RememberedSpell(SpellData data)
        {
            spellData = data;
            level = 1;
            currentExpToNextLevel = 0;
            totalUseCount = 0;
        }

        public int GetRequiredUsesForNextLevel()
        {
            if (spellData == null || level >= spellData.MaxLevel)
            {
                return int.MaxValue;
            }

            return spellData.GetRequiredUsesForNextLevel(level);
        }

        public void RegisterUse()
        {
            GainPractice(1);
            totalUseCount++;
        }

        public void GainPractice(int amount)
        {
            if (spellData == null || level >= spellData.MaxLevel || amount <= 0)
            {
                return;
            }

            currentExpToNextLevel += amount;

            while (level < spellData.MaxLevel)
            {
                int need = GetRequiredUsesForNextLevel();
                if (currentExpToNextLevel < need)
                {
                    break;
                }

                currentExpToNextLevel -= need;
                level++;
            }
        }

        public float GetLevelAttackCoefficientBonus()
        {
            if (spellData == null)
            {
                return 0f;
            }

            return spellData.GetLevelAttackCoefficientBonus(level);
        }

        public float GetUseCountAttackCoefficientBonus()
        {
            if (spellData == null)
            {
                return 0f;
            }

            return spellData.GetUseCountAttackCoefficientBonus(totalUseCount);
        }
    }
}
