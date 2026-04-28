using System.Collections.Generic;
using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// バトルメニューへの入力（操作）の有効・無効を制御する。
/// </summary>
public class BattleMenuInputGate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private SpriteMenuStackController menuStack;
    [SerializeField] private GameObject menuRootObject;

    [Header("Menus")]
    [SerializeField] private List<SpriteMaskedVerticalMenu> menus = new();

    [Header("Behaviour")]
    [SerializeField] private bool showMenuOnlyDuringPlayerSelection = true;
    [SerializeField] private bool returnToRootWhenPlayerSelectionStarts = true;

    private void OnEnable()
    {
        if (battleManager != null)
            battleManager.OnStateChanged += HandleStateChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (battleManager != null)
            battleManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(BattleFlowState state)
    {
        Refresh();

        if (state == BattleFlowState.PlayerSelection && returnToRootWhenPlayerSelectionStarts && menuStack != null)
            menuStack.ReturnToRoot();
    }

    public void Refresh()
    {
        bool canOperate = battleManager != null && battleManager.State == BattleFlowState.PlayerSelection;

        if (menuRootObject != null && showMenuOnlyDuringPlayerSelection)
            menuRootObject.SetActive(canOperate);

        for (int i = 0; i < menus.Count; i++)
        {
            if (menus[i] != null)
                menus[i].SetInputEnabled(canOperate);
        }
    }
}
