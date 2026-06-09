using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    public enum EnemySpellFrequency
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    [Serializable]
    public class EnemySpellEntry
    {
        public SpellData spell;
        public EnemySpellFrequency frequency = EnemySpellFrequency.Medium;
        public bool forceOnFirstTurn;
        [Min(0)] public int forceOnTurn;
        public bool onlyOnce;
        [Min(0)] public int earliestTurn;
        [Min(0)] public int initialTotalUseCount;
    }

    [CreateAssetMenu(fileName = "EnemyAIProfile", menuName = "TurnBattle/Enemy AI Profile")]
    public class EnemyAIProfile : ScriptableObject
    {
        [SerializeField] private List<EnemySpellEntry> spells = new List<EnemySpellEntry>();
        [SerializeField] private SpellData fallbackSpell;

        public IReadOnlyList<EnemySpellEntry> Spells => spells;
        public SpellData FallbackSpell => fallbackSpell;
    }
}
