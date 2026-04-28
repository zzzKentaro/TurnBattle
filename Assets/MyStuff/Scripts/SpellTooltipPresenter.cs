using TMPro;
using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// スロット上の魔法にフォーカスした際の詳細情報（ツールチップ）を表示するプレゼンター。
/// </summary>
public class SpellTooltipPresenter : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private string emptyDisplayName = "";
    [SerializeField] private string emptyDescription = "";

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.OnPlayerSelectionStarted += Clear;
        }

        Clear();
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnPlayerSelectionStarted -= Clear;
        }
    }

    public void ShowRememberedSpell(RememberedSpell rememberedSpell)
    {
        ShowSpell(rememberedSpell != null ? rememberedSpell.SpellData : null);
    }

    public void ShowSpell(SpellData spellData)
    {
        if (spellData == null)
        {
            Clear();
            return;
        }

        if (displayNameText != null)
        {
            displayNameText.text = spellData.DisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = spellData.Description;
        }
    }

    public void Clear()
    {
        if (displayNameText != null)
        {
            displayNameText.text = emptyDisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = emptyDescription;
        }
    }
}
