using UnityEngine;
using TurnBasedBattle;

public class RememberedSpellSlotsSpriteBinder : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private SpriteRenderer[] slotRenderers = new SpriteRenderer[10];
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
        for (int i = 0; i < slotRenderers.Length; i++)
        {
            Apply(slotRenderers[i], battleManager != null ? battleManager.GetPlayerRememberedSpellAt(i)?.SpellData : null);
        }
    }

    private void Apply(SpriteRenderer targetRenderer, SpellData spellData)
    {
        if (targetRenderer == null)
        {
            return;
        }

        bool hasIcon = spellData != null && spellData.IconSprite != null;
        targetRenderer.sprite = hasIcon ? spellData.IconSprite : emptySprite;
        targetRenderer.color = hasIcon ? filledColor : emptyColor;
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
