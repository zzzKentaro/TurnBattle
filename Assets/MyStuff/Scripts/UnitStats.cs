using System;
using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// ユニットの基礎ステータスを定義するデータ構造体。
    /// </summary>
    [Serializable]
    public class UnitStats
    {
        [Min(1)] public int maxHP = 1000;
        [Min(0)] public int maxMP = 1000;
        [Min(0)] public int attack = 100;
        [Min(0)] public int defense = 30;
    }
}
