using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [Serializable]
    public class ResolvedHitInfo
    {
        public BattleUnit target;
        public int damage;
        public bool isHealing;
        public OneShotParticleCallback effectPrefab;
        public AudioClip effectSe;

        public ResolvedHitInfo(BattleUnit target, int damage, OneShotParticleCallback effectPrefab, AudioClip effectSe, bool isHealing = false)
        {
            this.target = target;
            this.damage = damage;
            this.isHealing = isHealing;
            this.effectPrefab = effectPrefab;
            this.effectSe = effectSe;
        }
    }

    public class SpellExecutionReport
    {
        public int totalDamage;
        public int totalHealing;
        public bool anyCritical;
        public float grantNextSpellAttackCoefficientBonus;
        public readonly List<ResolvedHitInfo> hits = new List<ResolvedHitInfo>();
    }

    /// <summary>
    /// プレイヤーおよび敵が予約した魔法アクションの実行順序や効果解決を処理する。
    /// </summary>
    public static class SpellResolver
    {
        public static SpellExecutionReport Execute(
            SpellAction action,
            BattleTuning tuning,
            float carriedSequenceAttackCoefficientBonus,
            Action<string> log)
        {
            SpellExecutionReport report = new SpellExecutionReport();

            if (action == null || action.SpellData == null || action.Caster == null || action.Target == null)
            {
                log?.Invoke("行動データが不正です");
                return report;
            }

            SpellData spell = action.SpellData;
            BattleUnit caster = action.Caster;
            BattleUnit target = action.Target;

            if (caster.IsDead)
            {
                log?.Invoke($"{caster.UnitName} は倒れているため行動できない");
                return report;
            }

            if (!spell.IsValidForOrder(action.OrderIndex))
            {
                log?.Invoke($"{spell.DisplayName} はこの順番では使えない");
                return report;
            }

            if (!caster.SpendMP(spell.MpCost))
            {
                log?.Invoke($"{caster.UnitName} はMPが足りず {spell.DisplayName} を使えない");
                return report;
            }

            log?.Invoke($"{caster.UnitName} が {spell.DisplayName} を発動。({action.OrderIndex + 1}番目)");

            switch (spell.Category)
            {
                case SpellCategory.Attack:
                    ExecuteAttack(action, tuning, carriedSequenceAttackCoefficientBonus, report, log);
                    break;

                case SpellCategory.Heal:
                    ExecuteHeal(action, report, log);
                    break;

                case SpellCategory.Buff:
                case SpellCategory.Debuff:
                case SpellCategory.Utility:
                    break;
            }

            ApplyModifiers(spell, caster, target, log);
            ApplyPermanentSelfGrowth(spell, caster, log);
            action.RegisterUseIfNeeded();
            report.grantNextSpellAttackCoefficientBonus = spell.NextSpellAttackCoefficientBonus;
            return report;
        }

        private static void ExecuteAttack(
            SpellAction action,
            BattleTuning tuning,
            float carriedSequenceAttackCoefficientBonus,
            SpellExecutionReport report,
            Action<string> log)
        {
            SpellData spell = action.SpellData;
            BattleUnit caster = action.Caster;
            BattleUnit target = action.Target;

            for (int i = 0; i < spell.HitCount; i++)
            {
                DamageCalculationResult calc = DamageCalculator.Calculate(new DamageCalculationInput
                {
                    caster = caster,
                    target = target,
                    spellData = spell,
                    levelAttackCoefficientBonus = action.GetLevelAttackCoefficientBonus(),
                    useCountAttackCoefficientBonus = action.GetUseCountAttackCoefficientBonus(),
                    carriedSequenceAttackCoefficientBonus = carriedSequenceAttackCoefficientBonus,
                    orderIndex = action.OrderIndex,
                    tuning = tuning,
                });

                target.TakeDamage(calc.damage);
                report.totalDamage += calc.damage;
                report.anyCritical |= calc.isCritical;
                report.hits.Add(new ResolvedHitInfo(target, calc.damage, spell.ImpactEffectPrefab, spell.AttackEffectSe));

                if (spell.Element != ElementType.None && spell.StackPerHit > 0)
                {
                    target.ElementStacks.AddStacks(spell.Element, spell.StackPerHit);
                }

                string criticalText = calc.isCritical ? " クリティカル！" : string.Empty;
                log?.Invoke($"  {target.UnitName} に {calc.damage} ダメージ{criticalText}");

                if (target.IsDead)
                {
                    log?.Invoke($"{target.UnitName} は倒れた");
                    break;
                }
            }

            if (!target.IsDead)
            {
                TryWindFollowUp(caster, target, tuning, report, log);
            }
        }

        private static void TryWindFollowUp(
            BattleUnit caster,
            BattleUnit target,
            BattleTuning tuning,
            SpellExecutionReport report,
            Action<string> log)
        {
            int windStacks = caster.GetElementStacks(ElementType.Wind);
            float followUpCoefficient = tuning.GetWindFollowUpCoefficient(windStacks);
            if (followUpCoefficient <= 0f || target.IsDead)
            {
                return;
            }

            int raw = Mathf.RoundToInt((caster.GetCurrentAttack() * followUpCoefficient) - target.GetCurrentDefense());
            int followUpDamage = Mathf.Max(1, raw);
            target.TakeDamage(followUpDamage);
            report.totalDamage += followUpDamage;
            report.hits.Add(new ResolvedHitInfo(target, followUpDamage, null, null));
            log?.Invoke($"  風属性追撃！ {target.UnitName} に {followUpDamage} ダメージ");

            if (target.IsDead)
            {
                log?.Invoke($"{target.UnitName} は倒れた");
            }
        }

        private static void ExecuteHeal(SpellAction action, SpellExecutionReport report, Action<string> log)
        {
            SpellData spell = action.SpellData;
            BattleUnit caster = action.Caster;
            BattleUnit target = action.Target;

            int amount = spell.FlatHealAmount + Mathf.RoundToInt(caster.GetCurrentAttack() * (spell.AttackCoefficient + action.GetLevelAttackCoefficientBonus() + action.GetUseCountAttackCoefficientBonus()));
            amount = Mathf.Max(1, amount);

            int beforeHp = target.CurrentHP;
            target.Heal(amount);
            int actualHealing = Mathf.Max(0, target.CurrentHP - beforeHp);

            report.totalHealing += actualHealing;
            report.hits.Add(new ResolvedHitInfo(target, actualHealing, spell.ImpactEffectPrefab, spell.AttackEffectSe, true));
            log?.Invoke($"  {target.UnitName} の HP が {actualHealing} 回復");
        }


        private static void ApplyPermanentSelfGrowth(SpellData spell, BattleUnit caster, Action<string> log)
        {
            if (spell == null || caster == null || !spell.HasPermanentSelfGrowth)
            {
                return;
            }

            int beforeAttack = caster.GetCurrentAttack();
            int beforeDefense = caster.GetCurrentDefense();
            int beforeMaxHP = caster.MaxHP;
            int beforeMaxMP = caster.MaxMP;

            caster.ApplyPermanentGrowth(
                spell.PermanentSelfAttackDeltaOnUse,
                spell.PermanentSelfDefenseDeltaOnUse,
                spell.PermanentSelfMaxHpDeltaOnUse,
                spell.PermanentSelfMaxMpDeltaOnUse,
                spell.RestoreGainedMaxHpOnUse,
                spell.RestoreGainedMaxMpOnUse);

            int attackGain = Mathf.Max(0, caster.GetCurrentAttack() - beforeAttack);
            int defenseGain = Mathf.Max(0, caster.GetCurrentDefense() - beforeDefense);
            int maxHpGain = Mathf.Max(0, caster.MaxHP - beforeMaxHP);
            int maxMpGain = Mathf.Max(0, caster.MaxMP - beforeMaxMP);

            if (attackGain > 0)
            {
                log?.Invoke($"  {caster.UnitName} の攻撃力が永続的に {attackGain} 上がった");
            }

            if (defenseGain > 0)
            {
                log?.Invoke($"  {caster.UnitName} の防御力が永続的に {defenseGain} 上がった");
            }

            if (maxHpGain > 0)
            {
                log?.Invoke($"  {caster.UnitName} の最大HPが永続的に {maxHpGain} 上がった");
            }

            if (maxMpGain > 0)
            {
                log?.Invoke($"  {caster.UnitName} の最大MPが永続的に {maxMpGain} 上がった");
            }
        }

        private static void ApplyModifiers(SpellData spell, BattleUnit caster, BattleUnit target, Action<string> log)
        {
            if (spell.SelfAttackStatDelta != 0)
            {
                caster.ApplyStatModifier(StatType.Attack, spell.SelfAttackStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke($"  {caster.UnitName} の攻撃力が {(spell.SelfAttackStatDelta >= 0 ? "上がった" : "下がった")}");
            }

            if (spell.SelfDefenseStatDelta != 0)
            {
                caster.ApplyStatModifier(StatType.Defense, spell.SelfDefenseStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke($"  {caster.UnitName} の防御力が {(spell.SelfDefenseStatDelta >= 0 ? "上がった" : "下がった")}");
            }

            if (spell.TargetAttackStatDelta != 0)
            {
                target.ApplyStatModifier(StatType.Attack, spell.TargetAttackStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke($"  {target.UnitName} の攻撃力が {(spell.TargetAttackStatDelta >= 0 ? "上がった" : "下がった")}");
            }

            if (spell.TargetDefenseStatDelta != 0)
            {
                target.ApplyStatModifier(StatType.Defense, spell.TargetDefenseStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke($"  {target.UnitName} の防御力が {(spell.TargetDefenseStatDelta >= 0 ? "上がった" : "下がった")}");
            }

            if (!Mathf.Approximately(spell.SelfAttackCoefficientDelta, 0f))
            {
                caster.ApplyCoefficientModifier(CoefficientModifierType.AttackCoefficientAdd, spell.SelfAttackCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke($"  {caster.UnitName} の攻撃係数が変化した");
            }

            if (!Mathf.Approximately(spell.SelfDefenseCoefficientDelta, 0f))
            {
                caster.ApplyCoefficientModifier(CoefficientModifierType.DefenseCoefficientAdd, spell.SelfDefenseCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke($"  {caster.UnitName} の防御係数が変化した");
            }

            if (!Mathf.Approximately(spell.TargetAttackCoefficientDelta, 0f))
            {
                target.ApplyCoefficientModifier(CoefficientModifierType.AttackCoefficientAdd, spell.TargetAttackCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke($"  {target.UnitName} の攻撃係数が変化した");
            }

            if (!Mathf.Approximately(spell.TargetDefenseCoefficientDelta, 0f))
            {
                target.ApplyCoefficientModifier(CoefficientModifierType.DefenseCoefficientAdd, spell.TargetDefenseCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke($"  {target.UnitName} の防御係数が変化した");
            }
        }
    }
}
