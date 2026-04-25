using DG.Tweening;
using UnityEngine;

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
