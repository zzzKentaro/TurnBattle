using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TurnBasedBattle
{
    /// <summary>
    /// BattleManagerの外側で、1つのステージ内のWave連戦を進行する。
    /// BattleManagerは1Wave分の戦闘実行に集中させる。
    /// </summary>
    public class BattleStageController : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BattleStageData stageData;
        [SerializeField] private bool startOnStart;

        [Header("Wave Transition")]
        [SerializeField] private FadeController waveTransitionFade;
        [SerializeField, Min(0f)] private float waveTransitionHoldSeconds = 0.2f;

        [Header("Events")]
        [SerializeField] private UnityEvent onStageStarted;
        [SerializeField] private UnityEvent onWaveStarted;
        [SerializeField] private UnityEvent onWaveEnded;
        [SerializeField] private UnityEvent onStageCleared;
        [SerializeField] private UnityEvent onStageFailed;

        private int currentWaveIndex = -1;
        private bool isRunning;
        private BattleManager subscribedBattleManager;

        public BattleStageData CurrentStageData => stageData;
        public int CurrentWaveIndex => currentWaveIndex;
        public bool IsRunning => isRunning;
        public event Action<BattleStageData> StageStarted;
        public event Action<BattleWaveData, int> WaveStarted;
        public event Action<BattleWaveData, int> WaveEnded;
        public event Action<BattleStageData> StageCleared;
        public event Action<BattleStageData> StageFailed;

        private void Start()
        {
            if (startOnStart && stageData != null)
            {
                StartStage(stageData);
            }
        }

        private void OnEnable()
        {
            Subscribe(true);
        }

        private void OnDisable()
        {
            Subscribe(false);
        }

        public void StartConfiguredStage()
        {
            StartStage(stageData);
        }

        public void StartStage(BattleStageData nextStageData)
        {
            if (isRunning)
            {
                Debug.LogWarning("BattleStageController: stage is already running. Ignored duplicate StartStage call.", this);
                return;
            }

            if (battleManager == null)
            {
                Debug.LogWarning("BattleStageController: battleManager is not assigned.", this);
                return;
            }

            if (nextStageData == null || nextStageData.Waves == null || nextStageData.Waves.Count == 0)
            {
                Debug.LogWarning("BattleStageController: stageData has no waves.", this);
                return;
            }

            Subscribe(true);
            stageData = nextStageData;
            isRunning = true;
            currentWaveIndex = -1;
            battleManager.ConfigureStageRules(stageData);

            battleManager.PreparePlayerMemoryForStage(
                stageData.PlayerStartingSpells,
                stageData.ClearPlayerMemoryOnStageStart);

            if (!string.IsNullOrWhiteSpace(stageData.StageStartLogMessage))
            {
                battleManager.WriteBattleLog(stageData.StageStartLogMessage);
            }

            onStageStarted?.Invoke();
            StageStarted?.Invoke(stageData);
            StartNextWave();
        }

        public void StartNextWave()
        {
            if (!isRunning || stageData == null)
            {
                return;
            }

            currentWaveIndex++;
            if (currentWaveIndex >= stageData.Waves.Count)
            {
                CompleteStage();
                return;
            }

            BattleWaveData wave = stageData.Waves[currentWaveIndex];
            if (wave == null)
            {
                Debug.LogWarning($"BattleStageController: Wave {currentWaveIndex + 1} is null. Skipping.", this);
                StartNextWave();
                return;
            }

            battleManager.NotifyWaveRulesStarted(wave, currentWaveIndex);
            battleManager.StartWaveBattle(wave, stageData, currentWaveIndex);
            onWaveStarted?.Invoke();
            WaveStarted?.Invoke(wave, currentWaveIndex);
        }

        private void HandleBattleWon()
        {
            if (!isRunning || stageData == null)
            {
                return;
            }

            BattleWaveData wave = GetCurrentWave();
            if (wave != null && !string.IsNullOrWhiteSpace(wave.WaveVictoryLogMessage))
            {
                battleManager.WriteBattleLog(wave.WaveVictoryLogMessage);
            }

            onWaveEnded?.Invoke();
            WaveEnded?.Invoke(wave, currentWaveIndex);
            Debug.Log($"BattleStageController: Wave {currentWaveIndex + 1} ended.", this);

            StartCoroutine(StartNextWaveAfterFrame());
        }

        private void HandleBattleLost()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            onStageFailed?.Invoke();
            StageFailed?.Invoke(stageData);
        }

        private void CompleteStage()
        {
            isRunning = false;

            if (stageData != null && !string.IsNullOrWhiteSpace(stageData.StageClearLogMessage))
            {
                battleManager.WriteBattleLog(stageData.StageClearLogMessage);
            }

            onStageCleared?.Invoke();
            StageCleared?.Invoke(stageData);
        }

        private IEnumerator StartNextWaveAfterFrame()
        {
            yield return null;

            if (HasNextWave() && waveTransitionFade != null)
            {
                yield return waveTransitionFade.FadeOut();

                if (waveTransitionHoldSeconds > 0f)
                {
                    yield return new WaitForSeconds(waveTransitionHoldSeconds);
                }

                StartNextWave();
                yield return waveTransitionFade.FadeIn();
                yield break;
            }

            StartNextWave();
        }

        private bool HasNextWave()
        {
            return stageData != null &&
                stageData.Waves != null &&
                currentWaveIndex + 1 < stageData.Waves.Count;
        }

        private BattleWaveData GetCurrentWave()
        {
            if (stageData == null || currentWaveIndex < 0 || currentWaveIndex >= stageData.Waves.Count)
            {
                return null;
            }

            return stageData.Waves[currentWaveIndex];
        }

        private void Subscribe(bool subscribe)
        {
            if (subscribe)
            {
                if (battleManager == null || subscribedBattleManager == battleManager)
                {
                    return;
                }

                Subscribe(false);
                battleManager.OnBattleWon += HandleBattleWon;
                battleManager.OnBattleLost += HandleBattleLost;
                subscribedBattleManager = battleManager;
                return;
            }

            if (subscribedBattleManager != null)
            {
                subscribedBattleManager.OnBattleWon -= HandleBattleWon;
                subscribedBattleManager.OnBattleLost -= HandleBattleLost;
                subscribedBattleManager = null;
            }
        }
    }
}
