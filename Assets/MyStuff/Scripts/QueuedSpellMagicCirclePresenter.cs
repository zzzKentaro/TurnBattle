using UnityEngine;
using TurnBasedBattle;

public class QueuedSpellMagicCirclePresenter : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private AnimatedMagicCircleView slot1Circle;
    [SerializeField] private AnimatedMagicCircleView slot2Circle;
    [SerializeField] private AnimatedMagicCircleView slot3Circle;

    private readonly bool[] lastVisibleStates = new bool[3];

    private void OnEnable()
    {
        Subscribe(true);
        Refresh(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    public void Refresh()
    {
        Refresh(false);
    }

    public void ResetAllImmediately()
    {
        ResetCircle(slot1Circle);
        ResetCircle(slot2Circle);
        ResetCircle(slot3Circle);

        for (int i = 0; i < lastVisibleStates.Length; i++)
        {
            lastVisibleStates[i] = false;
        }
    }

    private void Refresh(bool immediate)
    {
        ApplyCircleState(0, slot1Circle, immediate);
        ApplyCircleState(1, slot2Circle, immediate);
        ApplyCircleState(2, slot3Circle, immediate);
    }

    private void ApplyCircleState(int slotIndex, AnimatedMagicCircleView circle, bool immediate)
    {
        if (circle == null)
        {
            lastVisibleStates[slotIndex] = false;
            return;
        }

        bool shouldShow = battleManager != null && battleManager.GetQueuedPlayerSpellData(slotIndex) != null;
        bool wasVisible = lastVisibleStates[slotIndex];

        if (immediate)
        {
            if (shouldShow)
            {
                circle.ShowImmediate();
            }
            else
            {
                circle.ResetImmediate();
            }
        }
        else if (shouldShow && !wasVisible)
        {
            circle.Show();
        }
        else if (!shouldShow && wasVisible)
        {
            circle.Hide();
        }

        lastVisibleStates[slotIndex] = shouldShow;
    }

    private void ResetCircle(AnimatedMagicCircleView circle)
    {
        if (circle != null)
        {
            circle.ResetImmediate();
        }
    }

    private void Subscribe(bool subscribe)
    {
        if (battleManager == null)
        {
            return;
        }

        if (subscribe)
        {
            battleManager.OnPlayerQueuedActionsChanged += Refresh;
            battleManager.OnPlayerSelectionStarted += ResetAllImmediately;
        }
        else
        {
            battleManager.OnPlayerQueuedActionsChanged -= Refresh;
            battleManager.OnPlayerSelectionStarted -= ResetAllImmediately;
        }
    }
}
