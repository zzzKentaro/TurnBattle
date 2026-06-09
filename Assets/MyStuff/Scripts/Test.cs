using UnityEngine;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Approve1(){
        Debug.Log("test");
    }
    public void Approve2(){
        Debug.Log("here");
    }

    public void GoToStageScene()
    {
        SceneManager.LoadScene("DemoStartScene");
    }
}
