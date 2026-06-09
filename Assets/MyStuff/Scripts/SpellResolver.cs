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
        public bool suppressPopup;
        public OneShotParticleCallback effectPrefab;
        public AudioClip effectSe;

        public ResolvedHitInfo(BattleUnit target, int damage, OneShotParticleCallback effectPrefab, AudioClip effectSe, bool isHealing = false, bool suppressPopup = false)
        {
            this.target = target;
            this.damage = damage;
            this.isHealing = isHealing;
            this.suppressPopup = suppressPopup;
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
    /// 予紁E��れた魔法アクションのMP消費、ダメージ、回復、バフ、特殊効果を解決する、E    /// </summary>
    public static class SpellResolver
    {
        public static SpellExecutionReport Execute(
            SpellAction action,
            BattleTuning tuning,
            float carriedSequenceAttackCoefficientBonus,
            Action<string> log)
        {
            return Execute(action, tuning, carriedSequenceAttackCoefficientBonus, log, null, null, null);
        }

        public static SpellExecutionReport Execute(
            SpellAction action,
            BattleTuning tuning,
            float carriedSequenceAttackCoefficientBonus,
            Action<string> log,
            BattleManager battleManager,
            SpellEffectCatalog effectCatalog,
            IReadOnlyList<StageRuleBase> stageRules)
        {
            SpellExecutionReport report = new SpellExecutionReport();

            if (action == null || action.SpellData == null || action.Caster == null || action.Target == null)
            {
                log?.Invoke("Spell resolved.");
                return report;
            }

            SpellData spell = action.SpellData;
            BattleUnit caster = action.Caster;
            BattleUnit target = action.Target;
            IReadOnlyList<SpellEffectBase> effects = effectCatalog != null
                ? effectCatalog.GetEffects(spell)
                : Array.Empty<SpellEffectBase>();
            SpellEffectContext effectContext = new SpellEffectContext(action, battleManager, tuning, log)
            {
                Report = report,
            };

            if (caster.IsDead)
            {
                log?.Invoke("Spell resolved.");
                return report;
            }

            if (!spell.IsValidForOrder(action.OrderIndex))
            {
                log?.Invoke("Spell resolved.");
                return report;
            }

            if (!CanCastByEffects(effects, effectContext, log) ||
                !CanCastByStageRules(stageRules, battleManager, action, report, log))
            {
                return report;
            }

            if (!caster.SpendMP(spell.MpCost))
            {
                log?.Invoke("Spell resolved.");
                return report;
            }

            log?.Invoke("Spell resolved.");

            NotifyBeforeResolve(effects, effectContext);
            NotifyBeforeStageRules(stageRules, battleManager, action, report, log);

            switch (spell.Category)
            {
                case SpellCategory.Attack:
                    ExecuteAttack(action, tuning, carriedSequenceAttackCoefficientBonus, report, log, effects, effectContext);
                    break;

                case SpellCategory.Heal:
                    ExecuteHeal(action, report, log);
                    break;

                case SpellCategory.Buff:
                case SpellCategory.Debuff:
                case SpellCategory.Utility:
                    AddPresentationOnlyHit(action, report);
                    break;
            }

            ApplyModifiers(spell, caster, target, log);
            ApplyPermanentSelfGrowth(spell, caster, log);
            action.RegisterUseIfNeeded();
            report.grantNextSpellAttackCoefficientBonus = spell.NextSpellAttackCoefficientBonus;
            NotifyAfterSpellResolved(effects, effectContext, report);
            NotifyAfterStageRules(stageRules, battleManager, action, report, log);
            return report;
        }

        private static void ExecuteAttack(
            SpellAction action,
            BattleTuning tuning,
            float carriedSequenceAttackCoefficientBonus,
            SpellExecutionReport report,
            Action<string> log,
            IReadOnlyList<SpellEffectBase> effects,
            SpellEffectContext effectContext)
        {
            SpellData spell = action.SpellData;
            BattleUnit caster = action.Caster;
            BattleUnit target = action.Target;

            for (int i = 0; i < spell.HitCount; i++)
            {
                float baseAttackBonus = effectContext.AttackCoefficientBonus;
                float baseDefenseBonus = effectContext.DefenseCoefficientBonus;
                float baseDamageMultiplier = effectContext.DamageMultiplier;
                int baseFlatDamageBonus = effectContext.FlatDamageBonus;

                effectContext.HitIndex = i;
                NotifyBeforeDamageCalculation(effects, effectContext);

                DamageCalculationResult calc = DamageCalculator.Calculate(new DamageCalculationInput
                {
                    caster = caster,
                    target = target,
                    spellData = spell,
                    levelAttackCoefficientBonus = action.GetLevelAttackCoefficientBonus(),
                    useCountAttackCoefficientBonus = action.GetUseCountAttackCoefficientBonus(),
                    carriedSequenceAttackCoefficientBonus = carriedSequenceAttackCoefficientBonus,
                    customAttackCoefficientBonus = effectContext.AttackCoefficientBonus,
                    customDefenseCoefficientBonus = effectContext.DefenseCoefficientBonus,
                    finalDamageMultiplier = effectContext.DamageMultiplier,
                    flatDamageBonus = effectContext.FlatDamageBonus,
                    orderIndex = action.OrderIndex,
                    tuning = tuning,
                });

                effectContext.AttackCoefficientBonus = baseAttackBonus;
                effectContext.DefenseCoefficientBonus = baseDefenseBonus;
                effectContext.DamageMultiplier = baseDamageMultiplier;
                effectContext.FlatDamageBonus = baseFlatDamageBonus;

                target.TakeDamage(calc.damage);
                report.totalDamage += calc.damage;
                report.anyCritical |= calc.isCritical;
                report.hits.Add(new ResolvedHitInfo(target, calc.damage, spell.ImpactEffectPrefab, spell.AttackEffectSe));

                if (spell.Element != ElementType.None && spell.StackPerHit > 0)
                {
                    target.ElementStacks.AddStacks(spell.Element, spell.StackPerHit);
                }

                string criticalText = calc.isCritical ? " Critical!" : string.Empty;
                log?.Invoke("Spell resolved.");

                if (target.IsDead)
                {
                    log?.Invoke("Spell resolved.");
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
            log?.Invoke("Spell resolved.");

            if (target.IsDead)
            {
                log?.Invoke("Spell resolved.");
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
            log?.Invoke("Spell resolved.");
        }

        private static void AddPresentationOnlyHit(SpellAction action, SpellExecutionReport report)
        {
            if (action == null || action.SpellData == null || action.Target == null || report == null)
            {
                return;
            }

            SpellData spell = action.SpellData;
            if (spell.ImpactEffectPrefab == null && spell.AttackEffectSe == null)
            {
                return;
            }

            report.hits.Add(new ResolvedHitInfo(action.Target, 0, spell.ImpactEffectPrefab, spell.AttackEffectSe, false, true));
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

            if (attackGain > 0) log?.Invoke("Attack increased.");
            if (defenseGain > 0) log?.Invoke("Defense increased.");
            if (maxHpGain > 0) log?.Invoke("Max HP increased.");
            if (maxMpGain > 0) log?.Invoke("Max MP increased.");
        }

        private static void ApplyModifiers(SpellData spell, BattleUnit caster, BattleUnit target, Action<string> log)
        {
            if (spell.SelfAttackStatDelta != 0)
            {
                caster.ApplyStatModifier(StatType.Attack, spell.SelfAttackStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (spell.SelfDefenseStatDelta != 0)
            {
                caster.ApplyStatModifier(StatType.Defense, spell.SelfDefenseStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (spell.TargetAttackStatDelta != 0)
            {
                target.ApplyStatModifier(StatType.Attack, spell.TargetAttackStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (spell.TargetDefenseStatDelta != 0)
            {
                target.ApplyStatModifier(StatType.Defense, spell.TargetDefenseStatDelta, spell.StatModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (!Mathf.Approximately(spell.SelfAttackCoefficientDelta, 0f))
            {
                caster.ApplyCoefficientModifier(CoefficientModifierType.AttackCoefficientAdd, spell.SelfAttackCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (!Mathf.Approximately(spell.SelfDefenseCoefficientDelta, 0f))
            {
                caster.ApplyCoefficientModifier(CoefficientModifierType.DefenseCoefficientAdd, spell.SelfDefenseCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (!Mathf.Approximately(spell.TargetAttackCoefficientDelta, 0f))
            {
                target.ApplyCoefficientModifier(CoefficientModifierType.AttackCoefficientAdd, spell.TargetAttackCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }

            if (!Mathf.Approximately(spell.TargetDefenseCoefficientDelta, 0f))
            {
                target.ApplyCoefficientModifier(CoefficientModifierType.DefenseCoefficientAdd, spell.TargetDefenseCoefficientDelta, spell.CoefficientModifierDurationTurns);
                log?.Invoke("Spell resolved.");
            }
        }

        private static bool CanCastByEffects(IReadOnlyList<SpellEffectBase> effects, SpellEffectContext context, Action<string> log)
        {
            if (effects == null)
            {
                return true;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                SpellEffectBase effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                if (!effect.CanCast(context, out string reason))
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        log?.Invoke(reason);
                    }
                    return false;
                }
            }

            return true;
        }

        private static bool CanCastByStageRules(
            IReadOnlyList<StageRuleBase> stageRules,
            BattleManager battleManager,
            SpellAction action,
            SpellExecutionReport report,
            Action<string> log)
        {
            if (stageRules == null || battleManager == null)
            {
                return true;
            }

            StageRuleContext context = CreateStageRuleContext(battleManager, action, report, log);
            for (int i = 0; i < stageRules.Count; i++)
            {
                StageRuleBase rule = stageRules[i];
                if (rule == null)
                {
                    continue;
                }

                if (!rule.CanCast(context, out string reason))
                {
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        log?.Invoke(reason);
                    }
                    return false;
                }
            }

            return true;
        }

        private static void NotifyBeforeResolve(IReadOnlyList<SpellEffectBase> effects, SpellEffectContext context)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.BeforeResolve(context);
            }
        }

        private static void NotifyBeforeDamageCalculation(IReadOnlyList<SpellEffectBase> effects, SpellEffectContext context)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.BeforeDamageCalculation(context);
            }
        }

        private static void NotifyAfterSpellResolved(IReadOnlyList<SpellEffectBase> effects, SpellEffectContext context, SpellExecutionReport report)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.AfterSpellResolved(context, report);
            }
        }

        private static void NotifyBeforeStageRules(
            IReadOnlyList<StageRuleBase> stageRules,
            BattleManager battleManager,
            SpellAction action,
            SpellExecutionReport report,
            Action<string> log)
        {
            if (stageRules == null || battleManager == null)
            {
                return;
            }

            StageRuleContext context = CreateStageRuleContext(battleManager, action, report, log);
            for (int i = 0; i < stageRules.Count; i++)
            {
                stageRules[i]?.BeforeSpellResolved(context);
            }
        }

        private static void NotifyAfterStageRules(
            IReadOnlyList<StageRuleBase> stageRules,
            BattleManager battleManager,
            SpellAction action,
            SpellExecutionReport report,
            Action<string> log)
        {
            if (stageRules == null || battleManager == null)
            {
                return;
            }

            StageRuleContext context = CreateStageRuleContext(battleManager, action, report, log);
            for (int i = 0; i < stageRules.Count; i++)
            {
                stageRules[i]?.AfterSpellResolved(context);
            }
        }

        private static StageRuleContext CreateStageRuleContext(
            BattleManager battleManager,
            SpellAction action,
            SpellExecutionReport report,
            Action<string> log)
        {
            return new StageRuleContext(
                battleManager,
                battleManager.CurrentStageData,
                battleManager.CurrentWaveData,
                battleManager.CurrentWaveIndex,
                action,
                report,
                log);
        }
    }
}
