using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ノベルゲーム用の入力受付クラス。
/// Input Systemを使って、キーボード・マウス・ゲームパッド入力をまとめて扱う。
/// </summary>
public class NovelInputHandler : MonoBehaviour
{
    public event Action OnNextPressed;
    public event Action OnBackToStartPressed;

    private InputAction nextAction;
    private InputAction backToStartAction;

    private void Awake()
    {
        // 「次へ進む」入力
        nextAction = new InputAction("Next", InputActionType.Button);

        nextAction.AddBinding("<Keyboard>/space");
        nextAction.AddBinding("<Keyboard>/enter");
        nextAction.AddBinding("<Mouse>/leftButton");

        // ゲームパッドの決定ボタン。
        // PlayStation系なら×ボタン、Xbox系ならAボタン相当。
        nextAction.AddBinding("<Gamepad>/buttonSouth");

        // 「最初へ戻る」入力
        backToStartAction = new InputAction("BackToStart", InputActionType.Button);
        backToStartAction.AddBinding("<Keyboard>/escape");
    }

    private void OnEnable()
    {
        nextAction.performed += HandleNext;
        backToStartAction.performed += HandleBackToStart;

        nextAction.Enable();
        backToStartAction.Enable();
    }

    private void OnDisable()
    {
        nextAction.performed -= HandleNext;
        backToStartAction.performed -= HandleBackToStart;

        nextAction.Disable();
        backToStartAction.Disable();
    }

    private void OnDestroy()
    {
        nextAction.Dispose();
        backToStartAction.Dispose();
    }

    private void HandleNext(InputAction.CallbackContext context)
    {
        OnNextPressed?.Invoke();
    }

    private void HandleBackToStart(InputAction.CallbackContext context)
    {
        OnBackToStartPressed?.Invoke();
    }
}