using UnityEngine;
using UnityEngine.SceneManagement;

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
}