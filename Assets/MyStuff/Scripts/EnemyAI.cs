using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    public class EnemyAI : MonoBehaviour
    {
        public List<SpellAction> BuildTurnActions(BattleUnit enemy, BattleUnit target)
        {
            List<SpellAction> actions = new List<SpellAction>();
            if (enemy == null || target == null)
            {
                return actions;
            }

            List<SpellData> pool = new List<SpellData>(enemy.EnemySpellPool);
            if (pool.Count == 0)
            {
                return actions;
            }

            SpellData previous = null;
            int reservedMp = 0;

            for (int orderIndex = 0; orderIndex < 3; orderIndex++)
            {
                int availableMp = Mathf.Max(0, enemy.CurrentMP - reservedMp);
                SpellData chosen = ChooseSpell(pool, enemy, target, orderIndex, previous, availableMp);
                if (chosen == null)
                {
                    break;
                }

                BattleUnit actualTarget = chosen.TargetType == TargetType.Self ? enemy : target;
                actions.Add(new SpellAction(enemy, actualTarget, chosen, orderIndex));

                reservedMp += Mathf.Max(0, chosen.MpCost);
                previous = chosen;
            }

            return actions;
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
