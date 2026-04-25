using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SpriteVerticalMenu : MonoBehaviour
{
    [Header("Menu Items")]
    [SerializeField] private List<SpriteMenuItem> items = new List<SpriteMenuItem>();
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool wrapAround = true;

    [Header("Auto Collect")]
    [SerializeField] private bool autoCollectItemsFromChildren = false;
    [SerializeField] private Transform itemsRoot;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Navigation")]
    [SerializeField] private float deadZone = 0.5f;
    [SerializeField] private float firstRepeatDelay = 0.25f;
    [SerializeField] private float repeatInterval = 0.10f;

    [Header("Optional")]
    [SerializeField] private UnityEvent onCancel;

    private int currentIndex;
    private bool isHolding;
    private int holdDirection;
    private float nextRepeatTime;
    private bool inputEnabled = true;

    private void Reset()
    {
        if (itemsRoot == null)
            itemsRoot = transform;
    }

    private void OnValidate()
    {
        if (itemsRoot == null)
            itemsRoot = transform;

        if (autoCollectItemsFromChildren)
            RefreshItemListFromChildren();
    }

    private void OnEnable()
    {
        if (autoCollectItemsFromChildren)
            RefreshItemListFromChildren();

        ApplyInputActionState();

        if (items.Count == 0)
            return;

        currentIndex = Mathf.Clamp(startIndex, 0, items.Count - 1);
        RefreshSelection(true);
        ResetHold();
    }

    private void OnDisable()
    {
        navigateAction?.action.Disable();
        submitAction?.action.Disable();
        cancelAction?.action.Disable();
    }

    private void Update()
    {
        if (!inputEnabled || items.Count == 0)
            return;

        HandleNavigate();
        HandleSubmit();
        HandleCancel();
    }

    private void HandleNavigate()
    {
        if (navigateAction == null)
            return;

        Vector2 input = navigateAction.action.ReadValue<Vector2>();

        int direction = 0;

        if (input.y >= deadZone)
        {
            direction = -1;
        }
        else if (input.y <= -deadZone)
        {
            direction = 1;
        }

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
        int nextIndex = currentIndex + direction;

        if (wrapAround)
        {
            if (nextIndex < 0)
                nextIndex = items.Count - 1;
            else if (nextIndex >= items.Count)
                nextIndex = 0;
        }
        else
        {
            nextIndex = Mathf.Clamp(nextIndex, 0, items.Count - 1);
        }

        if (nextIndex == currentIndex)
            return;

        currentIndex = nextIndex;
        RefreshSelection();
    }

    private void RefreshSelection(bool immediate = false)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                items[i].SetSelected(i == currentIndex, immediate);
        }
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
            navigateAction?.action.Disable();
            submitAction?.action.Disable();
            cancelAction?.action.Disable();
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
        Transform root = itemsRoot != null ? itemsRoot : transform;
        items.Clear();

        for (int i = 0; i < root.childCount; i++)
        {
            SpriteMenuItem item = root.GetChild(i).GetComponent<SpriteMenuItem>();
            if (item != null)
                items.Add(item);
        }
    }

    public void SetIndex(int index)
    {
        if (items.Count == 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);
        RefreshSelection();
        ResetHold();
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public SpriteMenuItem GetCurrentItem()
    {
        if (items.Count == 0)
            return null;

        return items[currentIndex];
    }
}
