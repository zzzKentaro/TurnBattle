using UnityEngine;

/// <summary>
/// スタート画面の「ゲーム体験」「バトル体験」ボタンからDemoFlowControllerを呼ぶための薄い中継役。
/// </summary>
public class DemoStartMenuActions : MonoBehaviour
{
    [SerializeField] private DemoFlowController demoFlowController;

    public void StartGameExperience()
    {
        DemoFlowController flow = ResolveFlowController();
        if (flow == null)
        {
            Debug.LogWarning("DemoStartMenuActions: demoFlowController is not assigned.", this);
            return;
        }

        flow.StartGameExperience();
    }

    public void StartBattleExperience()
    {
        DemoFlowController flow = ResolveFlowController();
        if (flow == null)
        {
            Debug.LogWarning("DemoStartMenuActions: demoFlowController is not assigned.", this);
            return;
        }

        flow.StartBattleExperience();
    }

    private DemoFlowController ResolveFlowController()
    {
        if (demoFlowController != null)
        {
            return demoFlowController;
        }

        return DemoFlowController.Instance;
    }
}
