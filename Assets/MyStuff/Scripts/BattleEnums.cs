using System;

/// <summary>
/// バトルシステム全体で使用される列挙型（バトルのフェーズ状態など）を定義する。
/// </summary>
namespace TurnBasedBattle
{
    public enum ElementType
    {
        None = 0,
        Earth = 1,
        Water = 2,
        Fire = 3,
        Wind = 4,
        Thunder = 5,
    }

    public enum SpellCategory
    {
        Attack = 0,
        Heal = 1,
        Buff = 2,
        Debuff = 3,
        Utility = 4,
    }

    public enum TargetType
    {
        Self = 0,
        SingleEnemy = 1,
    }

    public enum OrderAffinity
    {
        Any = 0,
        PreferFirst = 1,
        PreferSecond = 2,
        PreferThird = 3,
        FirstOnly = 4,
        SecondOnly = 5,
        ThirdOnly = 6,
    }

    public enum StatType
    {
        Attack = 0,
        Defense = 1,
    }

    public enum CoefficientModifierType
    {
        AttackCoefficientAdd = 0,
        DefenseCoefficientAdd = 1,
    }

    public enum BattleFlowState
    {
        Idle = 0,
        PlayerSelection = 1,
        PlayerExecution = 2,
        EnemyExecution = 3,
        MemoryPhase = 4,
        Victory = 5,
        Defeat = 6,
    }
}
