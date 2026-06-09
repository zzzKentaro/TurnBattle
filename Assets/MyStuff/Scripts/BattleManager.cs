using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TurnBasedBattle
{
    /// <summary>
    /// バトルの進行状況やターン管琁E��E��法アクションのキュー管琁E��行うゲームのメインシスチE��、E
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleUnit playerUnit;
        [SerializeField] private BattleUnit enemyUnit;
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private BattleTuning tuning;
        [SerializeField] private DamageSequencePlayer damageSequencePlayer;
        [SerializeField] private ProjectionShifter projectionShifter;
        [SerializeField] private SpellEffectCatalog spellEffectCatalog;

        [Header("Player initial memory")]
        [SerializeField] private bool startBattleOnStart = true;
        [SerializeField] private List<SpellData> playerStartingSpells = new List<SpellData>();

        [Header("Timing")]
        [SerializeField, Min(0f)] private float logIntervalSeconds = 0.35f;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip turnToEnemySe;
        [SerializeField] private AudioClip turnToPlayerSe;
        [SerializeField] private AudioClip memorySuccessSe;
        [SerializeField, Min(0f)] private float uiSeVolume = 1f;

        [Header("BGM")]
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField, Min(0f)] private float bgmVolume = 1f;
        [SerializeField] private bool stopBgmWhenWaveHasNoClip;

        private readonly SpellAction[] playerQueuedActions = new SpellAction[3];
        private readonly List<SpellData> memoryCandidates = new List<SpellData>();
        private readonly SpellData[] lastEnemyUsedSpells = new SpellData[3];

        private bool isBusy;
        private int turnNumber = 1;
        private BattleUnit defaultEnemyUnit;
        private BattleUnit spawnedWaveEnemyUnit;
        private SpellData pendingEnemySpellCopy;
        private int pendingEnemySpellCopySourceSlot = -1;
        private bool skipDefaultStartingSpellsOnce;
        private bool battleResultNotified;
        private readonly List<SpellData> pendingWavePlayerSpells = new List<SpellData>();
        private readonly List<SpellData> pendingInitialEnemyLastUsedSpells = new List<SpellData>();
        private readonly List<StageRuleBase> activeStageRules = new List<StageRuleBase>();
        private string pendingWaveStartLogMessage;
        private BattleStageData currentStageData;
        private BattleWaveData currentWaveData;
        private int currentWaveIndex = -1;

        public BattleFlowState State { get; private set; } = BattleFlowState.Idle;
        public int CurrentTurnNumber => turnNumber;
        public BattleStageData CurrentStageData => currentStageData;
        public BattleWaveData CurrentWaveData => currentWaveData;
        public int CurrentWaveIndex => currentWaveIndex;
        public BattleUnit CurrentPlayerUnit => playerUnit;
        public BattleUnit CurrentEnemyUnit => enemyUnit;
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
        public event Action<BattleUnit> OnEnemyUnitChanged;
        public event Action OnPendingEnemySpellCopyChanged;
        public event Action OnBattleWon;
        public event Action OnBattleLost;

        private void Awake()
        {
            defaultEnemyUnit = enemyUnit;
        }

        private void Start()
        {
            if (startBattleOnStart)
            {
                StartBattle();
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClickCancel();
            }
        }

        public void StartBattle()
        {
            if (playerUnit == null || enemyUnit == null || enemyAI == null || tuning == null)
            {
                Debug.LogError("BattleManager: required references are missing.");
                return;
            }

            turnNumber = 1;
            isBusy = false;
            battleResultNotified = false;
            NotifyEnemyUnitChanged();
            memoryCandidates.Clear();
            ClearAllPlayerQueuedActions(false);
            ClearLastEnemyUsedSpells(false);
            ClearPendingEnemySpellCopy(false);

            playerUnit.InitializeForBattle();
            enemyUnit.InitializeForBattle();
            enemyAI.ResetBattleState();

            if (!skipDefaultStartingSpellsOnce && playerUnit.MemoryBook.RememberedSpells.Count == 0)
            {
                for (int i = 0; i < playerStartingSpells.Count; i++)
                {
                    playerUnit.MemoryBook.LearnSpell(playerStartingSpells[i], false);
                }
            }

            if (pendingWavePlayerSpells.Count > 0)
            {
                playerUnit.LearnSpells(pendingWavePlayerSpells, false);
                pendingWavePlayerSpells.Clear();
            }

            ApplyInitialEnemyLastUsedSpells();

            if (projectionShifter != null)
            {
                projectionShifter.SnapToPlayerTurn();
            }

            NotifyPlayerMemoryChanged();
            NotifyPlayerQueuedActionsChanged();
            NotifyEnemyLastUsedSpellsChanged();
            NotifyPendingEnemySpellCopyChanged();

            Log("Battle log.");
            if (!string.IsNullOrWhiteSpace(pendingWaveStartLogMessage))
            {
                Log("Battle log.");
                pendingWaveStartLogMessage = null;
            }

            skipDefaultStartingSpellsOnce = false;
            BeginPlayerTurn();
        }

        public void StartWaveBattle(BattleWaveData waveData)
        {
            StartWaveBattle(waveData, currentStageData, currentWaveIndex);
        }

        public void StartWaveBattle(BattleWaveData waveData, BattleStageData stageData, int waveIndex)
        {
            if (waveData == null)
            {
                Debug.LogWarning("BattleManager warning.", this);
                return;
            }

            currentStageData = stageData;
            currentWaveData = waveData;
            currentWaveIndex = waveIndex;

            PlayWaveBgm(waveData);

            enemyUnit = ResolveEnemyUnitForWave(waveData);
            if (projectionShifter != null && enemyUnit != null)
            {
                projectionShifter.SetEnemyObject(enemyUnit.gameObject);
            }

            NotifyEnemyUnitChanged();

            List<SpellData> enemySpellsForWave = new List<SpellData>();
            AddNonNullUnique(enemySpellsForWave, waveData.EnemySpells);
            AddEnemyProfileSpells(enemySpellsForWave, waveData.EnemyAIProfile);

            if (enemyUnit != null)
            {
                enemyUnit.SetEnemySpellsForWave(enemySpellsForWave, true);
            }

            if (enemyAI != null)
            {
                enemyAI.SetProfile(waveData.EnemyAIProfile);
            }

            pendingWavePlayerSpells.Clear();
            AddNonNull(pendingWavePlayerSpells, waveData.PlayerSpellsToAddOnStart);

            pendingInitialEnemyLastUsedSpells.Clear();
            AddNonNull(pendingInitialEnemyLastUsedSpells, waveData.InitialEnemyLastUsedSpells);
            pendingWaveStartLogMessage = !string.IsNullOrWhiteSpace(waveData.WaveStartLogMessage)
                ? waveData.WaveStartLogMessage
                : waveData.WaveDisplayName;

            skipDefaultStartingSpellsOnce = true;
            StartBattle();
        }

        private BattleUnit ResolveEnemyUnitForWave(BattleWaveData waveData)
        {
            BattleUnit overrideUnit = waveData != null ? waveData.EnemyUnitOverride : null;
            if (overrideUnit == null)
            {
                return UseDefaultEnemyUnit();
            }

            if (overrideUnit == defaultEnemyUnit || IsSceneObject(overrideUnit))
            {
                ClearSpawnedWaveEnemy();
                if (defaultEnemyUnit != null && overrideUnit != defaultEnemyUnit)
                {
                    defaultEnemyUnit.gameObject.SetActive(false);
                }

                overrideUnit.gameObject.SetActive(true);
                return overrideUnit;
            }

            return SpawnWaveEnemy(overrideUnit);
        }

        private BattleUnit UseDefaultEnemyUnit()
        {
            ClearSpawnedWaveEnemy();
            if (defaultEnemyUnit != null)
            {
                defaultEnemyUnit.gameObject.SetActive(true);
            }

            return defaultEnemyUnit;
        }

        private BattleUnit SpawnWaveEnemy(BattleUnit prefab)
        {
            ClearSpawnedWaveEnemy();
            if (defaultEnemyUnit == null)
            {
                Debug.LogWarning("BattleManager: default enemyUnit is not assigned, so enemyUnitOverride prefab cannot be placed.", this);
                return prefab;
            }

            Transform defaultTransform = defaultEnemyUnit.transform;
            BattleUnit instance = Instantiate(prefab, defaultTransform.parent);
            Transform instanceTransform = instance.transform;
            instanceTransform.SetSiblingIndex(defaultTransform.GetSiblingIndex());
            instanceTransform.localPosition = defaultTransform.localPosition;
            instanceTransform.localRotation = defaultTransform.localRotation;
            instanceTransform.localScale = defaultTransform.localScale;
            instance.name = prefab.name;

            defaultEnemyUnit.gameObject.SetActive(false);
            spawnedWaveEnemyUnit = instance;
            return instance;
        }

        private void ClearSpawnedWaveEnemy()
        {
            if (spawnedWaveEnemyUnit == null)
            {
                return;
            }

            Destroy(spawnedWaveEnemyUnit.gameObject);
            spawnedWaveEnemyUnit = null;
        }

        private static bool IsSceneObject(BattleUnit unit)
        {
            return unit != null && unit.gameObject.scene.IsValid();
        }

        public void ConfigureStageRules(BattleStageData stageData)
        {
            currentStageData = stageData;
            activeStageRules.Clear();

            if (stageData != null && stageData.StageRules != null)
            {
                for (int i = 0; i < stageData.StageRules.Count; i++)
                {
                    if (stageData.StageRules[i] != null)
                    {
                        activeStageRules.Add(stageData.StageRules[i]);
                    }
                }
            }

            StageRuleContext context = CreateStageRuleContext(null, null);
            for (int i = 0; i < activeStageRules.Count; i++)
            {
                activeStageRules[i].OnStageStarted(context);
            }
        }

        public void NotifyWaveRulesStarted(BattleWaveData waveData, int waveIndex)
        {
            currentWaveData = waveData;
            currentWaveIndex = waveIndex;

            StageRuleContext context = CreateStageRuleContext(null, null);
            for (int i = 0; i < activeStageRules.Count; i++)
            {
                activeStageRules[i].OnWaveStarted(context);
            }
        }

        public void PreparePlayerMemoryForStage(IReadOnlyList<SpellData> spells, bool clearMemory)
        {
            if (playerUnit == null)
            {
                Debug.LogWarning("BattleManager warning.", this);
                return;
            }

            if (clearMemory)
            {
                playerUnit.ClearMemory();
            }

            playerUnit.LearnSpells(spells, false);
            skipDefaultStartingSpellsOnce = true;
            NotifyPlayerMemoryChanged();
        }

        public void WriteBattleLog(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Log("Battle log.");
            }
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

        public bool HasPlayerRememberedSpell(SpellData spellData)
        {
            return playerUnit != null && playerUnit.MemoryBook.IndexOf(spellData) >= 0;
        }

        public bool CanCopyLastEnemyUsedSpell(int enemySlotIndex, out string reason)
        {
            reason = "Action is not available.";

            if (State != BattleFlowState.PlayerSelection || isBusy)
            {
                reason = "Action is not available.";
                return false;
            }

            if (playerUnit == null)
            {
                reason = "Action is not available.";
                return false;
            }

            SpellData sourceSpell = GetLastEnemyUsedSpellData(enemySlotIndex);
            if (sourceSpell == null)
            {
                reason = "Action is not available.";
                return false;
            }

            if (HasPlayerRememberedSpell(sourceSpell))
            {
                reason = "Action is not available.";
                return false;
            }

            if (playerUnit.MemoryBook.IsFull)
            {
                reason = "Action is not available.";
                return false;
            }

            return true;
        }

        public bool TryCopyLastEnemyUsedSpellToMemory(int enemySlotIndex)
        {
            if (!CanCopyLastEnemyUsedSpell(enemySlotIndex, out string reason))
            {
                Log("Battle log.");
                return false;
            }

            SpellData sourceSpell = GetLastEnemyUsedSpellData(enemySlotIndex);
            bool success = playerUnit.MemoryBook.TryLearnSpellInFirstEmptySlot(
                sourceSpell,
                out _,
                out int targetIndex,
                out bool alreadyKnown,
                false);

            if (!success || alreadyKnown)
            {
                Log("Battle log.");
                return false;
            }

            PlayUiSe(memorySuccessSe);
            ClearPendingEnemySpellCopy(true);
            NotifyPlayerMemoryChanged();
            Log("Battle log.");
            return true;
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
                Debug.Log("BattleManager debug.");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= playerQueuedActions.Length)
            {
                Debug.LogWarning("BattleManager warning.");
                return false;
            }

            if (rememberedSpell == null || rememberedSpell.SpellData == null)
            {
                Debug.LogWarning("BattleManager warning.");
                return false;
            }

            SpellData spell = rememberedSpell.SpellData;
            if (!CanQueuePlayerSpell(slotIndex, rememberedSpell, out string reason))
            {
                Log("Battle log.");
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
            Log("Battle log.");
            NotifyPlayerQueuedActionsChanged();
            Debug.Log("BattleManager debug.");

            if (AreAllPlayerActionsReady())
            {
                isBusy = true;
                StartCoroutine(RunPlayerAndEnemyTurn());
            }

            return true;
        }

        public bool CanQueuePlayerSpell(int slotIndex, RememberedSpell rememberedSpell, out string reason)
        {
            reason = "Action is not available.";

            if (playerUnit == null)
            {
                reason = "Action is not available.";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= playerQueuedActions.Length)
            {
                reason = "Action is not available.";
                return false;
            }

            if (rememberedSpell == null || rememberedSpell.SpellData == null)
            {
                reason = "Action is not available.";
                return false;
            }

            SpellData spell = rememberedSpell.SpellData;
            if (!spell.IsValidForOrder(slotIndex))
            {
                reason = "Action is not available.";
                return false;
            }

            int reservedMp = GetQueuedPlayerMpCostExcluding(slotIndex);
            int requiredMp = reservedMp + Mathf.Max(0, spell.MpCost);
            if (playerUnit.CurrentMP < requiredMp)
            {
                reason = "Action is not available.";
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
            Debug.Log("BattleManager debug.");
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
                Log("Battle log.");
                return true;
            }

            return false;
        }

        private void HandleRightClickCancel()
        {
            if (State != BattleFlowState.PlayerSelection || isBusy)
            {
                return;
            }

            if (HasPendingEnemySpellCopy)
            {
                string spellName = pendingEnemySpellCopy != null ? pendingEnemySpellCopy.DisplayName : "(null)";
                CancelPendingEnemySpellCopy();
                Log("Battle log.");
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
        /// プレイヤーターン中、敵が前ターンに使った魔法を自刁E�E記�E枠へコピ�Eする、E
        /// 空きがあれば一番若ぁE��き番号へ自動で入れる、E
        /// 満杯なら置き換え征E��状態に入る、E
        /// </summary>
        public bool TryBeginEnemySpellCopy(int enemySlotIndex)
        {
            if (isBusy || State != BattleFlowState.PlayerSelection)
            {
                Debug.Log("BattleManager debug.");
                return false;
            }

            SpellData sourceSpell = GetLastEnemyUsedSpellData(enemySlotIndex);
            if (sourceSpell == null)
            {
                Debug.LogWarning("BattleManager warning.");
                return false;
            }

            int existingIndex = playerUnit.MemoryBook.IndexOf(sourceSpell);
            if (existingIndex >= 0)
            {
                ClearPendingEnemySpellCopy(true);
                NotifyPlayerMemoryChanged();
                Log("Battle log.");
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
                    Debug.LogWarning("BattleManager warning.");
                    return false;
                }

                int afterCount = playerUnit.MemoryBook.Count;
                NotifyPlayerMemoryChanged();
                ClearPendingEnemySpellCopy(true);

                if (alreadyKnown)
                {
                    Log("Battle log.");
                }
                else
                {
                    PlayUiSe(memorySuccessSe);
                    Log("Battle log.");
                }

                return true;
            }

            pendingEnemySpellCopy = sourceSpell;
            pendingEnemySpellCopySourceSlot = enemySlotIndex;
            NotifyPendingEnemySpellCopyChanged();
            Log("Battle log.");
            return true;
        }

        /// <summary>
        /// 満杯時に選ばれた敵魔法を、指定した�E刁E�E記�EスロチE��へ上書きする、E
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
                Debug.LogWarning("BattleManager warning.");
                return false;
            }

            NotifyPlayerMemoryChanged();
            ClearPendingEnemySpellCopy(true);

            if (alreadyKnown)
            {
                int existingIndex = result != null ? playerUnit.MemoryBook.IndexOf(result.SpellData) : -1;
                Log("Battle log.");
                return true;
            }

            PlayUiSe(memorySuccessSe);
            Log("Battle log.");
            return true;
        }

        public void CancelPendingEnemySpellCopy()
        {
            ClearPendingEnemySpellCopy(true);
        }

        /// <summary>
        /// 旧・記�Eフェーズ用。現在は使用しなぁE��E
        /// </summary>
        public void ConfirmMemoryChoices(List<SpellData> selectedSpells)
        {
            Debug.LogWarning("BattleManager warning.");
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
                    Log("Battle log.");
                    return;
                }
            }

            SetState(BattleFlowState.PlayerSelection);
            Log("Battle log.");
            OnPlayerSelectionStarted?.Invoke();
        }

        private IEnumerator RunPlayerAndEnemyTurn()
        {
            SetState(BattleFlowState.PlayerExecution);

            yield return ExecuteSequence(playerQueuedActions);
            playerUnit.AdvanceOwnTurnEnd();

            ClearAllPlayerQueuedActions(true);
            Debug.Log("BattleManager debug.");

            if (enemyUnit.IsDead)
            {
                SetState(BattleFlowState.Victory);
                Log("Battle log.");
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
                    Log("Battle log.");
                    isBusy = false;
                    yield break;
                }
            }

            SetState(BattleFlowState.EnemyExecution);
            Log("Battle log.");

            List<SpellAction> enemyActions = enemyAI.BuildTurnActions(enemyUnit, playerUnit, turnNumber);
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
                Log("Battle log.");
                Debug.LogWarning("BattleManager warning.");
            }

            yield return ExecuteSequence(enemyActions);
            enemyUnit.AdvanceOwnTurnEnd();

            if (playerUnit.IsDead)
            {
                SetState(BattleFlowState.Defeat);
                Log("Battle log.");
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
                    Log,
                    this,
                    spellEffectCatalog,
                    activeStageRules);

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
                        hit.isHealing,
                        hit.suppressPopup,
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
                        suppressHitSound = hit.isHealing || hit.suppressPopup,
                        suppressPopup = hit.suppressPopup,
                        isHealing = hit.isHealing,
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

        private void ApplyInitialEnemyLastUsedSpells()
        {
            if (pendingInitialEnemyLastUsedSpells.Count == 0)
            {
                return;
            }

            ClearLastEnemyUsedSpells(false);
            for (int i = 0; i < lastEnemyUsedSpells.Length && i < pendingInitialEnemyLastUsedSpells.Count; i++)
            {
                lastEnemyUsedSpells[i] = pendingInitialEnemyLastUsedSpells[i];
            }

            pendingInitialEnemyLastUsedSpells.Clear();
        }

        private static void AddNonNull(List<SpellData> destination, IReadOnlyList<SpellData> source)
        {
            if (destination == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    destination.Add(source[i]);
                }
            }
        }

        private static void AddNonNullUnique(List<SpellData> destination, IReadOnlyList<SpellData> source)
        {
            if (destination == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                SpellData spell = source[i];
                if (spell != null && !destination.Contains(spell))
                {
                    destination.Add(spell);
                }
            }
        }

        private static void AddEnemyProfileSpells(List<SpellData> destination, EnemyAIProfile profile)
        {
            if (destination == null || profile == null)
            {
                return;
            }

            IReadOnlyList<EnemySpellEntry> entries = profile.Spells;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    SpellData spell = entries[i] != null ? entries[i].spell : null;
                    if (spell != null && !destination.Contains(spell))
                    {
                        destination.Add(spell);
                    }
                }
            }

            if (profile.FallbackSpell != null && !destination.Contains(profile.FallbackSpell))
            {
                destination.Add(profile.FallbackSpell);
            }
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
            Log("Battle log.");
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

        private void NotifyEnemyUnitChanged()
        {
            OnEnemyUnitChanged?.Invoke(enemyUnit);
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

        private void PlayWaveBgm(BattleWaveData waveData)
        {
            AudioClip clip = waveData != null ? waveData.WaveBgmClip : null;
            if (clip == null)
            {
                if (stopBgmWhenWaveHasNoClip && bgmAudioSource != null)
                {
                    bgmAudioSource.Stop();
                    bgmAudioSource.clip = null;
                }

                return;
            }

            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.volume = bgmVolume;
                return;
            }

            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.Play();
        }

        private void SetState(BattleFlowState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);

            if (battleResultNotified)
            {
                return;
            }

            if (state == BattleFlowState.Victory)
            {
                battleResultNotified = true;
                OnBattleWon?.Invoke();
            }
            else if (state == BattleFlowState.Defeat)
            {
                battleResultNotified = true;
                OnBattleLost?.Invoke();
            }
        }

        private void Log(string message)
        {
            OnBattleLog?.Invoke(message);
        }

        private StageRuleContext CreateStageRuleContext(SpellAction action, SpellExecutionReport report)
        {
            return new StageRuleContext(
                this,
                currentStageData,
                currentWaveData,
                currentWaveIndex,
                action,
                report,
                Log);
        }
    }
}
