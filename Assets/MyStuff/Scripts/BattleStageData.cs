using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [CreateAssetMenu(fileName = "BattleStageData", menuName = "TurnBattle/Battle Stage Data")]
    public class BattleStageData : ScriptableObject
    {
        [SerializeField] private string stageDisplayName = "New Stage";
        [SerializeField] private bool clearPlayerMemoryOnStageStart = true;
        [SerializeField] private List<SpellData> playerStartingSpells = new List<SpellData>();
        [SerializeField] private List<StageRuleBase> stageRules = new List<StageRuleBase>();
        [SerializeField] private List<BattleWaveData> waves = new List<BattleWaveData>();
        [SerializeField, TextArea] private string stageStartLogMessage;
        [SerializeField, TextArea] private string stageClearLogMessage;

        public string StageDisplayName => stageDisplayName;
        public bool ClearPlayerMemoryOnStageStart => clearPlayerMemoryOnStageStart;
        public IReadOnlyList<SpellData> PlayerStartingSpells => playerStartingSpells;
        public IReadOnlyList<StageRuleBase> StageRules => stageRules;
        public IReadOnlyList<BattleWaveData> Waves => waves;
        public string StageStartLogMessage => stageStartLogMessage;
        public string StageClearLogMessage => stageClearLogMessage;
    }
}
