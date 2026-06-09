using System;
using TurnBasedBattle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum DemoFlowStep
{
    None,
    OpeningNovel,
    Battle,
    EndingNovel,
    Complete,
}

/// <summary>
/// 体験版のScene遷移を管理する常駐オブジェクト。
/// TitleSceneに置き、DontDestroyOnLoadでNovelScene/BattleSceneへ状態を持ち越す。
/// </summary>
public class DemoFlowController : MonoBehaviour
{
    [Serializable]
    private class DemoRoute
    {
        public string routeName;
        public NovelScenarioData openingScenario;
        public BattleStageData battleStage;
        public NovelScenarioData endingScenario;
        public bool playOpeningScenario = true;
        public bool playEndingScenario = true;
    }

    public static DemoFlowController Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "DemoStartScene";
    [SerializeField] private string novelSceneName = "ADVScene1";
    [SerializeField] private string battleSceneName = "GameScene2";

    [Header("Routes")]
    [SerializeField] private DemoRoute gameExperienceRoute = new DemoRoute { routeName = "Game Experience" };
    [SerializeField] private DemoRoute battleExperienceRoute = new DemoRoute { routeName = "Battle Experience", playOpeningScenario = false, playEndingScenario = false };

    [Header("Events")]
    [SerializeField] private UnityEvent onFlowStarted;
    [SerializeField] private UnityEvent onReturnToTitle;

    private DemoRoute activeRoute;
    private DemoFlowStep currentStep = DemoFlowStep.None;

    public DemoFlowStep CurrentStep => currentStep;
    public NovelScenarioData CurrentScenario
    {
        get
        {
            if (activeRoute == null)
            {
                return null;
            }

            return currentStep == DemoFlowStep.OpeningNovel
                ? activeRoute.openingScenario
                : activeRoute.endingScenario;
        }
    }

    public BattleStageData CurrentBattleStage => activeRoute != null ? activeRoute.battleStage : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGameExperience()
    {
        StartRoute(gameExperienceRoute);
    }

    public void StartBattleExperience()
    {
        StartRoute(battleExperienceRoute);
    }

    public void NotifyNovelFinished()
    {
        if (activeRoute == null)
        {
            LoadTitleScene();
            return;
        }

        if (currentStep == DemoFlowStep.OpeningNovel)
        {
            LoadBattleScene();
            return;
        }

        if (currentStep == DemoFlowStep.EndingNovel)
        {
            CompleteFlow();
        }
    }

    public void NotifyBattleCleared()
    {
        if (activeRoute != null &&
            activeRoute.playEndingScenario &&
            activeRoute.endingScenario != null &&
            !string.IsNullOrWhiteSpace(novelSceneName))
        {
            currentStep = DemoFlowStep.EndingNovel;
            SceneManager.LoadScene(novelSceneName);
            return;
        }

        CompleteFlow();
    }

    public void NotifyBattleFailed()
    {
        CompleteFlow();
    }

    public void ReturnToTitle()
    {
        CompleteFlow();
    }

    private void StartRoute(DemoRoute route)
    {
        if (route == null)
        {
            Debug.LogWarning("DemoFlowController: route is null.", this);
            return;
        }

        activeRoute = route;
        onFlowStarted?.Invoke();

        if (route.playOpeningScenario &&
            route.openingScenario != null &&
            !string.IsNullOrWhiteSpace(novelSceneName))
        {
            currentStep = DemoFlowStep.OpeningNovel;
            SceneManager.LoadScene(novelSceneName);
            return;
        }

        LoadBattleScene();
    }

    private void LoadBattleScene()
    {
        if (activeRoute == null || activeRoute.battleStage == null)
        {
            Debug.LogWarning("DemoFlowController: active route has no battle stage.", this);
            CompleteFlow();
            return;
        }

        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogWarning("DemoFlowController: battleSceneName is empty.", this);
            CompleteFlow();
            return;
        }

        currentStep = DemoFlowStep.Battle;
        SceneManager.LoadScene(battleSceneName);
    }

    private void CompleteFlow()
    {
        currentStep = DemoFlowStep.Complete;
        activeRoute = null;
        LoadTitleScene();
    }

    private void LoadTitleScene()
    {
        currentStep = DemoFlowStep.None;
        onReturnToTitle?.Invoke();

        if (!string.IsNullOrWhiteSpace(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }
}
