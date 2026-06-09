using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ワールド座標上に数字スプライトを並べて、ふわっと上に飛ばしながらフェードアウトする。
/// UI は使わない。
/// </summary>
[RequireComponent(typeof(SortingGroup))]
public class DamageNumberPopup : MonoBehaviour
{
    [Header("Digit Prefab / Sprites")]
    [SerializeField] private SpriteRenderer digitPrefab;
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Header("Digit Layout (World Units)")]
    [SerializeField] private Vector2 digitSize = new Vector2(0.5f, 0.7f);
    [SerializeField] private float digitSpacing = 0.04f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.65f;
    [SerializeField] private float riseDistance = 0.9f;
    [SerializeField] private float jumpHeight = 0.25f;
    [SerializeField] private float scalePunch = 0.18f;
    [SerializeField] private float fadeStartNormalized = 0.45f;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool destroyOnComplete = true;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "DamageText";
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private Color digitColor = Color.white;

    private readonly List<SpriteRenderer> activeDigits = new List<SpriteRenderer>();

    private SortingGroup sortingGroup;
    private Vector3 startWorldPosition;
    private Vector3 baseScale;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        ApplySorting();
        baseScale = transform.localScale;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        startWorldPosition = worldPosition;
    }

    public void SetSorting(string layerName, int order)
    {
        sortingLayerName = layerName;
        sortingOrder = order;
        ApplySorting();
    }

    public void SetDigitColor(Color color)
    {
        digitColor = color;
        for (int i = 0; i < activeDigits.Count; i++)
        {
            if (activeDigits[i] == null) continue;
            Color c = digitColor;
            c.a = activeDigits[i].color.a;
            activeDigits[i].color = c;
        }
    }

    public void Play(int damage)
    {
        if (digitPrefab == null)
        {
            Debug.LogError("DamageNumberPopup: digitPrefab が未設定です。", this);
            return;
        }

        if (digitSprites == null || digitSprites.Length < 10)
        {
            Debug.LogError("DamageNumberPopup: digitSprites は 0～9 の10枚を設定してください。", this);
            return;
        }

        StopAllCoroutines();
        startWorldPosition = transform.position;
        BuildDigits(Mathf.Max(0, damage));
        StartCoroutine(CoPlay());
    }

    private void BuildDigits(int damage)
    {
        ClearActiveDigits();

        string text = damage.ToString();
        float totalWidth = (text.Length * digitSize.x) + ((text.Length - 1) * digitSpacing);
        float startX = -totalWidth * 0.5f + digitSize.x * 0.5f;

        for (int i = 0; i < text.Length; i++)
        {
            int digit = text[i] - '0';
            SpriteRenderer renderer = Instantiate(digitPrefab, transform);
            renderer.name = $"Digit_{digit}_{i}";
            renderer.sprite = digitSprites[digit];
            renderer.color = digitColor;
            renderer.transform.localPosition = new Vector3(startX + i * (digitSize.x + digitSpacing), 0f, 0f);
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = CalculateDigitLocalScale(renderer.sprite);
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            activeDigits.Add(renderer);
        }
    }

    private Vector3 CalculateDigitLocalScale(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector3.one;
        }

        Vector2 spriteSize = sprite.bounds.size;
        float scaleX = spriteSize.x > 0f ? digitSize.x / spriteSize.x : 1f;
        float scaleY = spriteSize.y > 0f ? digitSize.y / spriteSize.y : 1f;
        return new Vector3(scaleX, scaleY, 1f);
    }

    private IEnumerator CoPlay()
    {
        float time = 0f;
        transform.localScale = baseScale;
        SetDigitsAlpha(1f);

        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            float y = Mathf.Lerp(0f, riseDistance, EaseOutCubic(t));
            float jump = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = startWorldPosition + new Vector3(0f, y + jump, 0f);

            float scale = 1f + Mathf.Sin(t * Mathf.PI) * scalePunch;
            transform.localScale = baseScale * scale;

            float alphaT = t < fadeStartNormalized ? 0f : Mathf.InverseLerp(fadeStartNormalized, 1f, t);
            SetDigitsAlpha(1f - alphaT);

            yield return null;
        }

        transform.position = startWorldPosition + new Vector3(0f, riseDistance, 0f);
        SetDigitsAlpha(0f);

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    private void SetDigitsAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        for (int i = 0; i < activeDigits.Count; i++)
        {
            if (activeDigits[i] == null) continue;

            Color c = activeDigits[i].color;
            c.a = alpha;
            activeDigits[i].color = c;
        }
    }

    private void ApplySorting()
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
        }

        sortingGroup.sortingLayerName = sortingLayerName;
        sortingGroup.sortingOrder = sortingOrder;

        for (int i = 0; i < activeDigits.Count; i++)
        {
            if (activeDigits[i] == null) continue;
            activeDigits[i].sortingLayerName = sortingLayerName;
            activeDigits[i].sortingOrder = sortingOrder;
        }
    }

    private void ClearActiveDigits()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        activeDigits.Clear();
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}
