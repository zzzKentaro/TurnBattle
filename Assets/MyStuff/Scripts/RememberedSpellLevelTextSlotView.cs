using TMPro;
using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// 記憶した魔法のレベルテキスト表示を制御するビュースクリプト。
/// </summary>
public class RememberedSpellLevelTextSlotView : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField, Min(0)] private int memoryIndex;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private string levelPrefix = "Lv.";
    [SerializeField] private string emptyText = "";
    [SerializeField] private bool hideWhenEmpty = false;

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
        if (levelText == null)
        {
            return;
        }

        RememberedSpell rememberedSpell = battleManager != null ? battleManager.GetPlayerRememberedSpellAt(memoryIndex) : null;
        bool hasSpell = rememberedSpell != null && rememberedSpell.SpellData != null;

        if (hideWhenEmpty)
        {
            levelText.gameObject.SetActive(hasSpell);
        }

        if (!hasSpell)
        {
            levelText.text = emptyText;
            return;
        }

        levelText.text = $"{levelPrefix}{rememberedSpell.Level}";
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
