using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// 敵の行動（最大3つの魔法アクション）を決定するAIロジック。
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private EnemyAIProfile profile;

        private readonly HashSet<SpellData> usedOnceSpells = new HashSet<SpellData>();

        public void SetProfile(EnemyAIProfile nextProfile)
        {
            profile = nextProfile;
            ResetBattleState();
        }

        public void ResetBattleState()
        {
            usedOnceSpells.Clear();
        }

        public List<SpellAction> BuildTurnActions(BattleUnit enemy, BattleUnit target)
        {
            return BuildTurnActions(enemy, target, 1);
        }

        public List<SpellAction> BuildTurnActions(BattleUnit enemy, BattleUnit target, int turnNumber)
        {
            List<SpellAction> actions = new List<SpellAction>();
            if (enemy == null || target == null)
            {
                return actions;
            }

            List<SpellData> pool = new List<SpellData>(enemy.EnemySpellPool);
            AddProfileSpellsToPool(pool);
            if (pool.Count == 0)
            {
                return actions;
            }

            SpellData previous = null;
            int reservedMp = 0;

            for (int orderIndex = 0; orderIndex < 3; orderIndex++)
            {
                int availableMp = Mathf.Max(0, enemy.CurrentMP - reservedMp);
                SpellData chosen = profile != null
                    ? ChooseSpellFromProfile(enemy, target, orderIndex, previous, availableMp, turnNumber)
                    : ChooseSpell(pool, enemy, target, orderIndex, previous, availableMp);
                if (chosen == null)
                {
                    break;
                }

                BattleUnit actualTarget = chosen.TargetType == TargetType.Self ? enemy : target;
                actions.Add(new SpellAction(enemy, actualTarget, chosen, orderIndex));

                reservedMp += Mathf.Max(0, chosen.MpCost);
                previous = chosen;
                usedOnceSpells.Add(chosen);
            }

            return actions;
        }

        private SpellData ChooseSpellFromProfile(BattleUnit enemy, BattleUnit target, int orderIndex, SpellData previous, int availableMp, int turnNumber)
        {
            if (profile == null || profile.Spells == null || profile.Spells.Count == 0)
            {
                return null;
            }

            SpellData forced = FindForcedSpell(orderIndex, availableMp, turnNumber);
            if (forced != null)
            {
                return forced;
            }

            float totalWeight = 0f;
            List<float> weights = new List<float>(profile.Spells.Count);

            for (int i = 0; i < profile.Spells.Count; i++)
            {
                EnemySpellEntry entry = profile.Spells[i];
                SpellData spell = entry != null ? entry.spell : null;
                float weight = GetProfileWeight(entry, spell, enemy, target, orderIndex, previous, availableMp, turnNumber);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                SpellData fallback = profile.FallbackSpell;
                if (fallback != null && fallback.IsValidForOrder(orderIndex) && fallback.MpCost <= availableMp)
                {
                    return fallback;
                }

                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < profile.Spells.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return profile.Spells[i].spell;
                }
            }

            return null;
        }

        private SpellData FindForcedSpell(int orderIndex, int availableMp, int turnNumber)
        {
            for (int i = 0; i < profile.Spells.Count; i++)
            {
                EnemySpellEntry entry = profile.Spells[i];
                SpellData spell = entry != null ? entry.spell : null;
                if (spell == null || !spell.IsValidForOrder(orderIndex) || spell.MpCost > availableMp)
                {
                    continue;
                }

                if (entry.onlyOnce && usedOnceSpells.Contains(spell))
                {
                    continue;
                }

                if ((entry.forceOnFirstTurn && turnNumber == 1) ||
                    (entry.forceOnTurn > 0 && turnNumber >= entry.forceOnTurn))
                {
                    return spell;
                }
            }

            return null;
        }

        private float GetProfileWeight(
            EnemySpellEntry entry,
            SpellData spell,
            BattleUnit enemy,
            BattleUnit target,
            int orderIndex,
            SpellData previous,
            int availableMp,
            int turnNumber)
        {
            if (entry == null || spell == null || !spell.IsValidForOrder(orderIndex) || spell.MpCost > availableMp)
            {
                return 0f;
            }

            if (entry.earliestTurn > 0 && turnNumber < entry.earliestTurn)
            {
                return 0f;
            }

            if (entry.onlyOnce && usedOnceSpells.Contains(spell))
            {
                return 0f;
            }

            float weight = GetFrequencyWeight(entry.frequency);
            weight *= spell.GetOrderPreferenceWeight(orderIndex);

            float hpRatio = (float)enemy.CurrentHP / Mathf.Max(1, enemy.MaxHP);
            if (hpRatio <= 0.4f && spell.Category == SpellCategory.Heal)
            {
                weight *= 3.0f;
            }

            if (spell.Category == SpellCategory.Attack && spell.Element != ElementType.None)
            {
                weight += target.GetElementStacks(spell.Element) * 0.15f;
            }

            if (previous != null && previous.NextSpellAttackCoefficientBonus > 0f && spell.Category == SpellCategory.Attack)
            {
                weight += 0.8f;
            }

            return Mathf.Max(0f, weight);
        }

        private void AddProfileSpellsToPool(List<SpellData> pool)
        {
            if (profile == null || pool == null)
            {
                return;
            }

            if (profile.Spells != null)
            {
                for (int i = 0; i < profile.Spells.Count; i++)
                {
                    SpellData spell = profile.Spells[i] != null ? profile.Spells[i].spell : null;
                    if (spell != null && !pool.Contains(spell))
                    {
                        pool.Add(spell);
                    }
                }
            }

            if (profile.FallbackSpell != null && !pool.Contains(profile.FallbackSpell))
            {
                pool.Add(profile.FallbackSpell);
            }
        }

        private float GetFrequencyWeight(EnemySpellFrequency frequency)
        {
            switch (frequency)
            {
                case EnemySpellFrequency.Low:
                    return 0.35f;
                case EnemySpellFrequency.High:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }

        private SpellData ChooseSpell(List<SpellData> pool, BattleUnit enemy, BattleUnit target, int orderIndex, SpellData previous, int availableMp)
        {
            float totalWeight = 0f;
            List<float> weights = new List<float>(pool.Count);

            for (int i = 0; i < pool.Count; i++)
            {
                SpellData spell = pool[i];
                float weight = 0f;

                if (spell != null && spell.IsValidForOrder(orderIndex) && spell.MpCost <= availableMp)
                {
                    weight = 1f;
                    weight *= spell.GetOrderPreferenceWeight(orderIndex);

                    float hpRatio = (float)enemy.CurrentHP / Mathf.Max(1, enemy.MaxHP);
                    if (hpRatio <= 0.4f && spell.Category == SpellCategory.Heal)
                    {
                        weight *= 3.0f;
                    }

                    if (spell.Category == SpellCategory.Attack && spell.Element != ElementType.None)
                    {
                        weight += target.GetElementStacks(spell.Element) * 0.15f;
                    }

                    if (previous != null && previous.NextSpellAttackCoefficientBonus > 0f && spell.Category == SpellCategory.Attack)
                    {
                        weight += 0.8f;
                    }
                }

                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return pool[i];
                }
            }

            return null;
        }
    }
}
