using DG.Tweening;
using UnityEngine;

/// <summary>
/// バトル開始時の環境設定および初期化を行うコンポーネント。
/// </summary>
public class StartEnvironment : MonoBehaviour
{
    [SerializeField] Vector3 origin;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        origin = this.gameObject.transform.localScale;
        this.gameObject.transform.DOScale(new Vector3(0, 0, 0), 0f)
            .OnComplete(() => this.gameObject.transform.DOScale(origin, 1f));
    }
}
