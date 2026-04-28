using System.Collections.Generic;
using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// 記憶した魔法一覧メニューのUI構築およびデータのバインドを行う。
/// </summary>
public class RememberedSpellMenuBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private MagicManager magicManager;
    [SerializeField] private SpriteMaskedVerticalMenu menu;

    [Header("Items")]
    [Tooltip("未設定ならmenu.Itemsを使います。")]
    [SerializeField] private List<SpriteMenuItem> items = new();
    [SerializeField] private bool hideEmptyItems = false;

    [Header("Label")]
    [SerializeField] private string emptyLabel = "---";
    [SerializeField] private bool showLevel = true;
    [SerializeField] private bool showMpCost = true;

    [Header("Icon")]
    [SerializeField] private Sprite emptyIcon;

    [Header("Usability")]
    [SerializeField] private bool disableUnusableSpells = true;

    private void Reset()
    {
        menu = GetComponent<SpriteMaskedVerticalMenu>();
    }

    private void Awake()
    {
        if (menu == null)
            menu = GetComponent<SpriteMaskedVerticalMenu>();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
        NotifyCurrentSelectionToTooltip();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (battleManager != null)
        {
            battleManager.OnPlayerMemoryChanged += Refresh;
            battleManager.OnPlayerQueuedActionsChanged += Refresh;
            battleManager.OnStateChanged += HandleBattleStateChanged;
            battleManager.OnPendingEnemySpellCopyChanged += Refresh;
        }

        if (menu != null)
        {
            menu.SelectionChanged += HandleSelectionChanged;
            menu.Submitted += HandleSubmitted;
        }
    }

    private void Unsubscribe()
    {
        if (battleManager != null)
        {
            battleManager.OnPlayerMemoryChanged -= Refresh;
            battleManager.OnPlayerQueuedActionsChanged -= Refresh;
            battleManager.OnStateChanged -= HandleBattleStateChanged;
            battleManager.OnPendingEnemySpellCopyChanged -= Refresh;
        }

        if (menu != null)
        {
            menu.SelectionChanged -= HandleSelectionChanged;
            menu.Submitted -= HandleSubmitted;
        }
    }

    private void HandleBattleStateChanged(BattleFlowState _)
    {
        Refresh();
    }

    private IReadOnlyList<SpriteMenuItem> GetItems()
    {
        if (items != null && items.Count > 0)
            return items;

        return menu != null ? menu.Items : null;
    }

    public void Refresh()
    {
        IReadOnlyList<SpriteMenuItem> targetItems = GetItems();
        if (targetItems == null || battleManager == null)
            return;

        bool replacingMemorySlot = battleManager.HasPendingEnemySpellCopy;
        int nextActionSlot = battleManager.GetNextEmptyPlayerActionSlot();

        for (int i = 0; i < targetItems.Count; i++)
        {
            SpriteMenuItem item = targetItems[i];
            if (item == null)
                continue;

            RememberedSpell remembered = battleManager.GetPlayerRememberedSpellAt(i);
            SpellData spell = remembered != null ? remembered.SpellData : null;
            bool hasSpell = spell != null;

            if (hideEmptyItems)
                item.gameObject.SetActive(hasSpell);
            else
                item.gameObject.SetActive(true);

            if (!hasSpell)
            {
                item.SetLabelText(emptyLabel);
                item.SetIconSprite(emptyIcon);
                item.SetInteractable(false);
                continue;
            }

            item.SetLabelText(BuildLabel(remembered));
            item.SetIconSprite(spell.IconSprite != null ? spell.IconSprite : emptyIcon);

            bool interactable = true;

            if (replacingMemorySlot)
            {
                // 敵魔法コピーの入れ替え先を選ぶときは、既存の記憶スロットを選べればよい。
                interactable = true;
            }
            else if (disableUnusableSpells)
            {
                interactable = battleManager.State == BattleFlowState.PlayerSelection
                    && nextActionSlot >= 0
                    && battleManager.CanQueuePlayerSpell(nextActionSlot, remembered, out _);
            }

            item.SetInteractable(interactable);
        }
    }

    private string BuildLabel(RememberedSpell remembered)
    {
        if (remembered == null || remembered.SpellData == null)
            return emptyLabel;

        SpellData spell = remembered.SpellData;
        string label = spell.DisplayName;

        if (showLevel)
            label += $" Lv.{remembered.Level}";

        if (showMpCost)
            label += $"  MP:{spell.MpCost}";

        return label;
    }

    private void HandleSelectionChanged(int index, SpriteMenuItem item)
    {
        if (magicManager == null || battleManager == null)
            return;

        if (battleManager.GetPlayerRememberedSpellAt(index) == null)
        {
            magicManager.ClearHoveredSpell();
            return;
        }

        magicManager.HoverMagicByMemoryIndex(index);
    }

    private void HandleSubmitted(int index, SpriteMenuItem item)
    {
        if (magicManager == null)
            return;

        magicManager.SelectMagicByMemoryIndex(index);
        Refresh();
    }

    private void NotifyCurrentSelectionToTooltip()
    {
        if (menu == null || magicManager == null || battleManager == null)
            return;

        int index = menu.GetCurrentIndex();
        if (battleManager.GetPlayerRememberedSpellAt(index) != null)
            magicManager.HoverMagicByMemoryIndex(index);
        else
            magicManager.ClearHoveredSpell();
    }
}
