using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TurnBasedBattle;
using UnityEngine;

public enum BattleActionUiState
{
    PlayerTurnIdle,
    CategoryOpened,
    SelectingSpells,
    AttackPhase,
}

/// <summary>
/// プレイヤーターン中の上位カテゴリ表示、カテゴリ展開、コピー候補表示を制御する。
/// 既存の魔法選択処理は BattleManager / MagicManager / RememberedSpellMenuBinder に任せる。
/// </summary>
public class BattleCategoryActionPanel : MonoBehaviour
{
    private enum OpenCategory
    {
        None,
        Magic,
        Skill,
        Item,
    }

    [Header("References")]
    [SerializeField] private BattleManager battleManager;

    [Header("Category Buttons")]
    [SerializeField] private Transform categoryButtonsParent;
    [SerializeField] private Transform magicCategoryButton;
    [SerializeField] private Transform skillCategoryButton;
    [SerializeField] private Transform itemCategoryButton;

    [Header("Animation Start Points")]
    [SerializeField] private Transform magicAnimationStartPoint;
    [SerializeField] private Transform skillAnimationStartPoint;
    [SerializeField] private Transform itemAnimationStartPoint;
    [SerializeField] private Transform copyableSpellsAnimationStartPoint;

    [Header("Content Parents")]
    [SerializeField] private Transform magicIconsParent;
    [SerializeField] private Transform skillIconsParent;
    [SerializeField] private Transform itemIconsParent;
    [SerializeField] private Transform copyableSpellsParent;

    [Header("Optional Canvas Groups")]
    [SerializeField] private CanvasGroup categoryButtonsCanvasGroup;
    [SerializeField] private CanvasGroup magicIconsCanvasGroup;
    [SerializeField] private CanvasGroup copyableSpellsCanvasGroup;

    [Header("Selection Slots")]
    [SerializeField] private Transform[] selectedSpellSlots = new Transform[3];
    [SerializeField] private float selectedSlotPunchScale = 0.14f;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float categoryFadeDuration = 0.18f;
    [SerializeField, Min(0f)] private float expandDuration = 0.28f;
    [SerializeField, Min(0f)] private float collapseDuration = 0.22f;
    [SerializeField, Min(0f)] private float iconStaggerSeconds = 0.035f;
    [SerializeField] private Vector3 collapsedIconScale = new Vector3(0.25f, 0.25f, 0.25f);
    [SerializeField] private Ease expandEase = Ease.OutBack;
    [SerializeField] private Ease collapseEase = Ease.InBack;
    [SerializeField] private bool ignoreTimeScale = true;

    [Header("Auto Collect")]
    [SerializeField] private bool autoCollectMagicIcons = true;
    [SerializeField] private bool autoCollectCopyableIcons = true;

    public BattleActionUiState UiState { get; private set; } = BattleActionUiState.PlayerTurnIdle;

    private readonly List<Transform> magicIcons = new List<Transform>();
    private readonly List<Transform> copyableSpellIcons = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> expandedLocalPositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> expandedLocalScales = new Dictionary<Transform, Vector3>();
    private readonly List<Tween> activeTweens = new List<Tween>();

    private OpenCategory openCategory = OpenCategory.None;
    private int lastQueuedCount;

    private void Awake()
    {
        CacheInitialTransforms();
        SetContentActive(magicIconsParent, false);
        SetContentActive(skillIconsParent, false);
        SetContentActive(itemIconsParent, false);
        SetContentActive(copyableSpellsParent, false);
    }

    private void OnEnable()
    {
        Subscribe(true);
        ResetToPlayerTurnIdle(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
        KillTweens();
    }

    public void ToggleMagicCategory()
    {
        ToggleCategory(OpenCategory.Magic);
    }

    public void ToggleSkillCategory()
    {
        ToggleCategory(OpenCategory.Skill);
    }

    public void ToggleItemCategory()
    {
        ToggleCategory(OpenCategory.Item);
    }

    public void CloseCurrentCategory()
    {
        if (openCategory == OpenCategory.None || UiState == BattleActionUiState.AttackPhase)
        {
            return;
        }

        CollapseOpenCategory();
    }

    private void ToggleCategory(OpenCategory category)
    {
        if (UiState == BattleActionUiState.AttackPhase)
        {
            return;
        }

        if (openCategory == category)
        {
            CollapseOpenCategory();
            return;
        }

        if (openCategory != OpenCategory.None)
        {
            return;
        }

        Open(category);
    }

    private void Open(OpenCategory category)
    {
        openCategory = category;
        UiState = GetQueuedCount() > 0 && category == OpenCategory.Magic
            ? BattleActionUiState.SelectingSpells
            : BattleActionUiState.CategoryOpened;

        Transform source = GetAnimationStartPoint(category);
        Transform content = GetContentParent(category);

        FadeOtherCategoryButtons(category, false);
        SetContentActive(content, true);

        if (category == OpenCategory.Magic)
        {
            RefreshIconLists();
            AnimateItemsFromSource(magicIcons, source, expandDuration, expandEase, false);
            ShowCopyableSpells(source);
        }
        else
        {
            AnimateItemsFromSource(CollectChildren(content), source, expandDuration, expandEase, false);
        }
    }

    private void CollapseOpenCategory()
    {
        OpenCategory closingCategory = openCategory;
        Transform source = GetAnimationStartPoint(closingCategory);
        Transform content = GetContentParent(closingCategory);
        List<Transform> contentItems = closingCategory == OpenCategory.Magic
            ? new List<Transform>(magicIcons)
            : CollectChildren(content);

        if (closingCategory == OpenCategory.Magic)
        {
            contentItems.AddRange(copyableSpellIcons);
        }

        openCategory = OpenCategory.None;
        UiState = BattleActionUiState.PlayerTurnIdle;

        AnimateItemsToSource(contentItems, source, collapseDuration, collapseEase, () =>
        {
            SetContentActive(content, false);
            if (closingCategory == OpenCategory.Magic)
            {
                SetContentActive(copyableSpellsParent, false);
            }

            FadeOtherCategoryButtons(OpenCategory.None, true);
        });
    }

    private void ResetToPlayerTurnIdle(bool immediate)
    {
        KillTweens();
        openCategory = OpenCategory.None;
        UiState = BattleActionUiState.PlayerTurnIdle;
        lastQueuedCount = 0;

        SetContentActive(magicIconsParent, false);
        SetContentActive(skillIconsParent, false);
        SetContentActive(itemIconsParent, false);
        SetContentActive(copyableSpellsParent, false);
        SetCategoryButtonsVisible(true, immediate);
    }

    private void HandlePlayerSelectionStarted()
    {
        ResetToPlayerTurnIdle(false);
    }

    private void HandleBattleStateChanged(BattleFlowState state)
    {
        if (state == BattleFlowState.PlayerExecution ||
            state == BattleFlowState.EnemyExecution ||
            state == BattleFlowState.Victory ||
            state == BattleFlowState.Defeat)
        {
            UiState = BattleActionUiState.AttackPhase;
            CollapseAllImmediate();
        }
    }

    private void HandleQueuedActionsChanged()
    {
        int queuedCount = GetQueuedCount();

        if (queuedCount > lastQueuedCount && queuedCount > 0 && queuedCount <= selectedSpellSlots.Length)
        {
            AnimateSelectedSlot(selectedSpellSlots[queuedCount - 1]);
        }
        else if (queuedCount < lastQueuedCount && lastQueuedCount > 0 && lastQueuedCount <= selectedSpellSlots.Length)
        {
            AnimateSelectedSlot(selectedSpellSlots[lastQueuedCount - 1]);
        }

        lastQueuedCount = queuedCount;

        if (UiState == BattleActionUiState.AttackPhase)
        {
            return;
        }

        if (queuedCount >= 3)
        {
            UiState = BattleActionUiState.AttackPhase;
        }
        else if (openCategory == OpenCategory.Magic && queuedCount > 0)
        {
            UiState = BattleActionUiState.SelectingSpells;
        }
        else if (openCategory != OpenCategory.None)
        {
            UiState = BattleActionUiState.CategoryOpened;
        }
        else
        {
            UiState = BattleActionUiState.PlayerTurnIdle;
        }
    }

    private void RefreshCopyableSpellVisuals()
    {
        if (openCategory == OpenCategory.Magic && copyableSpellsParent != null)
        {
            RefreshIconLists();
            ShowCopyableSpells(GetCopyableSpellsAnimationStartPoint());
        }
    }

    private void ShowCopyableSpells(Transform source)
    {
        if (copyableSpellsParent == null)
        {
            return;
        }

        SetContentActive(copyableSpellsParent, true);
        RefreshIconLists();
        AnimateItemsFromSource(copyableSpellIcons, source, expandDuration, Ease.OutQuad, true);
    }

    private void FadeOtherCategoryButtons(OpenCategory activeCategory, bool visible)
    {
        FadeCategoryButton(magicCategoryButton, visible || activeCategory == OpenCategory.Magic);
        FadeCategoryButton(skillCategoryButton, visible || activeCategory == OpenCategory.Skill);
        FadeCategoryButton(itemCategoryButton, visible || activeCategory == OpenCategory.Item);
    }

    private void SetCategoryButtonsVisible(bool visible, bool immediate)
    {
        SetVisible(categoryButtonsParent, visible);

        if (categoryButtonsCanvasGroup != null)
        {
            categoryButtonsCanvasGroup.alpha = visible ? 1f : 0f;
            categoryButtonsCanvasGroup.interactable = visible;
            categoryButtonsCanvasGroup.blocksRaycasts = visible;
        }

        if (immediate)
        {
            SetAlpha(magicCategoryButton, visible ? 1f : 0f);
            SetAlpha(skillCategoryButton, visible ? 1f : 0f);
            SetAlpha(itemCategoryButton, visible ? 1f : 0f);
            SetVisible(magicCategoryButton, visible);
            SetVisible(skillCategoryButton, visible);
            SetVisible(itemCategoryButton, visible);
            return;
        }

        FadeCategoryButton(magicCategoryButton, visible);
        FadeCategoryButton(skillCategoryButton, visible);
        FadeCategoryButton(itemCategoryButton, visible);
    }

    private void FadeCategoryButton(Transform button, bool visible)
    {
        if (button == null)
        {
            return;
        }

        SetVisible(button, true);
        Tween tween = FadeTransform(button, visible ? 1f : 0f, categoryFadeDuration)
            .OnComplete(() => SetVisible(button, visible));
        Track(tween);
    }

    private void AnimateItemsFromSource(List<Transform> items, Transform source, float duration, Ease ease, bool slideOnly)
    {
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Transform item = items[i];
            if (item == null)
            {
                continue;
            }

            EnsureCached(item);
            SetVisible(item, true);
            SetAlpha(item, 0f);

            if (source != null)
            {
                item.position = source.position;
            }

            if (!slideOnly)
            {
                item.localScale = collapsedIconScale;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale).SetLink(item.gameObject);
            sequence.AppendInterval(i * iconStaggerSeconds);
            sequence.Append(item.DOLocalMove(expandedLocalPositions[item], duration).SetEase(ease));
            if (!slideOnly)
            {
                sequence.Join(item.DOScale(expandedLocalScales[item], duration).SetEase(ease));
            }
            sequence.Join(FadeTransform(item, 1f, duration));
            Track(sequence);
        }
    }

    private void AnimateItemsToSource(List<Transform> items, Transform source, float duration, Ease ease, Action onComplete)
    {
        int activeCount = 0;

        if (items == null || items.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Transform item = items[i];
            if (item == null || !item.gameObject.activeSelf)
            {
                continue;
            }

            activeCount++;
            Vector3 targetLocalPosition = item.localPosition;
            if (source != null && item.parent != null)
            {
                targetLocalPosition = item.parent.InverseTransformPoint(source.position);
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale).SetLink(item.gameObject);
            sequence.AppendInterval(i * iconStaggerSeconds);
            sequence.Append(item.DOLocalMove(targetLocalPosition, duration).SetEase(ease));
            sequence.Join(item.DOScale(collapsedIconScale, duration).SetEase(ease));
            sequence.Join(FadeTransform(item, 0f, duration));
            sequence.OnComplete(() => SetVisible(item, false));
            Track(sequence);
        }

        Tween callbackTween = DOVirtual.DelayedCall(duration + (activeCount * iconStaggerSeconds), () => onComplete?.Invoke(), ignoreTimeScale);
        Track(callbackTween);
    }

    private void AnimateSelectedSlot(Transform slot)
    {
        if (slot == null)
        {
            return;
        }

        Tween tween = slot.DOPunchScale(Vector3.one * selectedSlotPunchScale, 0.18f, 8, 0.8f)
            .SetUpdate(ignoreTimeScale)
            .SetLink(slot.gameObject);
        Track(tween);
    }

    private void CollapseAllImmediate()
    {
        KillTweens();
        openCategory = OpenCategory.None;
        SetContentActive(magicIconsParent, false);
        SetContentActive(skillIconsParent, false);
        SetContentActive(itemIconsParent, false);
        SetContentActive(copyableSpellsParent, false);
        SetCategoryButtonsVisible(false, true);
    }

    private void CacheInitialTransforms()
    {
        RefreshIconLists();

        CacheList(magicIcons);
        CacheList(copyableSpellIcons);
        CacheList(CollectChildren(skillIconsParent));
        CacheList(CollectChildren(itemIconsParent));
    }

    private void RefreshIconLists()
    {
        if (autoCollectMagicIcons)
        {
            magicIcons.Clear();
            magicIcons.AddRange(CollectChildren(magicIconsParent));
        }

        if (autoCollectCopyableIcons)
        {
            copyableSpellIcons.Clear();
            copyableSpellIcons.AddRange(CollectChildren(copyableSpellsParent));
        }
    }

    private void CacheList(List<Transform> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            EnsureCached(items[i]);
        }
    }

    private void EnsureCached(Transform item)
    {
        if (item == null)
        {
            return;
        }

        if (!expandedLocalPositions.ContainsKey(item))
        {
            expandedLocalPositions[item] = item.localPosition;
        }

        if (!expandedLocalScales.ContainsKey(item))
        {
            expandedLocalScales[item] = item.localScale;
        }
    }

    private Transform GetCategoryButton(OpenCategory category)
    {
        switch (category)
        {
            case OpenCategory.Magic:
                return magicCategoryButton;
            case OpenCategory.Skill:
                return skillCategoryButton;
            case OpenCategory.Item:
                return itemCategoryButton;
            default:
                return null;
        }
    }

    private Transform GetAnimationStartPoint(OpenCategory category)
    {
        switch (category)
        {
            case OpenCategory.Magic:
                return magicAnimationStartPoint != null ? magicAnimationStartPoint : magicCategoryButton;
            case OpenCategory.Skill:
                return skillAnimationStartPoint != null ? skillAnimationStartPoint : skillCategoryButton;
            case OpenCategory.Item:
                return itemAnimationStartPoint != null ? itemAnimationStartPoint : itemCategoryButton;
            default:
                return null;
        }
    }

    private Transform GetCopyableSpellsAnimationStartPoint()
    {
        if (copyableSpellsAnimationStartPoint != null)
        {
            return copyableSpellsAnimationStartPoint;
        }

        return GetAnimationStartPoint(OpenCategory.Magic);
    }

    private Transform GetContentParent(OpenCategory category)
    {
        switch (category)
        {
            case OpenCategory.Magic:
                return magicIconsParent;
            case OpenCategory.Skill:
                return skillIconsParent;
            case OpenCategory.Item:
                return itemIconsParent;
            default:
                return null;
        }
    }

    private int GetQueuedCount()
    {
        if (battleManager == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < selectedSpellSlots.Length; i++)
        {
            if (battleManager.GetQueuedPlayerSpellData(i) != null)
            {
                count++;
            }
        }

        return count;
    }

    private void Subscribe(bool subscribe)
    {
        if (battleManager == null)
        {
            Debug.LogWarning("BattleCategoryActionPanel: battleManager が未設定です。", this);
            return;
        }

        if (subscribe)
        {
            battleManager.OnPlayerSelectionStarted += HandlePlayerSelectionStarted;
            battleManager.OnPlayerQueuedActionsChanged += HandleQueuedActionsChanged;
            battleManager.OnPlayerMemoryChanged += RefreshCopyableSpellVisuals;
            battleManager.OnStateChanged += HandleBattleStateChanged;
        }
        else
        {
            battleManager.OnPlayerSelectionStarted -= HandlePlayerSelectionStarted;
            battleManager.OnPlayerQueuedActionsChanged -= HandleQueuedActionsChanged;
            battleManager.OnPlayerMemoryChanged -= RefreshCopyableSpellVisuals;
            battleManager.OnStateChanged -= HandleBattleStateChanged;
        }
    }

    private List<Transform> CollectChildren(Transform parent)
    {
        List<Transform> results = new List<Transform>();
        if (parent == null)
        {
            return results;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            results.Add(parent.GetChild(i));
        }

        return results;
    }

    private void SetContentActive(Transform content, bool active)
    {
        SetVisible(content, active);
        SetCanvasGroupState(GetCanvasGroupFor(content), active, active ? 1f : 0f);
    }

    private CanvasGroup GetCanvasGroupFor(Transform content)
    {
        if (content == magicIconsParent)
        {
            return magicIconsCanvasGroup;
        }

        if (content == copyableSpellsParent)
        {
            return copyableSpellsCanvasGroup;
        }

        return content != null ? content.GetComponent<CanvasGroup>() : null;
    }

    private void SetCanvasGroupState(CanvasGroup group, bool active, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = active;
        group.blocksRaycasts = active;
    }

    private Tween FadeTransform(Transform target, float alpha, float duration)
    {
        if (target == null)
        {
            return DOVirtual.DelayedCall(0f, () => { }, ignoreTimeScale);
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale).SetLink(target.gameObject);

        CanvasGroup[] canvasGroups = target.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            sequence.Join(canvasGroups[i].DOFade(alpha, duration));
        }

        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            sequence.Join(spriteRenderers[i].DOFade(alpha, duration));
        }

        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            sequence.Join(texts[i].DOFade(alpha, duration));
        }

        return sequence;
    }

    private void SetAlpha(Transform target, float alpha)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup[] canvasGroups = target.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            canvasGroups[i].alpha = alpha;
        }

        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color color = spriteRenderers[i].color;
            color.a = alpha;
            spriteRenderers[i].color = color;
        }

        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Color color = texts[i].color;
            color.a = alpha;
            texts[i].color = color;
        }
    }

    private void SetVisible(Transform target, bool visible)
    {
        if (target != null)
        {
            target.gameObject.SetActive(visible);
        }
    }

    private void KillTweens()
    {
        for (int i = 0; i < activeTweens.Count; i++)
        {
            Tween tween = activeTweens[i];
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }

        activeTweens.Clear();
    }

    private void Track(Tween tween)
    {
        if (tween != null)
        {
            activeTweens.Add(tween);
        }
    }
}
