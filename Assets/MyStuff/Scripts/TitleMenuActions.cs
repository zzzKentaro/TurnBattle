using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面のメニューアクション（ゲーム開始など）の処理を行うコンポーネント。
/// </summary>
public class TitleMenuActions : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log("ゲーム開始");
        // SceneManager.LoadScene("GameScene");
    }

    public void OpenOptions()
    {
        Debug.Log("オプションを開く");
    }

    public void ExitGame()
    {
        Debug.Log("ゲーム終了");
        Application.Quit();
    }

    public void CancelMenu()
    {
        Debug.Log("キャンセル");
    }

    public void NextMenu()
    {
        Debug.Log("選択ボタンを押した");
    }
}