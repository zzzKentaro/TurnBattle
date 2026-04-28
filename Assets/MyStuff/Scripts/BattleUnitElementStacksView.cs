using System;
using TMPro;
using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// 現在蓄積されている属性スタック数をアイコン等でUIに表示する。
/// </summary>
public class BattleUnitElementStacksView : MonoBehaviour
{
    [Serializable]
    private class ElementStackSlotView
    {
        public ElementType elementType = ElementType.Fire;
        public GameObject root;
        public SpriteRenderer iconRenderer;
        public Sprite iconSprite;
        public TMP_Text countText;
        public string countFormat = "{0}";
        public bool hideWhenZero = true;
    }

    [SerializeField] private BattleUnit targetUnit;
    [SerializeField] private ElementStackSlotView[] slots = new ElementStackSlotView[5];

    private void Awake()
    {
        ApplyIcons();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            RefreshSlot(slots[i]);
        }
    }

    private void ApplyIcons()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            ElementStackSlotView slot = slots[i];
            if (slot == null || slot.iconRenderer == null || slot.iconSprite == null)
            {
                continue;
            }

            slot.iconRenderer.sprite = slot.iconSprite;
        }
    }

    private void RefreshSlot(ElementStackSlotView slot)
    {
        if (slot == null)
        {
            return;
        }

        int count = targetUnit != null ? targetUnit.GetElementStacks(slot.elementType) : 0;
        bool visible = !slot.hideWhenZero || count > 0;

        if (slot.root != null && slot.root.activeSelf != visible)
        {
            slot.root.SetActive(visible);
        }

        if (slot.iconRenderer != null)
        {
            slot.iconRenderer.enabled = visible;
        }

        if (slot.countText != null)
        {
            slot.countText.enabled = visible;
            slot.countText.text = string.Format(slot.countFormat, count);
        }
    }
}
