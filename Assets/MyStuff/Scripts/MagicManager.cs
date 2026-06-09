using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// ゲーム内で登場する魔法データ全般を統括する。
/// </summary>
public class MagicManager : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleUnit enemyUnit;
    [SerializeField] private SpellTooltipPresenter tooltipPresenter;

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.OnEnemyUnitChanged += HandleEnemyUnitChanged;
            HandleEnemyUnitChanged(battleManager.CurrentEnemyUnit);
        }
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnEnemyUnitChanged -= HandleEnemyUnitChanged;
        }
    }

    public void SelectMagicByMemoryIndex(int memoryIndex)
    {
        if (battleManager == null)
        {
            Debug.LogWarning("MagicManager: battleManager が未設定です。");
            return;
        }

        // 敵魔法コピーの置き換え待ち中は、クリックした自分スロットを入れ替え先として使う。
        if (battleManager.HasPendingEnemySpellCopy)
        {
            bool replaced = battleManager.TryResolvePendingEnemySpellCopyToMemorySlot(memoryIndex);
            if (!replaced)
            {
                Debug.LogWarning($"MagicManager: 記憶スロット {memoryIndex + 1} への入れ替えに失敗しました。");
            }
            return;
        }

        if (battleManager.State != BattleFlowState.PlayerSelection)
        {
            Debug.Log("MagicManager: 今は魔法を行動枠にセットするタイミングではありません。");
            return;
        }

        var spells = battleManager.GetPlayerRememberedSpells();
        if (spells == null || spells.Count == 0)
        {
            Debug.Log("MagicManager: 記憶している魔法がありません。");
            return;
        }

        if (memoryIndex < 0 || memoryIndex >= spells.Count)
        {
            Debug.LogWarning($"MagicManager: memoryIndex が範囲外です。 memoryIndex={memoryIndex}, spells.Count={spells.Count}");
            return;
        }

        int nextSlotIndex = battleManager.GetNextEmptyPlayerActionSlot();
        if (nextSlotIndex < 0)
        {
            Debug.Log("MagicManager: 行動枠がすでに3つ埋まっています。");
            return;
        }

        RememberedSpell selectedSpell = spells[memoryIndex];
        BattleUnit targetEnemy = battleManager.CurrentEnemyUnit != null ? battleManager.CurrentEnemyUnit : enemyUnit;
        bool success = battleManager.TryQueuePlayerSpell(nextSlotIndex, selectedSpell, targetEnemy);

        if (!success)
        {
            string spellName = selectedSpell != null && selectedSpell.SpellData != null
                ? selectedSpell.SpellData.DisplayName
                : "(null)";
            Debug.LogWarning($"MagicManager: 魔法セット失敗 slot={nextSlotIndex}, spell={spellName}");
        }
    }

    public void HoverMagicByMemoryIndex(int memoryIndex)
    {
        if (tooltipPresenter == null || battleManager == null)
        {
            return;
        }

        RememberedSpell rememberedSpell = battleManager.GetPlayerRememberedSpellAt(memoryIndex);
        tooltipPresenter.ShowRememberedSpell(rememberedSpell);
    }

    public void HoverEnemyUsedSpellBySlotIndex(int slotIndex)
    {
        if (tooltipPresenter == null || battleManager == null)
        {
            return;
        }

        tooltipPresenter.ShowSpell(battleManager.GetLastEnemyUsedSpellData(slotIndex));
    }

    public void RequestEnemySpellCopyBySlotIndex(int slotIndex)
    {
        if (battleManager == null)
        {
            return;
        }

        bool success = battleManager.TryBeginEnemySpellCopy(slotIndex);
        if (!success)
        {
            Debug.LogWarning($"MagicManager: 敵スロット {slotIndex + 1} からのコピー開始に失敗しました。");
        }
    }

    public void CancelPendingEnemySpellCopy()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.CancelPendingEnemySpellCopy();
    }

    public void ClearHoveredSpell()
    {
        if (tooltipPresenter != null)
        {
            tooltipPresenter.Clear();
        }
    }

    private void HandleEnemyUnitChanged(BattleUnit nextEnemyUnit)
    {
        if (nextEnemyUnit != null)
        {
            enemyUnit = nextEnemyUnit;
        }
    }

    public void MagicSlot1() => SelectMagicByMemoryIndex(0);
    public void MagicSlot2() => SelectMagicByMemoryIndex(1);
    public void MagicSlot3() => SelectMagicByMemoryIndex(2);
    public void MagicSlot4() => SelectMagicByMemoryIndex(3);
    public void MagicSlot5() => SelectMagicByMemoryIndex(4);
    public void MagicSlot6() => SelectMagicByMemoryIndex(5);
    public void MagicSlot7() => SelectMagicByMemoryIndex(6);
    public void MagicSlot8() => SelectMagicByMemoryIndex(7);
    public void MagicSlot9() => SelectMagicByMemoryIndex(8);
    public void MagicSlot10() => SelectMagicByMemoryIndex(9);

    public void HoverSlot1() => HoverMagicByMemoryIndex(0);
    public void HoverSlot2() => HoverMagicByMemoryIndex(1);
    public void HoverSlot3() => HoverMagicByMemoryIndex(2);
    public void HoverSlot4() => HoverMagicByMemoryIndex(3);
    public void HoverSlot5() => HoverMagicByMemoryIndex(4);
    public void HoverSlot6() => HoverMagicByMemoryIndex(5);
    public void HoverSlot7() => HoverMagicByMemoryIndex(6);
    public void HoverSlot8() => HoverMagicByMemoryIndex(7);
    public void HoverSlot9() => HoverMagicByMemoryIndex(8);
    public void HoverSlot10() => HoverMagicByMemoryIndex(9);

    public void EnemyUsedSlot1() => RequestEnemySpellCopyBySlotIndex(0);
    public void EnemyUsedSlot2() => RequestEnemySpellCopyBySlotIndex(1);
    public void EnemyUsedSlot3() => RequestEnemySpellCopyBySlotIndex(2);

    public void HoverEnemyUsedSlot1() => HoverEnemyUsedSpellBySlotIndex(0);
    public void HoverEnemyUsedSlot2() => HoverEnemyUsedSpellBySlotIndex(1);
    public void HoverEnemyUsedSlot3() => HoverEnemyUsedSpellBySlotIndex(2);
}
