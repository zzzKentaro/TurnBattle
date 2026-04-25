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

    [Header("Background Color")]
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
    }

    private void OnDisable()
    {
        KillScaleTween();
    }

    private void OnDestroy()
    {
        KillScaleTween();
    }

    public void SetSelected(bool isSelected, bool immediate = false)
    {
        if (targetRenderer != null)
        {
            targetRenderer.color = isSelected ? selectedColor : normalColor;
        }

        if (iconRenderer != null && changeIconColor)
        {
            iconRenderer.color = isSelected ? selectedIconColor : normalIconColor;
        }

        if (labelText != null && changeLabelColor)
        {
            labelText.color = isSelected ? selectedLabelColor : normalLabelColor;
        }

        if (selectedMarker != null)
        {
            selectedMarker.SetActive(isSelected);
        }

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

    public void Submit()
    {
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
