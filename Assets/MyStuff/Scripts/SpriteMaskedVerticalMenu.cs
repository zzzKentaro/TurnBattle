using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[Serializable]
public class SpriteMenuIndexEvent : UnityEvent<int> { }

/// <summary>
/// スプライトマスクを活用した、スクロール可能なリッチな縦型UIメニューコンポーネント。
/// </summary>
public class SpriteMaskedVerticalMenu : MonoBehaviour
{
    [Header("Menu Items")]
    [SerializeField] private List<SpriteMenuItem> items = new();
    [SerializeField] private int startIndex = 0;
    [SerializeField] private int visibleCount = 3;
    [SerializeField] private bool wrapAround = false;

    [Header("Auto Collect")]
    [SerializeField] private bool autoCollectItemsFromChildren = false;
    [SerializeField] private Transform itemsRoot;

    [Header("Content Scroll")]
    [SerializeField] private bool enableScroll = true;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private float itemStep = 1.2f;
    [SerializeField] private float contentLerpSpeed = 16f;

    [Header("Scroll Bar")]
    [SerializeField] private bool showScrollBarOnlyWhenScrollEnabled = true;
    [SerializeField] private GameObject scrollBarRoot;
    [SerializeField] private Transform thumb;
    [SerializeField] private Transform thumbTopAnchor;
    [SerializeField] private Transform thumbBottomAnchor;
    [SerializeField] private float thumbLerpSpeed = 16f;
    [SerializeField] private float minThumbScaleRatio = 0.2f;

    [Header("Input")]
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Repeat")]
    [SerializeField] private float deadZone = 0.5f;
    [SerializeField] private float firstRepeatDelay = 0.25f;
    [SerializeField] private float repeatInterval = 0.10f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCancel;
    [SerializeField] private SpriteMenuIndexEvent onSelectionChanged;
    [SerializeField] private SpriteMenuIndexEvent onSubmitIndex;

    public event Action<int, SpriteMenuItem> SelectionChanged;
    public event Action<int, SpriteMenuItem> Submitted;

    private int currentIndex;
    private int topVisibleIndex;

    private bool isHolding;
    private int holdDirection;
    private float nextRepeatTime;
    private bool inputEnabled = true;

    private Vector3 contentBaseLocalPos;
    private Vector3 thumbBaseLocalScale;

    public IReadOnlyList<SpriteMenuItem> Items => items;
    public int ItemCount => items.Count;
    public int CurrentIndex => currentIndex;

    /// <summary>
    /// コンポーネント追加時やReset時に呼ばれ、itemsRootの初期設定を行う。
    /// </summary>
    private void Reset()
    {
        if (itemsRoot == null)
            itemsRoot = contentRoot != null ? contentRoot : transform;
    }

    /// <summary>
    /// インスペクターでの値変更時に呼ばれ、設定値の制限やスクロール状態の更新を行う。
    /// </summary>
    private void OnValidate()
    {
        visibleCount = Mathf.Max(1, visibleCount);

        if (itemsRoot == null)
            itemsRoot = contentRoot != null ? contentRoot : transform;

        if (autoCollectItemsFromChildren)
            RefreshItemListFromChildren();

        RefreshScrollVisualState();
    }

    /// <summary>
    /// 初期化処理。コンテンツルートやスクロールバーのベース位置・スケールを記録する。
    /// </summary>
    private void Awake()
    {
        if (contentRoot != null)
            contentBaseLocalPos = contentRoot.localPosition;

        if (thumb != null)
            thumbBaseLocalScale = thumb.localScale;
    }

    /// <summary>
    /// コンポーネント有効時に呼ばれ、入力の有効化、項目の自動収集、スクロールや選択状態の初期化を行う。
    /// </summary>
    private void OnEnable()
    {
        if (autoCollectItemsFromChildren)
            RefreshItemListFromChildren();

        ApplyInputActionState();

        if (items.Count == 0)
            return;

        currentIndex = Mathf.Clamp(startIndex, 0, items.Count - 1);
        topVisibleIndex = enableScroll ? CalculateTopVisibleIndex(currentIndex, 0) : 0;

        RefreshScrollVisualState();
        RefreshSelection(true);
        NotifySelectionChanged();
        ApplyThumbScaleImmediate();
        ApplyPositionsImmediate();
        ResetHold();
    }

    /// <summary>
    /// コンポーネント無効時に呼ばれ、入力のホールド状態をリセットする。
    /// </summary>
    private void OnDisable()
    {
        ResetHold();
    }

    /// <summary>
    /// 毎フレーム呼ばれ、入力処理とアニメーション（スクロール等）の更新を行う。
    /// </summary>
    private void Update()
    {
        if (items.Count == 0)
            return;

        if (inputEnabled)
        {
            HandleNavigate();
            HandleSubmit();
            HandleCancel();
        }

        UpdateAnimatedPositions();
    }

    /// <summary>
    /// ナビゲーション（上下移動）の入力処理を行う。長押しによる連続移動もサポートする。
    /// </summary>
    private void HandleNavigate()
    {
        if (navigateAction == null)
            return;

        Vector2 input = navigateAction.action.ReadValue<Vector2>();

        int direction = 0;

        if (input.y >= deadZone)
            direction = -1;
        else if (input.y <= -deadZone)
            direction = 1;

        if (direction == 0)
        {
            ResetHold();
            return;
        }

        if (!isHolding || direction != holdDirection)
        {
            MoveSelection(direction);
            isHolding = true;
            holdDirection = direction;
            nextRepeatTime = Time.unscaledTime + firstRepeatDelay;
            return;
        }

        if (Time.unscaledTime >= nextRepeatTime)
        {
            MoveSelection(direction);
            nextRepeatTime = Time.unscaledTime + repeatInterval;
        }
    }

    /// <summary>
    /// 決定ボタンの入力処理を行う。
    /// </summary>
    private void HandleSubmit()
    {
        if (submitAction == null)
            return;

        if (submitAction.action.WasPressedThisFrame())
            SubmitCurrentItem();
    }

    /// <summary>
    /// キャンセルボタンの入力処理を行う。
    /// </summary>
    private void HandleCancel()
    {
        if (cancelAction == null)
            return;

        if (cancelAction.action.WasPressedThisFrame())
            onCancel?.Invoke();
    }

    /// <summary>
    /// 現在選択されているアイテムの決定処理を実行する。
    /// </summary>
    public void SubmitCurrentItem()
    {
        SpriteMenuItem item = GetItem(currentIndex);
        if (item == null || !item.IsInteractable)
            return;

        item.Submit();
        onSubmitIndex?.Invoke(currentIndex);
        Submitted?.Invoke(currentIndex, item);
    }

    /// <summary>
    /// 選択位置を指定された方向（-1 または 1）へ移動させる。
    /// </summary>
    /// <param name="direction">移動方向</param>
    private void MoveSelection(int direction)
    {
        int next = currentIndex + direction;

        if (wrapAround)
        {
            if (next < 0) next = items.Count - 1;
            if (next >= items.Count) next = 0;
        }
        else
        {
            next = Mathf.Clamp(next, 0, items.Count - 1);
        }

        if (next == currentIndex)
            return;

        currentIndex = next;
        topVisibleIndex = enableScroll ? CalculateTopVisibleIndex(currentIndex, topVisibleIndex) : 0;

        RefreshSelection();
        NotifySelectionChanged();
    }

    /// <summary>
    /// 選択インデックスが画面内に収まるように、一番上に表示されるアイテムのインデックスを計算する。
    /// </summary>
    /// <param name="selectedIndex">現在の選択インデックス</param>
    /// <param name="currentTop">現在の最上段インデックス</param>
    /// <returns>新しい最上段のインデックス</returns>
    private int CalculateTopVisibleIndex(int selectedIndex, int currentTop)
    {
        int newTop = currentTop;

        if (selectedIndex < newTop)
        {
            newTop = selectedIndex;
        }
        else if (selectedIndex > newTop + visibleCount - 1)
        {
            newTop = selectedIndex - visibleCount + 1;
        }

        int maxTop = Mathf.Max(0, items.Count - visibleCount);
        newTop = Mathf.Clamp(newTop, 0, maxTop);

        return newTop;
    }

    /// <summary>
    /// すべてのメニューアイテムに対して、現在の選択状態を反映させる。
    /// </summary>
    /// <param name="immediate">即座に反映するかどうか</param>
    private void RefreshSelection(bool immediate = false)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                items[i].SetSelected(i == currentIndex, immediate);
        }
    }

    /// <summary>
    /// 選択項目の変更をイベントとして通知する。
    /// </summary>
    private void NotifySelectionChanged()
    {
        SpriteMenuItem item = GetItem(currentIndex);
        onSelectionChanged?.Invoke(currentIndex);
        SelectionChanged?.Invoke(currentIndex, item);
    }

    /// <summary>
    /// 現在のスクロール状態に基づき、コンテンツの目標となるローカル座標を計算する。
    /// </summary>
    /// <returns>目標のローカル座標</returns>
    private Vector3 GetTargetContentLocalPos()
    {
        if (!enableScroll)
            return contentBaseLocalPos;

        return contentBaseLocalPos + Vector3.up * (topVisibleIndex * itemStep);
    }

    /// <summary>
    /// 現在のスクロール状態に基づき、スクロールバーのつまみ（Thumb）の目標ローカル座標を計算する。
    /// </summary>
    /// <returns>目標のローカル座標</returns>
    private Vector3 GetTargetThumbLocalPos()
    {
        if (thumb == null || thumbTopAnchor == null || thumbBottomAnchor == null)
            return Vector3.zero;

        if (!enableScroll)
            return thumbTopAnchor.localPosition;

        int maxTop = Mathf.Max(0, items.Count - visibleCount);
        float t = maxTop == 0 ? 0f : (float)topVisibleIndex / maxTop;

        return Vector3.Lerp(
            thumbTopAnchor.localPosition,
            thumbBottomAnchor.localPosition,
            t
        );
    }

    /// <summary>
    /// アイテム総数と表示可能数から、スクロールバーのつまみのスケールを計算し、即座に適用する。
    /// </summary>
    private void ApplyThumbScaleImmediate()
    {
        if (thumb == null)
            return;

        Vector3 scale = thumbBaseLocalScale;

        if (enableScroll)
        {
            float ratio = (float)visibleCount / Mathf.Max(1, items.Count);
            ratio = Mathf.Clamp(ratio, minThumbScaleRatio, 1f);
            scale.y = thumbBaseLocalScale.y * ratio;
        }

        thumb.localScale = scale;
    }

    /// <summary>
    /// コンテンツおよびスクロールバーの目標位置を計算し、アニメーションなしで即座に適用する。
    /// </summary>
    private void ApplyPositionsImmediate()
    {
        if (contentRoot != null)
            contentRoot.localPosition = GetTargetContentLocalPos();

        if (thumb != null && thumbTopAnchor != null && thumbBottomAnchor != null)
            thumb.localPosition = GetTargetThumbLocalPos();
    }

    /// <summary>
    /// コンテンツとスクロールバーの位置を、目標位置に向けて滑らかに補間移動（アニメーション）させる。
    /// </summary>
    private void UpdateAnimatedPositions()
    {
        if (contentRoot != null)
        {
            Vector3 target = GetTargetContentLocalPos();
            contentRoot.localPosition = Vector3.Lerp(
                contentRoot.localPosition,
                target,
                1f - Mathf.Exp(-contentLerpSpeed * Time.unscaledDeltaTime)
            );
        }

        if (thumb != null && thumbTopAnchor != null && thumbBottomAnchor != null)
        {
            Vector3 target = GetTargetThumbLocalPos();
            thumb.localPosition = Vector3.Lerp(
                thumb.localPosition,
                target,
                1f - Mathf.Exp(-thumbLerpSpeed * Time.unscaledDeltaTime)
            );
        }
    }

    /// <summary>
    /// スクロール機能の有効/無効状態に応じて、スクロールバーの表示状態を更新する。
    /// </summary>
    private void RefreshScrollVisualState()
    {
        if (scrollBarRoot != null && showScrollBarOnlyWhenScrollEnabled)
            scrollBarRoot.SetActive(enableScroll);
    }

    /// <summary>
    /// 押しっぱなしによる連続移動（ホールド）の状態をリセットする。
    /// </summary>
    private void ResetHold()
    {
        isHolding = false;
        holdDirection = 0;
        nextRepeatTime = 0f;
    }

    /// <summary>
    /// 入力の有効/無効フラグに応じて、InputActionの有効化・無効化を行う。
    /// </summary>
    private void ApplyInputActionState()
    {
        if (inputEnabled)
        {
            navigateAction?.action.Enable();
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
        }
        else
        {
            ResetHold();
        }
    }

    /// <summary>
    /// メニューの入力を有効化、または無効化する。
    /// </summary>
    /// <param name="enabled">有効にするかどうか</param>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        ApplyInputActionState();
    }

    /// <summary>
    /// ルートオブジェクトの子要素からメニューアイテム（SpriteMenuItem）を検索し、リストを更新する。
    /// </summary>
    public void RefreshItemListFromChildren()
    {
        Transform root = itemsRoot != null ? itemsRoot : (contentRoot != null ? contentRoot : transform);
        items.Clear();

        for (int i = 0; i < root.childCount; i++)
        {
            SpriteMenuItem item = root.GetChild(i).GetComponent<SpriteMenuItem>();
            if (item != null)
                items.Add(item);
        }
    }

    /// <summary>
    /// 指定されたインデックスのメニューアイテムを取得する。
    /// </summary>
    /// <param name="index">取得するインデックス</param>
    /// <returns>メニューアイテム（範囲外の場合は null）</returns>
    public SpriteMenuItem GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    /// <summary>
    /// 現在選択されているインデックスを取得する。
    /// </summary>
    /// <returns>現在のインデックス</returns>
    public int GetCurrentIndex() => currentIndex;

    /// <summary>
    /// 選択インデックスを直接指定して設定する。
    /// </summary>
    /// <param name="index">設定するインデックス</param>
    public void SetCurrentIndex(int index)
    {
        if (items.Count == 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);
        topVisibleIndex = enableScroll ? CalculateTopVisibleIndex(currentIndex, topVisibleIndex) : 0;

        RefreshSelection();
        NotifySelectionChanged();
        ResetHold();
    }

    /// <summary>
    /// 選択状態の見た目を強制的に更新し、変更通知を発火する。
    /// </summary>
    public void RefreshSelectionVisuals()
    {
        RefreshSelection(true);
        NotifySelectionChanged();
    }

    /// <summary>
    /// メニューのスクロール機能を有効化、または無効化する。
    /// </summary>
    /// <param name="enabled">スクロールを有効にするかどうか</param>
    /// <param name="snapImmediately">即座に目標位置へスナップさせるかどうか</param>
    public void SetScrollEnabled(bool enabled, bool snapImmediately = true)
    {
        enableScroll = enabled;
        topVisibleIndex = enableScroll ? CalculateTopVisibleIndex(currentIndex, topVisibleIndex) : 0;

        RefreshScrollVisualState();
        ApplyThumbScaleImmediate();

        if (snapImmediately)
            ApplyPositionsImmediate();
    }

    /// <summary>
    /// スクロール機能が有効かどうかを取得する。
    /// </summary>
    /// <returns>有効な場合は true</returns>
    public bool IsScrollEnabled()
    {
        return enableScroll;
    }
}
