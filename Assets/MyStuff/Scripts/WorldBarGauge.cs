using UnityEngine;

public class WorldBarGauge : MonoBehaviour
{
    [Header("Fill Object")]
    [SerializeField] private Transform fillTransform;

    [Header("Animation")]
    [SerializeField] private bool smoothAnimation = true;
    [SerializeField] private float animationSpeed = 10f;

    private Vector3 initialScale;
    private float currentRatio = 1f;
    private float targetRatio = 1f;

    private void Awake()
    {
        if (fillTransform == null)
        {
            Debug.LogError($"{name}: fillTransform Ç™ñ¢ê›íËÇ≈Ç∑ÅB");
            enabled = false;
            return;
        }

        initialScale = fillTransform.localScale;
    }

    private void Update()
    {
        if (smoothAnimation)
        {
            currentRatio = Mathf.Lerp(currentRatio, targetRatio, 1f - Mathf.Exp(-animationSpeed * Time.deltaTime));
        }
        else
        {
            currentRatio = targetRatio;
        }

        ApplyRatio(currentRatio);
    }

    public void SetRatio(float ratio)
    {
        targetRatio = Mathf.Clamp01(ratio);
    }

    public void SetImmediate(float ratio)
    {
        targetRatio = Mathf.Clamp01(ratio);
        currentRatio = targetRatio;
        ApplyRatio(currentRatio);
    }

    private void ApplyRatio(float ratio)
    {
        Vector3 scale = initialScale;
        scale.x *= Mathf.Max(0f, ratio);
        fillTransform.localScale = scale;
    }
}