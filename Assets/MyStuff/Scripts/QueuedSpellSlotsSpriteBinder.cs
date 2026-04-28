using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// 予約した魔法をアクションスロットに反映させるバインダー。
/// </summary>
public class QueuedSpellSlotsSpriteBinder : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private SpriteRenderer slot1Renderer;
    [SerializeField] private SpriteRenderer slot2Renderer;
    [SerializeField] private SpriteRenderer slot3Renderer;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.2f);

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
        Apply(slot1Renderer, battleManager != null ? battleManager.GetQueuedPlayerSpellData(0) : null);
        Apply(slot2Renderer, battleManager != null ? battleManager.GetQueuedPlayerSpellData(1) : null);
        Apply(slot3Renderer, battleManager != null ? battleManager.GetQueuedPlayerSpellData(2) : null);
    }

    private void Apply(SpriteRenderer targetRenderer, SpellData spellData)
    {
        if (targetRenderer == null)
        {
            return;
        }

        bool hasSpell = spellData != null && spellData.IconSprite != null;
        targetRenderer.sprite = hasSpell ? spellData.IconSprite : emptySprite;
        targetRenderer.color = hasSpell ? filledColor : emptyColor;
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
            battleManager.OnPlayerSelectionStarted += Refresh;
        }
        else
        {
            battleManager.OnPlayerQueuedActionsChanged -= Refresh;
            battleManager.OnPlayerSelectionStarted -= Refresh;
        }
    }
}
