using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SpriteMenuItem : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private GameObject selectedMarker;
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private TMP_Text labelText;

    [Header("Background Sprite Swap")]
    [Tooltip("ONなら、選択状態を色ではなくSprite差し替えで表現します。")]
    [SerializeField] private bool useSelectionSprites = true;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [Tooltip("normalSpriteが未設定の場合、起動時のtargetRenderer.spriteを通常画像として使います。")]
    [SerializeField] private bool useInitialSpriteAsNormalSprite = true;

    [Header("Background Color - Sprite差し替えを使わない場合")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.4f, 1f);

    [Header("Icon Color")]
    [SerializeField] private bool changeIconColor = false;
    [SerializeField] private Color normalIconColor = Color.white;
    [SerializeField] private Color selectedIconColor = Color.white;

    [Header("Label Color")]
    [SerializeField] private bool changeLabelColor = false;
    [SerializeField] private Color normalLabelColor = Color.white;
    [SerializeField] private Color selectedLabelColor = Color.white;

    [Header("Disabled Visual")]
    [Tooltip("OFFにすると、選択不可でも見た目は変えず、Submitだけ無効化します。")]
    [SerializeField] private bool dimWhenNotInteractable = true;
    [SerializeField, Range(0.05f, 1f)] private float disabledAlpha = 0.45f;

    [Header("Scale")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 selectedScale = new Vector3(1.08f, 1.08f, 1f);

    [Header("Selection Animation")]
    [SerializeField] private float scaleTweenDuration = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.OutQuad;
    [SerializeField] private bool ignoreTimeScale = true;

    [Header("Action")]
    [SerializeField] private UnityEvent onSubmit;

    private Tween scaleTween;
    private bool isSelected;
    private bool isInteractable = true;

    public bool IsInteractable => isInteractable;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void OnValidate()
    {
        if (scaleTarget == null || targetRenderer == null || labelText == null || iconRenderer == null)
        {
            AutoAssignReferences();
        }
    }

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform;

        if (targetRenderer == null && scaleTarget != null)
            targetRenderer = scaleTarget.GetComponent<SpriteRenderer>();

        if (useInitialSpriteAsNormalSprite && normalSprite == null && targetRenderer != null)
            normalSprite = targetRenderer.sprite;
    }

    private void OnEnable()
    {
        ApplyVisualState(true);
    }

    private void OnDisable()
    {
        KillScaleTween();
    }

    private void OnDestroy()
    {
        KillScaleTween();
    }

    public void SetSelected(bool selected, bool immediate = false)
    {
        isSelected = selected;
        ApplyVisualState(immediate);
    }

    public void SetInteractable(bool interactable, bool immediate = true)
    {
        isInteractable = interactable;
        ApplyVisualState(immediate);
    }

    public void SetSelectionSprites(Sprite normal, Sprite selected, bool refreshVisual = true)
    {
        normalSprite = normal;
        selectedSprite = selected;

        if (refreshVisual)
            ApplyVisualState(true);
    }

    public void Submit()
    {
        if (!isInteractable)
            return;

        onSubmit?.Invoke();
    }

    public void SetLabelText(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    public void SetIconSprite(Sprite sprite)
    {
        if (iconRenderer != null)
            iconRenderer.sprite = sprite;
    }

    public void SetScaleTarget(Transform newScaleTarget)
    {
        if (newScaleTarget == null)
            return;

        scaleTarget = newScaleTarget;
    }

    public Transform GetScaleTarget()
    {
        return scaleTarget;
    }

    private void ApplyVisualState(bool immediate)
    {
        ApplyBackgroundVisual();
        ApplyIconVisual();
        ApplyLabelVisual();
        ApplyMarkerVisual();
        ApplyScaleVisual(immediate);
    }

    private void ApplyBackgroundVisual()
    {
        if (targetRenderer == null)
            return;

        if (useSelectionSprites)
        {
            Sprite nextSprite = isSelected ? selectedSprite : normalSprite;
            if (nextSprite != null)
                targetRenderer.sprite = nextSprite;

            Color c = Color.white;
            if (dimWhenNotInteractable && !isInteractable)
                c.a = disabledAlpha;
            targetRenderer.color = c;
        }
        else
        {
            Color c = isSelected ? selectedColor : normalColor;
            if (dimWhenNotInteractable && !isInteractable)
                c.a *= disabledAlpha;
            targetRenderer.color = c;
        }
    }

    private void ApplyIconVisual()
    {
        if (iconRenderer == null)
            return;

        Color c = changeIconColor
            ? (isSelected ? selectedIconColor : normalIconColor)
            : Color.white;

        if (dimWhenNotInteractable && !isInteractable)
            c.a *= disabledAlpha;

        iconRenderer.color = c;
    }

    private void ApplyLabelVisual()
    {
        if (labelText == null)
            return;

        Color c = changeLabelColor
            ? (isSelected ? selectedLabelColor : normalLabelColor)
            : labelText.color;

        if (dimWhenNotInteractable && !isInteractable)
            c.a = disabledAlpha;
        else if (!changeLabelColor)
            c.a = 1f;

        labelText.color = c;
    }

    private void ApplyMarkerVisual()
    {
        if (selectedMarker != null)
            selectedMarker.SetActive(isSelected);
    }

    private void ApplyScaleVisual(bool immediate)
    {
        if (scaleTarget == null)
            return;

        Vector3 targetScale = isSelected ? selectedScale : normalScale;

        KillScaleTween();

        if (immediate || scaleTweenDuration <= 0f)
        {
            scaleTarget.localScale = targetScale;
            return;
        }

        scaleTween = scaleTarget
            .DOScale(targetScale, scaleTweenDuration)
            .SetEase(scaleEase)
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject);
    }

    private void KillScaleTween()
    {
        if (scaleTween == null)
            return;

        if (scaleTween.IsActive())
            scaleTween.Kill();

        scaleTween = null;
    }

    private void AutoAssignReferences()
    {
        if (iconRenderer == null)
        {
            Transform icon = FindChildByNameContains("icon");
            if (icon != null)
                iconRenderer = icon.GetComponent<SpriteRenderer>();
        }

        if (labelText == null)
        {
            Transform label = FindChildByNameContains("label", "text", "name");
            if (label != null)
                labelText = label.GetComponent<TMP_Text>();
        }

        if (scaleTarget == null)
        {
            scaleTarget = FindChildByNameContains("scale", "visual", "frame", "body", "background", "sprite");
        }

        if (scaleTarget == null)
        {
            scaleTarget = transform;
        }

        if (targetRenderer == null)
        {
            targetRenderer = scaleTarget.GetComponent<SpriteRenderer>();

            if (targetRenderer == iconRenderer)
                targetRenderer = null;

            if (targetRenderer == null)
            {
                foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer == null || renderer == iconRenderer)
                        continue;

                    targetRenderer = renderer;
                    break;
                }
            }
        }
    }

    private Transform FindChildByNameContains(params string[] keywords)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform)
                continue;

            string lowerName = child.name.ToLowerInvariant();
            for (int i = 0; i < keywords.Length; i++)
            {
                if (lowerName.Contains(keywords[i].ToLowerInvariant()))
                    return child;
            }
        }

        return null;
    }
}
