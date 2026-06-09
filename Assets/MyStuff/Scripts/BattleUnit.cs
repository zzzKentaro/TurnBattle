using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [Serializable]
    public class TimedStatModifier
    {
        public StatType statType;
        public int delta;
        public int remainingTurns;
    }

    [Serializable]
    public class TimedCoefficientModifier
    {
        public CoefficientModifierType modifierType;
        public float delta;
        public int remainingTurns;
    }

    /// <summary>
    /// プレイヤーおよび敵ユニットのステータス、MP、HPなどを管理するコンポーネント。
    /// </summary>
    public class BattleUnit : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string unitName = "Unit";
        [SerializeField] private bool isPlayer = false;

        [Header("Base stats")]
        [SerializeField] private UnitStats baseStats = new UnitStats();

        [Header("Presentation")]
        [SerializeField] private DamageTargetAnchor damageTargetAnchor;

        [Header("Runtime")]
        [SerializeField] private int currentHP;
        [SerializeField] private int currentMP;
        [SerializeField] private ElementStackController elementStacks = new ElementStackController();
        [SerializeField] private MagicMemoryBook memoryBook = new MagicMemoryBook();
        [SerializeField] private List<SpellData> enemySpellPool = new List<SpellData>();
        [SerializeField] private List<TimedStatModifier> activeStatModifiers = new List<TimedStatModifier>();
        [SerializeField] private List<TimedCoefficientModifier> activeCoefficientModifiers = new List<TimedCoefficientModifier>();

        [Header("Runtime Permanent Growth")]
        [SerializeField] private int permanentMaxHpBonus = 0;
        [SerializeField] private int permanentMaxMpBonus = 0;
        [SerializeField] private int permanentAttackBonus = 0;
        [SerializeField] private int permanentDefenseBonus = 0;

        public IReadOnlyList<TimedStatModifier> ActiveStatModifiers => activeStatModifiers;
        public IReadOnlyList<TimedCoefficientModifier> ActiveCoefficientModifiers => activeCoefficientModifiers;

        public string UnitName => unitName;
        public bool IsPlayer => isPlayer;
        public int CurrentHP => currentHP;
        public int CurrentMP => currentMP;
        public int MaxHP => Mathf.Max(1, baseStats.maxHP + permanentMaxHpBonus);
        public int MaxMP => Mathf.Max(0, baseStats.maxMP + permanentMaxMpBonus);
        public int PermanentMaxHpBonus => permanentMaxHpBonus;
        public int PermanentMaxMpBonus => permanentMaxMpBonus;
        public int PermanentAttackBonus => permanentAttackBonus;
        public int PermanentDefenseBonus => permanentDefenseBonus;
        public bool IsDead => currentHP <= 0;
        public DamageTargetAnchor DamageTargetAnchor => damageTargetAnchor;
        public ElementStackController ElementStacks => elementStacks;
        public MagicMemoryBook MemoryBook => memoryBook;
        public IReadOnlyList<SpellData> EnemySpellPool => enemySpellPool;

        public void InitializeForBattle()
        {
            permanentMaxHpBonus = 0;
            permanentMaxMpBonus = 0;
            permanentAttackBonus = 0;
            permanentDefenseBonus = 0;

            currentHP = MaxHP;
            currentMP = MaxMP;
            elementStacks.EnsureInitialized();
            elementStacks.ClearAll();
            activeStatModifiers.Clear();
            activeCoefficientModifiers.Clear();
        }

        public void SetEnemySpellPool(IEnumerable<SpellData> spells)
        {
            enemySpellPool.Clear();
            if (spells == null)
            {
                return;
            }

            foreach (SpellData spell in spells)
            {
                if (spell != null)
                {
                    enemySpellPool.Add(spell);
                }
            }
        }

        public void SetEnemySpellsForWave(IEnumerable<SpellData> spells, bool replaceMemoryBook = true)
        {
            SetEnemySpellPool(spells);

            if (!replaceMemoryBook)
            {
                return;
            }

            ClearMemory();
            LearnSpells(enemySpellPool, false);
        }

        public void ClearMemory()
        {
            memoryBook.Clear();
        }

        public void LearnSpells(IEnumerable<SpellData> spells, bool duplicateGivesPractice = false)
        {
            if (spells == null)
            {
                return;
            }

            foreach (SpellData spell in spells)
            {
                memoryBook.LearnSpell(spell, duplicateGivesPractice);
            }
        }

        public int GetCurrentAttack()
        {
            return Mathf.Max(0, baseStats.attack + permanentAttackBonus + GetStatModifierSum(StatType.Attack));
        }

        public int GetCurrentDefense()
        {
            return Mathf.Max(0, baseStats.defense + permanentDefenseBonus + GetStatModifierSum(StatType.Defense));
        }

        public int GetElementStacks(ElementType elementType)
        {
            return elementStacks.GetStacks(elementType);
        }

        public float GetAttackCoefficientModifierTotal()
        {
            return GetCoefficientModifierSum(CoefficientModifierType.AttackCoefficientAdd);
        }

        public float GetDefenseCoefficientModifierTotal()
        {
            return GetCoefficientModifierSum(CoefficientModifierType.DefenseCoefficientAdd);
        }

        public bool SpendMP(int amount)
        {
            if (amount < 0)
            {
                amount = 0;
            }

            if (currentMP < amount)
            {
                return false;
            }

            currentMP -= amount;
            return true;
        }

        public void RestoreMP(int amount)
        {
            currentMP = Mathf.Clamp(currentMP + Mathf.Max(0, amount), 0, MaxMP);
        }

        public void TakeDamage(int amount)
        {
            currentHP = Mathf.Clamp(currentHP - Mathf.Max(0, amount), 0, MaxHP);
        }

        public void Heal(int amount)
        {
            currentHP = Mathf.Clamp(currentHP + Mathf.Max(0, amount), 0, MaxHP);
        }

        public void ApplyPermanentGrowth(
            int attackDelta,
            int defenseDelta,
            int maxHpDelta,
            int maxMpDelta,
            bool restoreGainedMaxHp = true,
            bool restoreGainedMaxMp = true)
        {
            int oldMaxHP = MaxHP;
            int oldMaxMP = MaxMP;

            permanentAttackBonus += Mathf.Max(0, attackDelta);
            permanentDefenseBonus += Mathf.Max(0, defenseDelta);
            permanentMaxHpBonus += Mathf.Max(0, maxHpDelta);
            permanentMaxMpBonus += Mathf.Max(0, maxMpDelta);

            int gainedMaxHP = Mathf.Max(0, MaxHP - oldMaxHP);
            int gainedMaxMP = Mathf.Max(0, MaxMP - oldMaxMP);

            if (restoreGainedMaxHp && gainedMaxHP > 0)
            {
                currentHP += gainedMaxHP;
            }

            if (restoreGainedMaxMp && gainedMaxMP > 0)
            {
                currentMP += gainedMaxMP;
            }

            currentHP = Mathf.Clamp(currentHP, 0, MaxHP);
            currentMP = Mathf.Clamp(currentMP, 0, MaxMP);
        }

        public void ApplyStatModifier(StatType statType, int delta, int durationTurns)
        {
            if (delta == 0 || durationTurns <= 0)
            {
                return;
            }

            activeStatModifiers.Add(new TimedStatModifier
            {
                statType = statType,
                delta = delta,
                remainingTurns = durationTurns,
            });
        }

        public void ApplyCoefficientModifier(CoefficientModifierType modifierType, float delta, int durationTurns)
        {
            if (Mathf.Approximately(delta, 0f) || durationTurns <= 0)
            {
                return;
            }

            activeCoefficientModifiers.Add(new TimedCoefficientModifier
            {
                modifierType = modifierType,
                delta = delta,
                remainingTurns = durationTurns,
            });
        }

        public void AdvanceOwnTurnEnd()
        {
            elementStacks.AdvanceTurn();
            TickStatModifiers();
            TickCoefficientModifiers();
        }

        private int GetStatModifierSum(StatType statType)
        {
            int sum = 0;
            for (int i = 0; i < activeStatModifiers.Count; i++)
            {
                if (activeStatModifiers[i].statType == statType)
                {
                    sum += activeStatModifiers[i].delta;
                }
            }
            return sum;
        }

        private float GetCoefficientModifierSum(CoefficientModifierType modifierType)
        {
            float sum = 0f;
            for (int i = 0; i < activeCoefficientModifiers.Count; i++)
            {
                if (activeCoefficientModifiers[i].modifierType == modifierType)
                {
                    sum += activeCoefficientModifiers[i].delta;
                }
            }
            return sum;
        }

        private void TickStatModifiers()
        {
            for (int i = activeStatModifiers.Count - 1; i >= 0; i--)
            {
                activeStatModifiers[i].remainingTurns--;
                if (activeStatModifiers[i].remainingTurns <= 0)
                {
                    activeStatModifiers.RemoveAt(i);
                }
            }
        }

        private void TickCoefficientModifiers()
        {
            for (int i = activeCoefficientModifiers.Count - 1; i >= 0; i--)
            {
                activeCoefficientModifiers[i].remainingTurns--;
                if (activeCoefficientModifiers[i].remainingTurns <= 0)
                {
                    activeCoefficientModifiers.RemoveAt(i);
                }
            }
        }
    }
}
