using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// 敵が直前に使用した魔法をスロットUIに表示するバインダー。
/// </summary>
public class EnemyUsedSpellSlotsSpriteBinder : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private SpriteRenderer slot1Renderer;
    [SerializeField] private SpriteRenderer slot2Renderer;
    [SerializeField] private SpriteRenderer slot3Renderer;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color copiedColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
    [SerializeField] private Color unavailableColor = new Color(1f, 1f, 1f, 0.35f);

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
        Apply(slot1Renderer, battleManager != null ? battleManager.GetLastEnemyUsedSpellData(0) : null, 0);
        Apply(slot2Renderer, battleManager != null ? battleManager.GetLastEnemyUsedSpellData(1) : null, 1);
        Apply(slot3Renderer, battleManager != null ? battleManager.GetLastEnemyUsedSpellData(2) : null, 2);
    }

    private void Apply(SpriteRenderer targetRenderer, SpellData spellData, int slotIndex)
    {
        if (targetRenderer == null)
        {
            return;
        }

        bool hasSpell = spellData != null && spellData.IconSprite != null;
        targetRenderer.sprite = hasSpell ? spellData.IconSprite : emptySprite;
        targetRenderer.color = ResolveColor(spellData, slotIndex, hasSpell);
    }

    private Color ResolveColor(SpellData spellData, int slotIndex, bool hasSpell)
    {
        if (!hasSpell)
        {
            return emptyColor;
        }

        if (battleManager != null && battleManager.HasPlayerRememberedSpell(spellData))
        {
            return copiedColor;
        }

        if (battleManager != null && !battleManager.CanCopyLastEnemyUsedSpell(slotIndex, out _))
        {
            return unavailableColor;
        }

        return filledColor;
    }

    private void Subscribe(bool subscribe)
    {
        if (battleManager == null)
        {
            return;
        }

        if (subscribe)
        {
            battleManager.OnEnemyLastUsedSpellsChanged += Refresh;
            battleManager.OnPlayerSelectionStarted += Refresh;
            battleManager.OnPlayerMemoryChanged += Refresh;
        }
        else
        {
            battleManager.OnEnemyLastUsedSpellsChanged -= Refresh;
            battleManager.OnPlayerSelectionStarted -= Refresh;
            battleManager.OnPlayerMemoryChanged -= Refresh;
        }
    }
}
