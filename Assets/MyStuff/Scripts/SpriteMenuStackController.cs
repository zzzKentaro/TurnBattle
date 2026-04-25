using System.Collections.Generic;
using UnityEngine;

public class SpriteMenuStackController : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject rootMenu;
    [SerializeField] private List<GameObject> managedMenus = new List<GameObject>();

    [Header("Display")]
    [SerializeField] private bool hidePreviousMenuWhenOpening = true;

    private readonly Stack<GameObject> menuStack = new Stack<GameObject>();
    private GameObject currentMenu;

    private void Start()
    {
        InitializeMenus();
    }

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

    public void Back()
    {
        if (menuStack.Count == 0)
        {
            ReturnToRoot();
            return;
        }

        if (currentMenu != null)
        {
            DeactivateMenu(currentMenu);
        }

        GameObject previousMenu = menuStack.Pop();
        ActivateMenu(previousMenu);
        currentMenu = previousMenu;
    }

    public void ReturnToRoot()
    {
        if (rootMenu == null)
            return;

        if (currentMenu != null && currentMenu != rootMenu)
        {
            DeactivateMenu(currentMenu);
        }

        while (menuStack.Count > 0)
        {
            GameObject stackedMenu = menuStack.Pop();
            if (stackedMenu != null && stackedMenu != rootMenu)
            {
                if (hidePreviousMenuWhenOpening)
                    stackedMenu.SetActive(false);
                else
                    SetMenuInputEnabled(stackedMenu, false);
            }
        }

        ActivateMenu(rootMenu);
        currentMenu = rootMenu;
    }

    public void ClearHistory()
    {
        menuStack.Clear();
    }

    private void InitializeMenus()
    {
        for (int i = 0; i < managedMenus.Count; i++)
        {
            GameObject menu = managedMenus[i];
            if (menu == null)
                continue;

            bool isRoot = menu == rootMenu;

            if (hidePreviousMenuWhenOpening)
            {
                menu.SetActive(isRoot);
            }
            else
            {
                menu.SetActive(true);
                SetMenuInputEnabled(menu, isRoot);
            }
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

    private void ActivateMenu(GameObject menu)
    {
        if (menu == null)
            return;

        if (hidePreviousMenuWhenOpening)
            menu.SetActive(true);
        else
        {
            menu.SetActive(true);
            SetMenuInputEnabled(menu, true);
        }
    }

    private void DeactivateMenu(GameObject menu)
    {
        if (menu == null)
            return;

        if (hidePreviousMenuWhenOpening)
            menu.SetActive(false);
        else
            SetMenuInputEnabled(menu, false);
    }

    private void SetMenuInputEnabled(GameObject menu, bool enabled)
    {
        if (menu == null)
            return;

        SpriteMaskedVerticalMenu maskedMenu = menu.GetComponent<SpriteMaskedVerticalMenu>();
        if (maskedMenu != null)
            maskedMenu.SetInputEnabled(enabled);
    }
}
