using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// スロットやボタンをホバー・押下した際の拡大縮小などのフィードバックエフェクトを提供する。
/// </summary>
public class SlotHoverPressFx : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler
{
    [Header("References")]
    [SerializeField] private Transform animatedTarget;
    [SerializeField] private SpriteRenderer hoverFrameRenderer;

    [Header("Hover")]
    [SerializeField] private Color hoverFrameColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float hoverFadeDuration = 0.12f;
    [SerializeField] private bool scaleOnHover = false;
    [SerializeField] private float hoverScaleMultiplier = 1.03f;

    [Header("Press")]
    [SerializeField, Range(0.5f, 1f)] private float pressedScaleMultiplier = 0.92f;
    [SerializeField, Min(0f)] private float pressDownDuration = 0.05f;
    [SerializeField, Min(0f)] private float pressUpDuration = 0.09f;
    [SerializeField] private Ease pressDownEase = Ease.OutQuad;
    [SerializeField] private Ease pressUpEase = Ease.OutBack;

    [Header("Audio")]
    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField] private AudioClip clickSe;
    [SerializeField, Range(0f, 1f)] private float clickSeVolume = 1f;

    private Vector3 baseScale = Vector3.one;
    private Tween hoverFrameTween;
    private Tween scaleTween;

    private void Awake()
    {
        CacheBaseScale();
        ApplyHoverVisualImmediate(false);
    }

    private void OnEnable()
    {
        CacheBaseScale();
        ApplyHoverVisualImmediate(false);
    }

    private void OnDisable()
    {
        hoverFrameTween?.Kill(false);
        scaleTween?.Kill(false);
        ApplyHoverVisualImmediate(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverVisual(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayHoverVisual(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayPressFeedback();
    }

    public void PlayPressFeedback()
    {
        PlayClickSound();

        Transform target = GetAnimatedTarget();
        if (target == null)
        {
            return;
        }

        scaleTween?.Kill(false);

        Vector3 hoverScale = GetHoverScale();
        Vector3 pressedScale = hoverScale * pressedScaleMultiplier;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(target.DOScale(pressedScale, pressDownDuration).SetEase(pressDownEase));
        sequence.Append(target.DOScale(hoverScale, pressUpDuration).SetEase(pressUpEase));
        scaleTween = sequence;
    }


    private void PlayClickSound()
    {
        if (clickSe == null)
        {
            return;
        }

        if (clickAudioSource != null)
        {
            clickAudioSource.PlayOneShot(clickSe, clickSeVolume);
            return;
        }

        Camera mainCamera = Camera.main;
        Vector3 playPosition = mainCamera != null ? mainCamera.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clickSe, playPosition, clickSeVolume);
    }

    private void CacheBaseScale()
    {
        Transform target = GetAnimatedTarget();
        if (target != null)
        {
            baseScale = target.localScale;
        }
    }

    private Transform GetAnimatedTarget()
    {
        return animatedTarget != null ? animatedTarget : transform;
    }

    private Vector3 GetHoverScale()
    {
        if (scaleOnHover)
        {
            return baseScale * hoverScaleMultiplier;
        }

        return baseScale;
    }

    private void PlayHoverVisual(bool show)
    {
        if (hoverFrameRenderer != null)
        {
            hoverFrameTween?.Kill(false);
            Color color = hoverFrameColor;
            float targetAlpha = show ? hoverFrameColor.a : 0f;
            hoverFrameTween = hoverFrameRenderer.DOFade(targetAlpha, hoverFadeDuration);
            hoverFrameRenderer.color = new Color(color.r, color.g, color.b, hoverFrameRenderer.color.a);
            hoverFrameRenderer.enabled = true;
        }

        Transform target = GetAnimatedTarget();
        if (target != null && scaleOnHover)
        {
            scaleTween?.Kill(false);
            scaleTween = target.DOScale(show ? GetHoverScale() : baseScale, hoverFadeDuration).SetEase(Ease.OutQuad);
        }
    }

    private void ApplyHoverVisualImmediate(bool show)
    {
        if (hoverFrameRenderer != null)
        {
            Color color = hoverFrameColor;
            hoverFrameRenderer.color = new Color(color.r, color.g, color.b, show ? color.a : 0f);
            hoverFrameRenderer.enabled = true;
        }

        Transform target = GetAnimatedTarget();
        if (target != null)
        {
            target.localScale = show ? GetHoverScale() : baseScale;
        }
    }
}
