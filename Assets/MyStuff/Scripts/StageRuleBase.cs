using System;
using UnityEngine;

namespace TurnBasedBattle
{
    public class StageRuleContext
    {
        public BattleManager BattleManager { get; }
        public BattleStageData StageData { get; }
        public BattleWaveData WaveData { get; }
        public int WaveIndex { get; }
        public SpellAction Action { get; }
        public SpellExecutionReport Report { get; }
        public Action<string> Log { get; }

        public BattleUnit Caster => Action != null ? Action.Caster : null;
        public BattleUnit Target => Action != null ? Action.Target : null;
        public SpellData SpellData => Action != null ? Action.SpellData : null;

        public StageRuleContext(
            BattleManager battleManager,
            BattleStageData stageData,
            BattleWaveData waveData,
            int waveIndex,
            SpellAction action,
            SpellExecutionReport report,
            Action<string> log)
        {
            BattleManager = battleManager;
            StageData = stageData;
            WaveData = waveData;
            WaveIndex = waveIndex;
            Action = action;
            Report = report;
            Log = log;
        }
    }

    /// <summary>
    /// ステージ固有ギミックをBattleManagerやSpellDataへ直接書かずに追加するための基底クラス。
    /// </summary>
    public abstract class StageRuleBase : ScriptableObject
    {
        public virtual void OnStageStarted(StageRuleContext context) { }
        public virtual void OnWaveStarted(StageRuleContext context) { }

        public virtual bool CanCast(StageRuleContext context, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void BeforeSpellResolved(StageRuleContext context) { }
        public virtual void AfterSpellResolved(StageRuleContext context) { }
    }
}
