using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// 記憶した魔法のアイコン表示を制御するビュースクリプト。
/// </summary>
public class RememberedSpellIconSlotView : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField, Min(0)] private int memoryIndex;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    private void OnEnable()
    {
        Subscribe(true);
        Refresh();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    public void Refresh()
    {
        if (iconRenderer == null)
        {
            return;
        }

        RememberedSpell rememberedSpell = battleManager != null ? battleManager.GetPlayerRememberedSpellAt(memoryIndex) : null;
        SpellData spellData = rememberedSpell != null ? rememberedSpell.SpellData : null;
        bool hasSpell = spellData != null && spellData.IconSprite != null;

        iconRenderer.sprite = hasSpell ? spellData.IconSprite : emptySprite;
        iconRenderer.color = hasSpell ? filledColor : emptyColor;
    }

    private void Subscribe(bool subscribe)
    {
        if (battleManager == null)
        {
            return;
        }

        if (subscribe)
        {
            battleManager.OnPlayerMemoryChanged += Refresh;
            battleManager.OnPlayerSelectionStarted += Refresh;
        }
        else
        {
            battleManager.OnPlayerMemoryChanged -= Refresh;
            battleManager.OnPlayerSelectionStarted -= Refresh;
        }
    }
}
