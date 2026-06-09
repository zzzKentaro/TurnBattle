using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpriteMaskedVerticalMenuの階層（スタック）を管理し、メニューの遷移を制御する。
/// </summary>
public class SpriteMenuStackController : MonoBehaviour
{
    [Header("Menus")]
    /// <summary>
    /// メニュー階層の一番根っこ（起点）となるメニューオブジェクト
    /// </summary>
    [SerializeField] private GameObject rootMenu;

    /// <summary>
    /// このコントローラーで管理するすべてのメニューオブジェクトのリスト
    /// </summary>
    [SerializeField] private List<GameObject> managedMenus = new List<GameObject>();

    [Header("Display")]
    /// <summary>
    /// 新しいメニューを開いたとき、これまでのメニューを非表示（SetActivate(false)）にするかどうか。
    /// falseの場合は表示したまま入力のみ無効化（SetInputEnabled(false)）します。
    /// </summary>
    [SerializeField] private bool hidePreviousMenuWhenOpening = true;

    /// <summary>
    /// 開いてきたメニューの履歴を保存するスタック
    /// </summary>
    private readonly Stack<GameObject> menuStack = new Stack<GameObject>();

    /// <summary>
    /// 現在アクティブになっているメニュー
    /// </summary>
    private GameObject currentMenu;

    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    private void Start()
    {
        InitializeMenus();
    }

    /// <summary>
    /// 指定したメニューを開き、現在のメニューを履歴（スタック）に積みます。
    /// </summary>
    /// <param name="nextMenu">次に開くメニューのGameObject</param>
    public void OpenMenu(GameObject nextMenu)
    {
        if (nextMenu == null)
            return;

        if (currentMenu == nextMenu)
            return;

        if (currentMenu != null)
        {
            menuStack.Push(currentMenu);
            DeactivateMenu(currentMenu);
        }

        ActivateMenu(nextMenu);
        currentMenu = nextMenu;
    }

    /// <summary>
    /// 履歴（スタック）から一つ前のメニューを取り出して戻ります。
    /// 履歴が空の場合はルートメニューに戻ります。
    /// </summary>
    public void Back()
    {
        if (menuStack.Count == 0)
        {
            ReturnToRoot();
            return;
        }

        if (currentMenu != null)
        {
            // Backで戻る＝現在のメニューを「閉じる」ため、完全に非表示にする
            CloseMenu(currentMenu);
        }

        GameObject previousMenu = menuStack.Pop();
        ActivateMenu(previousMenu);
        currentMenu = previousMenu;
        Debug.Log("キャンセル（Back）が呼ばれました");
    }

    /// <summary>
    /// 履歴（スタック）をすべて破棄し、ルートメニューまで一気に戻ります。
    /// </summary>
    public void ReturnToRoot()
    {
        if (rootMenu == null)
            return;

        if (currentMenu != null && currentMenu != rootMenu)
        {
            // 閉じるメニューは完全に非表示にする
            CloseMenu(currentMenu);
        }

        while (menuStack.Count > 0)
        {
            GameObject stackedMenu = menuStack.Pop();
            if (stackedMenu != null && stackedMenu != rootMenu)
            {
                // スタックされていた中間メニューも完全に非表示にする
                CloseMenu(stackedMenu);
            }
        }

        ActivateMenu(rootMenu);
        currentMenu = rootMenu;
    }

    /// <summary>
    /// メニューの履歴（スタック）をすべて消去します。
    /// </summary>
    public void ClearHistory()
    {
        menuStack.Clear();
    }

    /// <summary>
    /// 管理対象のメニューの初期状態（表示・非表示、入力の有効・無効）を一括で設定します。
    /// ルートメニューのみがアクティブになります。
    /// </summary>
    private void InitializeMenus()
    {
        for (int i = 0; i < managedMenus.Count; i++)
        {
            GameObject menu = managedMenus[i];
            if (menu == null)
                continue;

            bool isRoot = menu == rootMenu;

            // 初期状態ではルートメニュー以外は必ず非表示にする
            menu.SetActive(isRoot);
            SetMenuInputEnabled(menu, isRoot);
        }

        if (rootMenu != null)
        {
            if (hidePreviousMenuWhenOpening)
                rootMenu.SetActive(true);
            else
                SetMenuInputEnabled(rootMenu, true);

            currentMenu = rootMenu;
        }
    }

    /// <summary>
    /// 指定されたメニューをアクティブ（表示および入力有効）にします。
    /// </summary>
    /// <param name="menu">アクティブにするメニュー</param>
    private void ActivateMenu(GameObject menu)
    {
        if (menu == null)
            return;

        menu.SetActive(true);
        SetMenuInputEnabled(menu, true);
    }

    /// <summary>
    /// 指定されたメニューを非アクティブ（非表示、または入力無効）にします。
    /// </summary>
    /// <param name="menu">非アクティブにするメニュー</param>
    private void DeactivateMenu(GameObject menu)
    {
        if (menu == null)
            return;

        if (hidePreviousMenuWhenOpening)
            menu.SetActive(false);
        else
            SetMenuInputEnabled(menu, false);
    }

    /// <summary>
    /// メニューを完全に閉じる（非表示にする）処理です。
    /// </summary>
    /// <param name="menu">閉じるメニュー</param>
    private void CloseMenu(GameObject menu)
    {
        if (menu == null)
            return;

        menu.SetActive(false);
    }

    /// <summary>
    /// メニュー内の入力の有効・無効を切り替えます。
    /// </summary>
    /// <param name="menu">対象のメニュー</param>
    /// <param name="enabled">入力を有効にするかどうか</param>
    private void SetMenuInputEnabled(GameObject menu, bool enabled)
    {
        if (menu == null)
            return;

        SpriteMaskedVerticalMenu maskedMenu = menu.GetComponent<SpriteMaskedVerticalMenu>();
        if (maskedMenu != null)
            maskedMenu.SetInputEnabled(enabled);
    }
}
