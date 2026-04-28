using System;
using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// プレイヤーが記憶（ラーニング）した魔法の状態やレベルを保持するクラス。
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

            switch (level)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    return 1;
                case 5:
                    return 20;
                default:
                    return int.MaxValue;
            }
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

            return Mathf.Max(0, level - 1) * spellData.LevelAttackCoefficientPerLevel;
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
