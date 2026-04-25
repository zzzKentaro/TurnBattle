using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [Serializable]
    public class MagicMemoryBook
    {
        [SerializeField, Min(1)] private int maxSlots = 10;
        [SerializeField] private List<RememberedSpell> rememberedSpells = new List<RememberedSpell>();

        public int MaxSlots => maxSlots;
        public IReadOnlyList<RememberedSpell> RememberedSpells => rememberedSpells;
        public bool IsFull => rememberedSpells.Count >= maxSlots;
        public int FirstEmptyIndex => rememberedSpells.Count < maxSlots ? rememberedSpells.Count : -1;
        public int Count => rememberedSpells.Count;

        public void Clear()
        {
            rememberedSpells.Clear();
        }

        public RememberedSpell Find(SpellData spellData)
        {
            if (spellData == null)
            {
                return null;
            }

            for (int i = 0; i < rememberedSpells.Count; i++)
            {
                if (rememberedSpells[i] != null && rememberedSpells[i].SpellData == spellData)
                {
                    return rememberedSpells[i];
                }
            }

            return null;
        }

        public int IndexOf(SpellData spellData)
        {
            if (spellData == null)
            {
                return -1;
            }

            for (int i = 0; i < rememberedSpells.Count; i++)
            {
                if (rememberedSpells[i] != null && rememberedSpells[i].SpellData == spellData)
                {
                    return i;
                }
            }

            return -1;
        }

        public RememberedSpell LearnSpell(SpellData spellData, bool duplicateGivesPractice = false)
        {
            TryLearnSpellInFirstEmptySlot(
                spellData,
                out RememberedSpell result,
                out _,
                out _,
                duplicateGivesPractice);

            return result;
        }

        /// <summary>
        /// 空きスロットに新しい魔法を記憶する。
        /// すでに覚えている魔法なら新規追加せず、既存スロットを返す。
        /// 満杯なら勝手に古い魔法を消さず、false を返す。
        /// </summary>
        public bool TryLearnSpellInFirstEmptySlot(
            SpellData spellData,
            out RememberedSpell result,
            out int slotIndex,
            out bool alreadyKnown,
            bool duplicateGivesPractice = true)
        {
            result = null;
            slotIndex = -1;
            alreadyKnown = false;

            if (spellData == null)
            {
                return false;
            }

            RememberedSpell existing = Find(spellData);
            if (existing != null)
            {
                alreadyKnown = true;
                result = existing;
                slotIndex = IndexOf(spellData);

                if (duplicateGivesPractice)
                {
                    existing.GainPractice(1);
                }
                return true;
            }

            if (rememberedSpells.Count >= maxSlots)
            {
                return false;
            }

            slotIndex = rememberedSpells.Count;
            result = new RememberedSpell(spellData);
            rememberedSpells.Add(result);
            return true;
        }

        public bool ReplaceSpellAt(int index, SpellData spellData, bool duplicateGivesPractice = true)
        {
            return TryReplaceSpellAt(index, spellData, out _, out _, duplicateGivesPractice);
        }

        public bool TryReplaceSpellAt(
            int index,
            SpellData spellData,
            out RememberedSpell result,
            out bool alreadyKnown,
            bool duplicateGivesPractice = true)
        {
            result = null;
            alreadyKnown = false;

            if (spellData == null)
            {
                return false;
            }

            if (index < 0 || index >= rememberedSpells.Count)
            {
                return false;
            }

            RememberedSpell existing = Find(spellData);
            if (existing != null)
            {
                alreadyKnown = true;
                result = existing;

                if (duplicateGivesPractice)
                {
                    existing.GainPractice(1);
                }
                return true;
            }

            result = new RememberedSpell(spellData);
            rememberedSpells[index] = result;
            return true;
        }
    }
}
