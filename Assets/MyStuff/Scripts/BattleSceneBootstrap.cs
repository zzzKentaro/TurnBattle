using TurnBasedBattle;
using UnityEngine;

/// <summary>
/// BattleScene開始時にDemoFlowControllerからStageDataを受け取り、BattleStageControllerを起動する。
/// </summary>
public class BattleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private BattleStageController battleStageController;

    private void OnEnable()
    {
        if (battleStageController != null)
        {
            battleStageController.StageCleared += HandleStageCleared;
            battleStageController.StageFailed += HandleStageFailed;
        }
    }

    private void OnDisable()
    {
        if (battleStageController != null)
        {
            battleStageController.StageCleared -= HandleStageCleared;
            battleStageController.StageFailed -= HandleStageFailed;
        }
    }

    private void Start()
    {
        DemoFlowController flow = DemoFlowController.Instance;
        if (flow == null)
        {
            Debug.LogWarning("BattleSceneBootstrap: DemoFlowController.Instance is missing.", this);
            return;
        }

        if (battleStageController == null)
        {
            Debug.LogWarning("BattleSceneBootstrap: battleStageController is not assigned.", this);
            return;
        }

        BattleStageData stage = flow.CurrentBattleStage;
        if (stage == null)
        {
            Debug.LogWarning("BattleSceneBootstrap: current battle stage is null.", this);
            flow.NotifyBattleFailed();
            return;
        }

        battleStageController.StartStage(stage);
    }

    private void HandleStageCleared(BattleStageData _)
    {
        DemoFlowController flow = DemoFlowController.Instance;
        if (flow != null)
        {
            flow.NotifyBattleCleared();
        }
    }

    private void HandleStageFailed(BattleStageData _)
    {
        DemoFlowController flow = DemoFlowController.Instance;
        if (flow != null)
        {
            flow.NotifyBattleFailed();
        }
    }
}
