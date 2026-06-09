using UnityEngine;

/// <summary>
/// NovelScene開始時にDemoFlowControllerから再生対象Scenarioを受け取り、NovelManagerを起動する。
/// </summary>
public class NovelSceneBootstrap : MonoBehaviour
{
    [SerializeField] private NovelManager novelManager;

    private void OnEnable()
    {
        if (novelManager != null)
        {
            novelManager.OnScenarioFinished += HandleScenarioFinished;
        }
    }

    private void OnDisable()
    {
        if (novelManager != null)
        {
            novelManager.OnScenarioFinished -= HandleScenarioFinished;
        }
    }

    private void Start()
    {
        DemoFlowController flow = DemoFlowController.Instance;
        if (flow == null)
        {
            Debug.LogWarning("NovelSceneBootstrap: DemoFlowController.Instance is missing.", this);
            return;
        }

        if (novelManager == null)
        {
            Debug.LogWarning("NovelSceneBootstrap: novelManager is not assigned.", this);
            return;
        }

        NovelScenarioData scenario = flow.CurrentScenario;
        if (scenario == null)
        {
            Debug.LogWarning("NovelSceneBootstrap: current scenario is null.", this);
            flow.NotifyNovelFinished();
            return;
        }

        novelManager.PlayScenario(scenario);
    }

    private void HandleScenarioFinished()
    {
        DemoFlowController flow = DemoFlowController.Instance;
        if (flow != null)
        {
            flow.NotifyNovelFinished();
        }
    }
}
