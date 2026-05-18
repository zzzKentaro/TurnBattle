using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面全体を覆う黒いImageを使ってフェードを行うクラス。
/// FadeImageはCanvas内の一番手前に置く。
/// </summary>
public class FadeController : MonoBehaviour
{
    [Header("画面全体を覆う黒いImage")]
    [SerializeField] private Image fadeImage;

    [Header("通常のフェード時間")]
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        SetAlpha(0f);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 画面を黒くする。
    /// </summary>
    public IEnumerator FadeOut()
    {
        yield return FadeTo(1f, defaultFadeDuration);
    }

    /// <summary>
    /// 黒い画面から通常画面に戻す。
    /// </summary>
    public IEnumerator FadeIn()
    {
        yield return FadeTo(0f, defaultFadeDuration);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 指定した透明度までフェードする。
    /// alpha = 0 で透明、alpha = 1 で真っ黒。
    /// </summary>
    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        fadeImage.gameObject.SetActive(true);

        float startAlpha = fadeImage.color.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rate = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, rate);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    /// <summary>
    /// FadeImageの透明度を直接設定する。
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}