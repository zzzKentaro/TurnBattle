using UnityEngine;

namespace TurnBasedBattle
{
    /// <summary>
    /// 個々の魔法の効果、消費コスト、属性、演出などを定義するデータクラス（ScriptableObject）。
    /// </summary>
    [CreateAssetMenu(fileName = "SpellData", menuName = "TurnBattle/Spell Data")]
    public class SpellData : ScriptableObject
    {
        [Header("識別情報")]
        [SerializeField] private string spellId = "spell_new";
        [SerializeField] private string displayName = "New Spell";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private OneShotParticleCallback impactEffectPrefab;
        [SerializeField] private AudioClip attackEffectSe;

        [Header("基本設定")]
        [SerializeField] private ElementType element = ElementType.None;
        [SerializeField] private SpellCategory category = SpellCategory.Attack;
        [SerializeField] private TargetType targetType = TargetType.SingleEnemy;
        [SerializeField, Min(0)] private int mpCost = 0;

        [Header("攻撃 / 回復")]
        [SerializeField] private float attackCoefficient = 1.0f;
        [SerializeField] private float defenseCoefficient = 1.0f;
        [SerializeField, Min(1)] private int hitCount = 1;
        [SerializeField, Min(0)] private int flatHealAmount = 0;
        [SerializeField, Min(0)] private int stackPerHit = 1;
        [SerializeField] private bool useCurrentHpAsAttackSource = false;
        [SerializeField, Range(0f, 3f)] private float missingHpAttackCoefficientBonusAtZeroHp = 0f;
        [SerializeField] private bool allowCritical = true;
        [SerializeField, Min(0f)] private float defensePenetration = 0f;

        [Header("行動順補正")]
        [SerializeField] private OrderAffinity orderAffinity = OrderAffinity.Any;
        [SerializeField] private float firstOrderAttackCoefficientBonus = 0f;
        [SerializeField] private float secondOrderAttackCoefficientBonus = 0f;
        [SerializeField] private float thirdOrderAttackCoefficientBonus = 0f;
        [SerializeField] private float nextSpellAttackCoefficientBonus = 0f;

        [Header("能力値バフ / デバフ")]
        [SerializeField] private int selfAttackStatDelta = 0;
        [SerializeField] private int selfDefenseStatDelta = 0;
        [SerializeField] private int targetAttackStatDelta = 0;
        [SerializeField] private int targetDefenseStatDelta = 0;
        [SerializeField, Min(0)] private int statModifierDurationTurns = 0;

        [Header("係数バフ / デバフ")]
        [SerializeField] private float selfAttackCoefficientDelta = 0f;
        [SerializeField] private float selfDefenseCoefficientDelta = 0f;
        [SerializeField] private float targetAttackCoefficientDelta = 0f;
        [SerializeField] private float targetDefenseCoefficientDelta = 0f;
        [SerializeField, Min(0)] private int coefficientModifierDurationTurns = 0;

        [Header("レベル成長")]
        [SerializeField, Range(0f, 1f)] private float levelAttackCoefficientPerLevel = 0.05f;
        [SerializeField] private int maxLevel = 6;

        [Header("使用回数で強くなる魔法")]
        [Tooltip("この魔法の総使用回数1回につき加算される攻撃係数です。例: 0.05なら、10回使用済みで+0.5されます。")]
        [SerializeField, Min(0f)] private float useCountAttackCoefficientBonusPerUse = 0f;
        [Tooltip("ONにすると、使用回数による攻撃係数ボーナスに上限を設定します。")]
        [SerializeField] private bool capUseCountAttackCoefficientBonus = false;
        [Tooltip("使用回数による攻撃係数ボーナスの上限です。capUseCountAttackCoefficientBonusがONのときだけ使われます。")]
        [SerializeField, Min(0f)] private float maxUseCountAttackCoefficientBonus = 0f;

        [Header("使用時の永続成長")]
        [Tooltip("この魔法の発動に成功するたび、自分の攻撃力に永続加算される値です。")]
        [SerializeField, Min(0)] private int permanentSelfAttackDeltaOnUse = 0;
        [Tooltip("この魔法の発動に成功するたび、自分の防御力に永続加算される値です。")]
        [SerializeField, Min(0)] private int permanentSelfDefenseDeltaOnUse = 0;
        [Tooltip("この魔法の発動に成功するたび、自分の最大HPに永続加算される値です。")]
        [SerializeField, Min(0)] private int permanentSelfMaxHpDeltaOnUse = 0;
        [Tooltip("この魔法の発動に成功するたび、自分の最大MPに永続加算される値です。")]
        [SerializeField, Min(0)] private int permanentSelfMaxMpDeltaOnUse = 0;
        [Tooltip("最大HPが増えたとき、現在HPも同じ量だけ回復します。")]
        [SerializeField] private bool restoreGainedMaxHpOnUse = true;
        [Tooltip("最大MPが増えたとき、現在MPも同じ量だけ回復します。")]
        [SerializeField] private bool restoreGainedMaxMpOnUse = true;

        public string SpellId => spellId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite IconSprite => iconSprite;
        public OneShotParticleCallback ImpactEffectPrefab => impactEffectPrefab;
        public AudioClip AttackEffectSe => attackEffectSe;
        public ElementType Element => element;
        public SpellCategory Category => category;
        public TargetType TargetType => targetType;
        public int MpCost => mpCost;
        public float AttackCoefficient => attackCoefficient;
        public float DefenseCoefficient => defenseCoefficient;
        public int HitCount => hitCount;
        public int FlatHealAmount => flatHealAmount;
        public int StackPerHit => stackPerHit;
        public bool UseCurrentHpAsAttackSource => useCurrentHpAsAttackSource;
        public float MissingHpAttackCoefficientBonusAtZeroHp => missingHpAttackCoefficientBonusAtZeroHp;
        public bool AllowCritical => allowCritical;
        public float DefensePenetration => defensePenetration;
        public OrderAffinity OrderAffinity => orderAffinity;
        public float NextSpellAttackCoefficientBonus => nextSpellAttackCoefficientBonus;
        public int SelfAttackStatDelta => selfAttackStatDelta;
        public int SelfDefenseStatDelta => selfDefenseStatDelta;
        public int TargetAttackStatDelta => targetAttackStatDelta;
        public int TargetDefenseStatDelta => targetDefenseStatDelta;
        public int StatModifierDurationTurns => statModifierDurationTurns;
        public float SelfAttackCoefficientDelta => selfAttackCoefficientDelta;
        public float SelfDefenseCoefficientDelta => selfDefenseCoefficientDelta;
        public float TargetAttackCoefficientDelta => targetAttackCoefficientDelta;
        public float TargetDefenseCoefficientDelta => targetDefenseCoefficientDelta;
        public int CoefficientModifierDurationTurns => coefficientModifierDurationTurns;
        public float LevelAttackCoefficientPerLevel => levelAttackCoefficientPerLevel;
        public int MaxLevel => maxLevel;
        public float UseCountAttackCoefficientBonusPerUse => useCountAttackCoefficientBonusPerUse;
        public bool CapUseCountAttackCoefficientBonus => capUseCountAttackCoefficientBonus;
        public float MaxUseCountAttackCoefficientBonus => maxUseCountAttackCoefficientBonus;
        public int PermanentSelfAttackDeltaOnUse => permanentSelfAttackDeltaOnUse;
        public int PermanentSelfDefenseDeltaOnUse => permanentSelfDefenseDeltaOnUse;
        public int PermanentSelfMaxHpDeltaOnUse => permanentSelfMaxHpDeltaOnUse;
        public int PermanentSelfMaxMpDeltaOnUse => permanentSelfMaxMpDeltaOnUse;
        public bool RestoreGainedMaxHpOnUse => restoreGainedMaxHpOnUse;
        public bool RestoreGainedMaxMpOnUse => restoreGainedMaxMpOnUse;

        public bool HasUseCountScaling => useCountAttackCoefficientBonusPerUse > 0f;

        public bool HasPermanentSelfGrowth =>
            permanentSelfAttackDeltaOnUse > 0 ||
            permanentSelfDefenseDeltaOnUse > 0 ||
            permanentSelfMaxHpDeltaOnUse > 0 ||
            permanentSelfMaxMpDeltaOnUse > 0;

        public float GetUseCountAttackCoefficientBonus(int totalUseCount)
        {
            float bonus = Mathf.Max(0, totalUseCount) * useCountAttackCoefficientBonusPerUse;
            if (capUseCountAttackCoefficientBonus)
            {
                bonus = Mathf.Min(bonus, maxUseCountAttackCoefficientBonus);
            }

            return Mathf.Max(0f, bonus);
        }

        public float GetOrderSpecificAttackCoefficientBonus(int orderIndex)
        {
            switch (orderIndex)
            {
                case 0:
                    return firstOrderAttackCoefficientBonus;
                case 1:
                    return secondOrderAttackCoefficientBonus;
                case 2:
                    return thirdOrderAttackCoefficientBonus;
                default:
                    return 0f;
            }
        }

        public bool IsValidForOrder(int orderIndex)
        {
            switch (orderAffinity)
            {
                case OrderAffinity.FirstOnly:
                    return orderIndex == 0;
                case OrderAffinity.SecondOnly:
                    return orderIndex == 1;
                case OrderAffinity.ThirdOnly:
                    return orderIndex == 2;
                default:
                    return true;
            }
        }

        public float GetOrderPreferenceWeight(int orderIndex)
        {
            switch (orderAffinity)
            {
                case OrderAffinity.PreferFirst:
                    return orderIndex == 0 ? 1.7f : 1f;
                case OrderAffinity.PreferSecond:
                    return orderIndex == 1 ? 1.7f : 1f;
                case OrderAffinity.PreferThird:
                    return orderIndex == 2 ? 1.7f : 1f;
                case OrderAffinity.FirstOnly:
                    return orderIndex == 0 ? 2f : 0f;
                case OrderAffinity.SecondOnly:
                    return orderIndex == 1 ? 2f : 0f;
                case OrderAffinity.ThirdOnly:
                    return orderIndex == 2 ? 2f : 0f;
                default:
                    return 1f;
            }
        }
    }
}
