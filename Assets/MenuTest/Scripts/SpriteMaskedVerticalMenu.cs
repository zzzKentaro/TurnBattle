using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

    private int currentIndex;
    private int topVisibleIndex;

    private bool isHolding;
    private int holdDirection;
    private float nextRepeatTime;
    private bool inputEnabled = true;

    private Vector3 contentBaseLocalPos;
    private Vector3 thumbBaseLocalScale;

    private void Reset()
    {
        if (itemsRoot == null)
            itemsRoot = contentRoot != null ? contentRoot : transform;
    }

    private void OnValidate()
    {
        visibleCount = Mathf.Max(1, visibleCount);

        if (itemsRoot == null)
            itemsRoot = contentRoot != null ? contentRoot : transform;

        if (autoCollectItemsFromChildren)
            RefreshItemListFromChildren();

        RefreshScrollVisualState();
    }

    private void Awake()
    {
        if (contentRoot != null)
            contentBaseLocalPos = contentRoot.localPosition;

        if (thumb != null)
            thumbBaseLocalScale = thumb.localScale;
    }

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
        ApplyThumbScaleImmediate();
        ApplyPositionsImmediate();
        ResetHold();
    }

    private void OnDisable()
    {
        ResetHold();
    }

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

        if (direction == -1)
            Debug.Log("Menu Up");
        else if (direction == 1)
            Debug.Log("Menu Down");

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

    private void HandleSubmit()
    {
        if (submitAction == null)
            return;

        if (submitAction.action.WasPressedThisFrame())
        {
            items[currentIndex].Submit();
        }
    }

    private void HandleCancel()
    {
        if (cancelAction == null)
            return;

        if (cancelAction.action.WasPressedThisFrame())
        {
            onCancel?.Invoke();
        }
    }

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
    }

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

    private void RefreshSelection(bool immediate = false)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                items[i].SetSelected(i == currentIndex, immediate);
        }
    }

    private Vector3 GetTargetContentLocalPos()
    {
        if (!enableScroll)
            return contentBaseLocalPos;

        return contentBaseLocalPos + Vector3.up * (topVisibleIndex * itemStep);
    }

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

    private void ApplyPositionsImmediate()
    {
        if (contentRoot != null)
            contentRoot.localPosition = GetTargetContentLocalPos();

        if (thumb != null && thumbTopAnchor != null && thumbBottomAnchor != null)
            thumb.localPosition = GetTargetThumbLocalPos();
    }

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

    private void RefreshScrollVisualState()
    {
        if (scrollBarRoot != null && showScrollBarOnlyWhenScrollEnabled)
            scrollBarRoot.SetActive(enableScroll);
    }

    private void ResetHold()
    {
        isHolding = false;
        holdDirection = 0;
        nextRepeatTime = 0f;
    }

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

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        ApplyInputActionState();
    }

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

    public int GetCurrentIndex() => currentIndex;

    public void SetCurrentIndex(int index)
    {
        if (items.Count == 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);
        topVisibleIndex = enableScroll ? CalculateTopVisibleIndex(currentIndex, topVisibleIndex) : 0;

        RefreshSelection();
        ResetHold();
    }

    public void SetScrollEnabled(bool enabled, bool snapImmediately = true)
    {
        enableScroll = enabled;
        topVisibleIndex = enableScroll ? CalculateTopVisibleIndex(currentIndex, topVisibleIndex) : 0;

        RefreshScrollVisualState();
        ApplyThumbScaleImmediate();

        if (snapImmediately)
            ApplyPositionsImmediate();
    }

    public bool IsScrollEnabled()
    {
        return enableScroll;
    }
}
