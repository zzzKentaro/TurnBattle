using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "BattleWaveData", menuName = "TurnBattle/Battle Wave Data")]
    public class BattleWaveData : ScriptableObject
    {
        [SerializeField] private string waveDisplayName = "Wave 1";
        [SerializeField] private BattleUnit enemyUnitOverride;
        [SerializeField] private List<SpellData> enemySpells = new List<SpellData>();
        [SerializeField] private EnemyAIProfile enemyAIProfile;
        [SerializeField] private AudioClip waveBgmClip;
        [SerializeField] private List<SpellData> playerSpellsToAddOnStart = new List<SpellData>();
        [SerializeField] private List<SpellData> initialEnemyLastUsedSpells = new List<SpellData>();
        [SerializeField, TextArea] private string waveStartLogMessage;
        [SerializeField, TextArea] private string waveVictoryLogMessage;

        public string WaveDisplayName => waveDisplayName;
        public BattleUnit EnemyUnitOverride => enemyUnitOverride;
        public IReadOnlyList<SpellData> EnemySpells => enemySpells;
        public EnemyAIProfile EnemyAIProfile => enemyAIProfile;
        public AudioClip WaveBgmClip => waveBgmClip;
        public IReadOnlyList<SpellData> PlayerSpellsToAddOnStart => playerSpellsToAddOnStart;
        public IReadOnlyList<SpellData> InitialEnemyLastUsedSpells => initialEnemyLastUsedSpells;
        public string WaveStartLogMessage => waveStartLogMessage;
        public string WaveVictoryLogMessage => waveVictoryLogMessage;
    }
}
