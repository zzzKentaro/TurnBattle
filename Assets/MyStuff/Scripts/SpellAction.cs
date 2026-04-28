using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// 実行される魔法のアクション内容（誰から誰へ、どの魔法か）を保持するクラス。
    /// </summary>
    [System.Serializable]
    public class SpellAction
    {
        [SerializeField] private BattleUnit caster;
        [SerializeField] private BattleUnit target;
        [SerializeField] private SpellData spellData;
        [SerializeField] private RememberedSpell rememberedSpell;
        [SerializeField] private int orderIndex;

        public BattleUnit Caster => caster;
        public BattleUnit Target => target;
        public SpellData SpellData => rememberedSpell != null ? rememberedSpell.SpellData : spellData;
        public RememberedSpell RememberedSpell => rememberedSpell;
        public int OrderIndex => orderIndex;
        public int TotalUseCount => rememberedSpell != null ? rememberedSpell.TotalUseCount : 0;

        public SpellAction(BattleUnit caster, BattleUnit target, RememberedSpell rememberedSpell, int orderIndex)
        {
            this.caster = caster;
            this.target = target;
            this.rememberedSpell = rememberedSpell;
            this.spellData = rememberedSpell != null ? rememberedSpell.SpellData : null;
            this.orderIndex = orderIndex;
        }

        public SpellAction(BattleUnit caster, BattleUnit target, SpellData spellData, int orderIndex)
        {
            this.caster = caster;
            this.target = target;
            this.spellData = spellData;
            this.rememberedSpell = null;
            this.orderIndex = orderIndex;
        }

        public float GetLevelAttackCoefficientBonus()
        {
            return rememberedSpell != null ? rememberedSpell.GetLevelAttackCoefficientBonus() : 0f;
        }

        public float GetUseCountAttackCoefficientBonus()
        {
            return rememberedSpell != null ? rememberedSpell.GetUseCountAttackCoefficientBonus() : 0f;
        }

        public void RegisterUseIfNeeded()
        {
            rememberedSpell?.RegisterUse();
        }
    }
}
