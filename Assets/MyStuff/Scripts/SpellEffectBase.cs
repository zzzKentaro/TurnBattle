using System;
using UnityEngine;

namespace TurnBasedBattle
{
    public class SpellEffectContext
    {
        public SpellAction Action { get; }
        public BattleManager BattleManager { get; }
        public BattleTuning Tuning { get; }
        public SpellExecutionReport Report { get; set; }
        public BattleUnit Caster => Action != null ? Action.Caster : null;
        public BattleUnit Target => Action != null ? Action.Target : null;
        public SpellData SpellData => Action != null ? Action.SpellData : null;
        public float AttackCoefficientBonus { get; set; }
        public float DefenseCoefficientBonus { get; set; }
        public float DamageMultiplier { get; set; } = 1f;
        public int FlatDamageBonus { get; set; }
        public int HitIndex { get; set; }
        public int TurnNumber => BattleManager != null ? BattleManager.CurrentTurnNumber : 1;
        public Action<string> Log { get; }

        public SpellEffectContext(
            SpellAction action,
            BattleManager battleManager,
            BattleTuning tuning,
            Action<string> log)
        {
            Action = action;
            BattleManager = battleManager;
            Tuning = tuning;
            Log = log;
        }
    }

    /// <summary>
    /// SpellDataを肥大化させず、個別魔法の特殊効果をScriptableObjectとして追加するための基底クラス。
    /// </summary>
    public abstract class SpellEffectBase : ScriptableObject
    {
        public virtual bool CanCast(SpellEffectContext context, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void BeforeResolve(SpellEffectContext context) { }
        public virtual void BeforeDamageCalculation(SpellEffectContext context) { }
        public virtual void AfterSpellResolved(SpellEffectContext context, SpellExecutionReport report) { }
    }
}
