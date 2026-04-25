using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    public class BattleManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleUnit playerUnit;
        [SerializeField] private BattleUnit enemyUnit;
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private BattleTuning tuning;
        [SerializeField] private DamageSequencePlayer damageSequencePlayer;
        [SerializeField] private ProjectionShifter projectionShifter;

        [Header("Player initial memory")]
        [SerializeField] private List<SpellData> playerStartingSpells = new List<SpellData>();

        [Header("Timing")]
        [SerializeField, Min(0f)] private float logIntervalSeconds = 0.35f;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip turnToEnemySe;
        [SerializeField] private AudioClip turnToPlayerSe;
        [SerializeField] private AudioClip memorySuccessSe;
        [SerializeField, Min(0f)] private float uiSeVolume = 1f;

        private readonly SpellAction[] playerQueuedActions = new SpellAction[3];
        private readonly List<SpellData> memoryCandidates = new List<SpellData>();
        private readonly SpellData[] lastEnemyUsedSpells = new SpellData[3];

        private bool isBusy;
        private int turnNumber = 1;
        private SpellData pendingEnemySpellCopy;
        private int pendingEnemySpellCopySourceSlot = -1;

        public BattleFlowState State { get; private set; } = BattleFlowState.Idle;
        public IReadOnlyList<SpellData> MemoryCandidates => memoryCandidates;
        public bool HasPendingEnemySpellCopy => pendingEnemySpellCopy != null;
        public SpellData PendingEnemySpellCopySpellData => pendingEnemySpellCopy;
        public int PendingEnemySpellCopySourceSlot => pendingEnemySpellCopySourceSlot;

        public event Action<string> OnBattleLog;
        public event Action<BattleFlowState> OnStateChanged;
        public event Action OnPlayerSelectionStarted;
        public event Action OnMemoryPhaseStarted;
        public event Action OnPlayerQueuedActionsChanged;
        public event Action OnPlayerMemoryChanged;
        public event Action OnEnemyLastUsedSpellsChanged;
        public event Action OnPendingEnemySpellCopyChanged;

        private void Start()
        {
            StartBattle();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }
        }

        public void StartBattle()
        {
            if (playerUnit == null || enemyUnit == null || enemyAI == null || tuning == null)
            {
                Debug.LogError("BattleManager: 参照が不足しています。");
                return;
            }

            turnNumber = 1;
            isBusy = false;
            memoryCandidates.Clear();
            ClearAllPlayerQueuedActions(false);
            ClearLastEnemyUsedSpells(false);
            ClearPendingEnemySpellCopy(false);

            playerUnit.InitializeForBattle();
            enemyUnit.InitializeForBattle();

            if (playerUnit.MemoryBook.RememberedSpells.Count == 0)
            {
                for (int i = 0; i < playerStartingSpells.Count; i++)
                {
                    playerUnit.MemoryBook.LearnSpell(playerStartingSpells[i], false);
                }
            }

            if (projectionShifter != null)
            {
                projectionShifter.SnapToPlayerTurn();
            }

            NotifyPlayerMemoryChanged();
            NotifyPlayerQueuedActionsChanged();
            NotifyEnemyLastUsedSpellsChanged();
            NotifyPendingEnemySpellCopyChanged();

            Log("バトル開始。");
            BeginPlayerTurn();
        }

        public IReadOnlyList<RememberedSpell> GetPlayerRememberedSpells()
        {
            return playerUnit.MemoryBook.RememberedSpells;
        }

        public int GetPlayerRememberedSpellCount()
        {
            return playerUnit != null ? playerUnit.MemoryBook.Count : 0;
        }

        public RememberedSpell GetPlayerRememberedSpellAt(int memoryIndex)
        {
            IReadOnlyList<RememberedSpell> spells = GetPlayerRememberedSpells();
            if (spells == null || memoryIndex < 0 || memoryIndex >= spells.Count)
            {
                return null;
            }

            return spells[memoryIndex];
        }

        public bool IsPlayerMemoryFull()
        {
            return playerUnit != null && playerUnit.MemoryBook.IsFull;
        }

        public SpellAction GetPlayerQueuedAction(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerQueuedActions.Length)
            {
                return null;
            }

            return playerQueuedActions[slotIndex];
        }

        public SpellData GetQueuedPlayerSpellData(int slotIndex)
        {
            SpellAction action = GetPlayerQueuedAction(slotIndex);
            return action != null ? action.SpellData : null;
        }

        public SpellData GetLastEnemyUsedSpellData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= lastEnemyUsedSpells.Length)
            {
                return null;
            }

            return lastEnemyUsedSpells[slotIndex];
        }

        public int GetNextEmptyPlayerActionSlot()
        {
            for (int i = 0; i < playerQueuedActions.Length; i++)
            {
                if (playerQueuedActions[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public string GetQueuedActionSummary()
        {
            string s1 = GetQueuedPlayerSpellData(0) != null ? GetQueuedPlayerSpellData(0).DisplayName : "-";
            string s2 = GetQueuedPlayerSpellData(1) != null ? GetQueuedPlayerSpellData(1).DisplayName : "-";
            string s3 = GetQueuedPlayerSpellData(2) != null ? GetQueuedPlayerSpellData(2).DisplayName : "-";
            return $"[1] {s1} / [2] {s2} / [3] {s3}";
        }

        public bool TryQueuePlayerSpell(int slotIndex, RememberedSpell rememberedSpell, BattleUnit explicitTarget = null)
        {
            if (State != BattleFlowState.PlayerSelection || isBusy)
            {
                Debug.Log("TryQueuePlayerSpell: 今はプレイヤー選択中ではないか、処理中です。");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= playerQueuedActions.Length)
            {
                Debug.LogWarning($"TryQueuePlayerSpell: slotIndex が範囲外です。 slotIndex={slotIndex}, length={playerQueuedActions.Length}");
                return false;
            }

            if (rememberedSpell == null || rememberedSpell.SpellData == null)
            {
                Debug.LogWarning("TryQueuePlayerSpell: rememberedSpell か SpellData が null です。");
                return false;
            }

            SpellData spell = rememberedSpell.SpellData;
            if (!CanQueuePlayerSpell(slotIndex, rememberedSpell, out string reason))
            {
                Log(reason);
                return false;
            }

            BattleUnit target = explicitTarget;
            if (spell.TargetType == TargetType.Self)
            {
                target = playerUnit;
            }
            else if (target == null)
            {
                target = enemyUnit;
            }

            playerQueuedActions[slotIndex] = new SpellAction(playerUnit, target, rememberedSpell, slotIndex);
            Log($"プレイヤー行動{slotIndex + 1}に {spell.DisplayName} をセット。");
            NotifyPlayerQueuedActionsChanged();
            Debug.Log($"現在の選択魔法: {GetQueuedActionSummary()}");

            if (AreAllPlayerActionsReady())
            {
                isBusy = true;
                StartCoroutine(RunPlayerAndEnemyTurn());
            }

            return true;
        }

        public bool CanQueuePlayerSpell(int slotIndex, RememberedSpell rememberedSpell, out string reason)
        {
            reason = string.Empty;

            if (playerUnit == null)
            {
                reason = "プレイヤーユニットが未設定です。";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= playerQueuedActions.Length)
            {
                reason = $"行動スロット {slotIndex + 1} は存在しません。";
                return false;
            }

            if (rememberedSpell == null || rememberedSpell.SpellData == null)
            {
                reason = "魔法データが不正です。";
                return false;
            }

            SpellData spell = rememberedSpell.SpellData;
            if (!spell.IsValidForOrder(slotIndex))
            {
                reason = $"{spell.DisplayName} は {slotIndex + 1}番目には使えません。";
                return false;
            }

            int reservedMp = GetQueuedPlayerMpCostExcluding(slotIndex);
            int requiredMp = reservedMp + Mathf.Max(0, spell.MpCost);
            if (playerUnit.CurrentMP < requiredMp)
            {
                reason = $"MPが足りません。必要MP: {requiredMp} / 現在MP: {playerUnit.CurrentMP}";
                return false;
            }

            return true;
        }

        private int GetQueuedPlayerMpCostExcluding(int excludedSlotIndex)
        {
            int sum = 0;
            for (int i = 0; i < playerQueuedActions.Length; i++)
            {
                if (i == excludedSlotIndex)
                {
                    continue;
                }

                SpellAction action = playerQueuedActions[i];
                if (action != null && action.SpellData != null)
                {
                    sum += Mathf.Max(0, action.SpellData.MpCost);
                }
            }

            return sum;
        }

        public void ClearPlayerSpell(int slotIndex)
        {
            if (State != BattleFlowState.PlayerSelection || isBusy)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= playerQueuedActions.Length)
            {
                return;
            }

            playerQueuedActions[slotIndex] = null;
            NotifyPlayerQueuedActionsChanged();
            Debug.Log($"現在の選択魔法: {GetQueuedActionSummary()}");
        }

        public bool CancelLastPlayerQueuedSpell()
        {
            if (State != BattleFlowState.PlayerSelection || isBusy)
            {
                return false;
            }

            for (int i = playerQueuedActions.Length - 1; i >= 0; i--)
            {
                SpellAction action = playerQueuedActions[i];
                if (action == null)
                {
                    continue;
                }

                string spellName = action.SpellData != null ? action.SpellData.DisplayName : "(null)";
                ClearPlayerSpell(i);
                Log($"プレイヤー行動{i + 1}の {spellName} をキャンセル。");
                return true;
            }

            return false;
        }

        private void HandleEscapeKey()
        {
            if (State != BattleFlowState.PlayerSelection || isBusy)
            {
                return;
            }

            if (HasPendingEnemySpellCopy)
            {
                string spellName = pendingEnemySpellCopy != null ? pendingEnemySpellCopy.DisplayName : "(null)";
                CancelPendingEnemySpellCopy();
                Log($"敵魔法コピー待ちをキャンセル。 対象: {spellName}");
                return;
            }

            CancelLastPlayerQueuedSpell();
        }

        public void ClearAllPlayerQueuedActions(bool notify = true)
        {
            Array.Clear(playerQueuedActions, 0, playerQueuedActions.Length);
            if (notify)
            {
                NotifyPlayerQueuedActionsChanged();
            }
        }

        /// <summary>
        /// プレイヤーターン中、敵が前ターンに使った魔法を自分の記憶枠へコピーする。
        /// 空きがあれば一番若い空き番号へ自動で入れる。
        /// 満杯なら置き換え待ち状態に入る。
        /// </summary>
        public bool TryBeginEnemySpellCopy(int enemySlotIndex)
        {
            if (isBusy || State != BattleFlowState.PlayerSelection)
            {
                Debug.Log("TryBeginEnemySpellCopy: 今は敵魔法コピーを行えるタイミングではありません。");
                return false;
            }

            SpellData sourceSpell = GetLastEnemyUsedSpellData(enemySlotIndex);
            if (sourceSpell == null)
            {
                Debug.LogWarning($"TryBeginEnemySpellCopy: 敵スロット {enemySlotIndex + 1} に魔法がありません。");
                return false;
            }

            int existingIndex = playerUnit.MemoryBook.IndexOf(sourceSpell);
            if (existingIndex >= 0)
            {
                ClearPendingEnemySpellCopy(true);
                NotifyPlayerMemoryChanged();
                Log($"{sourceSpell.DisplayName} は既に記憶スロット {existingIndex + 1} にあります。新しくは追加されません。");
                return true;
            }

            if (!playerUnit.MemoryBook.IsFull)
            {
                int beforeCount = playerUnit.MemoryBook.Count;
                bool success = playerUnit.MemoryBook.TryLearnSpellInFirstEmptySlot(
                    sourceSpell,
                    out RememberedSpell result,
                    out int targetIndex,
                    out bool alreadyKnown,
                    false);

                if (!success)
                {
                    Debug.LogWarning($"TryBeginEnemySpellCopy: {sourceSpell.DisplayName} の記憶に失敗しました。");
                    return false;
                }

                int afterCount = playerUnit.MemoryBook.Count;
                NotifyPlayerMemoryChanged();
                ClearPendingEnemySpellCopy(true);

                if (alreadyKnown)
                {
                    Log($"{sourceSpell.DisplayName} は既に記憶済みです。新しくは追加されません。 (Count {beforeCount} -> {afterCount})");
                }
                else
                {
                    PlayUiSe(memorySuccessSe);
                    Log($"{sourceSpell.DisplayName} を記憶スロット {targetIndex + 1} にコピーした");
                }

                return true;
            }

            pendingEnemySpellCopy = sourceSpell;
            pendingEnemySpellCopySourceSlot = enemySlotIndex;
            NotifyPendingEnemySpellCopyChanged();
            Log($"記憶スロットが満杯です。{sourceSpell.DisplayName} を入れ替えたい自分のスロットをクリックしてください。");
            return true;
        }

        /// <summary>
        /// 満杯時に選ばれた敵魔法を、指定した自分の記憶スロットへ上書きする。
        /// </summary>
        public bool TryResolvePendingEnemySpellCopyToMemorySlot(int memoryIndex)
        {
            if (pendingEnemySpellCopy == null)
            {
                return false;
            }

            if (playerUnit == null)
            {
                return false;
            }

            RememberedSpell oldSpell = GetPlayerRememberedSpellAt(memoryIndex);
            string oldName = oldSpell != null && oldSpell.SpellData != null ? oldSpell.SpellData.DisplayName : "(空)";
            string newName = pendingEnemySpellCopy.DisplayName;

            bool success = playerUnit.MemoryBook.TryReplaceSpellAt(
                memoryIndex,
                pendingEnemySpellCopy,
                out RememberedSpell result,
                out bool alreadyKnown,
                false);

            if (!success)
            {
                Debug.LogWarning($"TryResolvePendingEnemySpellCopyToMemorySlot: memoryIndex が不正です。 memoryIndex={memoryIndex}");
                return false;
            }

            NotifyPlayerMemoryChanged();
            ClearPendingEnemySpellCopy(true);

            if (alreadyKnown)
            {
                int existingIndex = result != null ? playerUnit.MemoryBook.IndexOf(result.SpellData) : -1;
                Log($"{newName} は既に記憶スロット {existingIndex + 1} にあります。入れ替えは行いません。");
                return true;
            }

            PlayUiSe(memorySuccessSe);
            Log($"記憶スロット {memoryIndex + 1} を {oldName} から {newName} に入れ替えた。");
            return true;
        }

        public void CancelPendingEnemySpellCopy()
        {
            ClearPendingEnemySpellCopy(true);
        }

        /// <summary>
        /// 旧・記憶フェーズ用。現在は使用しない。
        /// </summary>
        public void ConfirmMemoryChoices(List<SpellData> selectedSpells)
        {
            Debug.LogWarning("ConfirmMemoryChoices: 現在の仕様では記憶フェーズは使っていません。敵魔法スロットをプレイヤーターン中にクリックしてください。");
        }

        private void BeginPlayerTurn()
        {
            ClearAllPlayerQueuedActions(true);
            ClearPendingEnemySpellCopy(true);
            memoryCandidates.Clear();

            if (TryApplyTurnStartEffects(playerUnit))
            {
                if (playerUnit.IsDead)
                {
                    SetState(BattleFlowState.Defeat);
                    Log("プレイヤーは倒れた。");
                    return;
                }
            }

            SetState(BattleFlowState.PlayerSelection);
            Log($"--- ターン {turnNumber} / プレイヤーターン ---");
            Debug.Log($"現在の選択魔法: {GetQueuedActionSummary()}");
            OnPlayerSelectionStarted?.Invoke();
        }

        private IEnumerator RunPlayerAndEnemyTurn()
        {
            SetState(BattleFlowState.PlayerExecution);

            yield return ExecuteSequence(playerQueuedActions);
            playerUnit.AdvanceOwnTurnEnd();

            ClearAllPlayerQueuedActions(true);
            Debug.Log($"現在の選択魔法: {GetQueuedActionSummary()}");

            if (enemyUnit.IsDead)
            {
                SetState(BattleFlowState.Victory);
                Log("勝利！");
                isBusy = false;
                yield break;
            }

            PlayUiSe(turnToEnemySe);
            if (projectionShifter != null)
            {
                yield return projectionShifter.ShiftToEnemyTurnRoutine();
            }

            if (TryApplyTurnStartEffects(enemyUnit))
            {
                if (enemyUnit.IsDead)
                {
                    SetState(BattleFlowState.Victory);
                    Log("勝利！");
                    isBusy = false;
                    yield break;
                }
            }

            SetState(BattleFlowState.EnemyExecution);
            Log("--- 敵ターン ---");

            List<SpellAction> enemyActions = enemyAI.BuildTurnActions(enemyUnit, playerUnit);
            CacheEnemyLastUsedSpells(enemyActions);

            memoryCandidates.Clear();
            for (int i = 0; i < enemyActions.Count; i++)
            {
                if (enemyActions[i] != null && enemyActions[i].SpellData != null)
                {
                    memoryCandidates.Add(enemyActions[i].SpellData);
                }
            }

            if (enemyActions.Count == 0)
            {
                Log("敵は行動できなかった。");
                Debug.LogWarning("BattleManager: enemyActions が 0 件です。Enemy Spell Pool や順番制約を確認してください。");
            }

            yield return ExecuteSequence(enemyActions);
            enemyUnit.AdvanceOwnTurnEnd();

            if (playerUnit.IsDead)
            {
                SetState(BattleFlowState.Defeat);
                Log("敗北……");
                isBusy = false;
                yield break;
            }

            turnNumber++;

            PlayUiSe(turnToPlayerSe);
            if (projectionShifter != null)
            {
                yield return projectionShifter.ShiftToPlayerTurnRoutine();
            }

            BeginPlayerTurn();
            isBusy = false;
        }

        private IEnumerator ExecuteSequence(IList<SpellAction> actions)
        {
            float carryToNextSpellAttackBonus = 0f;

            for (int i = 0; i < actions.Count; i++)
            {
                SpellAction action = actions[i];
                if (action == null)
                {
                    continue;
                }

                SpellExecutionReport report = SpellResolver.Execute(
                    action,
                    tuning,
                    carryToNextSpellAttackBonus,
                    Log);

                if (action.Caster == playerUnit && action.RememberedSpell != null)
                {
                    NotifyPlayerMemoryChanged();
                }

                carryToNextSpellAttackBonus = report.grantNextSpellAttackCoefficientBonus;

                yield return PlayHitPresentation(report);

                if (playerUnit.IsDead || enemyUnit.IsDead)
                {
                    yield return new WaitForSeconds(logIntervalSeconds);
                    yield break;
                }

                yield return new WaitForSeconds(logIntervalSeconds);
            }
        }

        private IEnumerator PlayHitPresentation(SpellExecutionReport report)
        {
            if (damageSequencePlayer == null || report == null || report.hits == null || report.hits.Count == 0)
            {
                yield break;
            }

            if (report.hits.Count == 1)
            {
                ResolvedHitInfo hit = report.hits[0];
                if (hit.target != null && hit.target.DamageTargetAnchor != null)
                {
                    yield return damageSequencePlayer.PlayHitRoutine(
                        hit.target.DamageTargetAnchor,
                        hit.damage,
                        GetShakeTargetTypeForHit(hit),
                        hit.effectPrefab,
                        hit.effectSe,
                        hit.isHealing);
                }

                yield break;
            }

            List<DamageSequencePlayer.DamageHitRequest> requests = new List<DamageSequencePlayer.DamageHitRequest>();
            for (int i = 0; i < report.hits.Count; i++)
            {
                ResolvedHitInfo hit = report.hits[i];
                if (hit.target != null && hit.target.DamageTargetAnchor != null)
                {
                    requests.Add(new DamageSequencePlayer.DamageHitRequest
                    {
                        target = hit.target.DamageTargetAnchor,
                        damage = hit.damage,
                        effectPrefab = hit.effectPrefab,
                        effectSe = hit.effectSe,
                        shakeTargetType = GetShakeTargetTypeForHit(hit),
                        suppressHitSound = hit.isHealing,
                    });
                }
            }

            if (requests.Count > 0)
            {
                yield return damageSequencePlayer.PlayMultiHitRoutine(requests);
            }
        }

        private DamageSequencePlayer.ShakeTargetType GetShakeTargetTypeForHit(ResolvedHitInfo hit)
        {
            if (hit == null || hit.target == null || hit.isHealing)
            {
                return DamageSequencePlayer.ShakeTargetType.None;
            }

            if (hit.target == playerUnit)
            {
                return DamageSequencePlayer.ShakeTargetType.Player;
            }

            if (hit.target == enemyUnit)
            {
                return DamageSequencePlayer.ShakeTargetType.Enemy;
            }

            return DamageSequencePlayer.ShakeTargetType.None;
        }

        private void CacheEnemyLastUsedSpells(IList<SpellAction> enemyActions)
        {
            ClearLastEnemyUsedSpells(false);

            if (enemyActions != null)
            {
                for (int i = 0; i < lastEnemyUsedSpells.Length; i++)
                {
                    if (i >= enemyActions.Count)
                    {
                        break;
                    }

                    SpellAction action = enemyActions[i];
                    if (action != null)
                    {
                        lastEnemyUsedSpells[i] = action.SpellData;
                    }
                }
            }

            NotifyEnemyLastUsedSpellsChanged();
        }

        private void ClearLastEnemyUsedSpells(bool notify)
        {
            Array.Clear(lastEnemyUsedSpells, 0, lastEnemyUsedSpells.Length);
            if (notify)
            {
                NotifyEnemyLastUsedSpellsChanged();
            }
        }

        private void ClearPendingEnemySpellCopy(bool notify)
        {
            pendingEnemySpellCopy = null;
            pendingEnemySpellCopySourceSlot = -1;
            if (notify)
            {
                NotifyPendingEnemySpellCopyChanged();
            }
        }

        private bool TryApplyTurnStartEffects(BattleUnit unit)
        {
            int fireStacks = unit.GetElementStacks(ElementType.Fire);
            int dotDamage = tuning.GetFireDotDamage(fireStacks);
            if (dotDamage <= 0)
            {
                return false;
            }

            unit.TakeDamage(dotDamage);
            Log($"{unit.UnitName} は火傷で {dotDamage} ダメージを受けた。");
            return true;
        }

        private bool AreAllPlayerActionsReady()
        {
            for (int i = 0; i < playerQueuedActions.Length; i++)
            {
                if (playerQueuedActions[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void NotifyPlayerQueuedActionsChanged()
        {
            OnPlayerQueuedActionsChanged?.Invoke();
        }

        private void NotifyPlayerMemoryChanged()
        {
            OnPlayerMemoryChanged?.Invoke();
        }

        private void NotifyEnemyLastUsedSpellsChanged()
        {
            OnEnemyLastUsedSpellsChanged?.Invoke();
        }

        private void NotifyPendingEnemySpellCopyChanged()
        {
            OnPendingEnemySpellCopyChanged?.Invoke();
        }

        private void PlayUiSe(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(clip, uiSeVolume);
                return;
            }

            Vector3 playPosition = transform != null ? transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, playPosition, uiSeVolume);
        }

        private void SetState(BattleFlowState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);
        }

        private void Log(string message)
        {
            Debug.Log(message);
            OnBattleLog?.Invoke(message);
        }
    }
}
