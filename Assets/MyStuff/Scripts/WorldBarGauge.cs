using UnityEngine;

/// <summary>
/// ワールド空間に配置されるバーゲージUIコンポーネント。
/// fillTransform の「初期ローカルスケール」をゲージ満タン時のスケールとして記憶し、
/// 現在値 / 最大値の割合に応じて指定軸だけを縮める。
/// </summary>
public class WorldBarGauge : MonoBehaviour
{
    private enum FillAxis
    {
        X,
        Y,
    }

    [Header("Fill Object")]
    [SerializeField] private Transform fillTransform;
    [SerializeField] private FillAxis fillAxis = FillAxis.X;

    [Header("Full Scale")]
    [Tooltip("ONにすると、シーン/Prefab上で設定したFillの初期Scaleを満タン時のScaleとして使用します。")]
    [SerializeField] private bool useInitialScaleAsFullScale = true;

    [Tooltip("useInitialScaleAsFullScaleがOFFの場合に使う、満タン時のScaleです。")]
    [SerializeField] private Vector3 manualFullScale = Vector3.one;

    [Tooltip("ONにすると、Awake/OnEnable時点のFillの位置も基準位置として記憶します。")]
    [SerializeField] private bool cacheInitialPosition = true;

    [Header("Animation")]
    [SerializeField] private bool smoothAnimation = true;
    [SerializeField] private float animationSpeed = 10f;

    [Header("Debug / Safety")]
    [Tooltip("満タン時Scaleのうち、ゲージ方向の軸が0だった場合に警告します。0のままだとHP割合を掛けても見た目は伸びません。")]
    [SerializeField] private bool warnIfFillAxisScaleIsZero = true;

    private Vector3 fullScale = Vector3.one;
    private Vector3 initialPosition;
    private float currentRatio = 1f;
    private float targetRatio = 1f;
    private bool initialized;

    private void Awake()
    {
        CacheFullTransform();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            CacheFullTransform();
        }

        ApplyRatio(currentRatio);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

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

    /// <summary>
    /// 現在値と最大値からゲージ割合を設定する。
    /// </summary>
    public void SetValue(int current, int max)
    {
        SetRatio(max <= 0 ? 0f : (float)current / max);
    }

    /// <summary>
    /// 0〜1の割合でゲージを更新する。
    /// smoothAnimationがONなら徐々に近づく。
    /// </summary>
    public void SetRatio(float ratio)
    {
        targetRatio = Mathf.Clamp01(ratio);
    }

    /// <summary>
    /// 0〜1の割合でゲージを即時更新する。
    /// </summary>
    public void SetImmediate(float ratio)
    {
        targetRatio = Mathf.Clamp01(ratio);
        currentRatio = targetRatio;
        ApplyRatio(currentRatio);
    }

    /// <summary>
    /// 現在のFill TransformのScale/Positionを、満タン時の基準として取り直す。
    /// UIを手で調整した後に呼ぶと、その状態が新しい満タン状態になる。
    /// </summary>
    public void ResetGaugeTransformCache()
    {
        CacheFullTransform();
        ApplyRatio(currentRatio);
    }

    /// <summary>
    /// 外部から満タン時Scaleを明示的に設定したい場合に使う。
    /// </summary>
    public void SetFullScale(Vector3 newFullScale, bool applyImmediately = true)
    {
        fullScale = newFullScale;
        manualFullScale = newFullScale;
        useInitialScaleAsFullScale = false;
        initialized = true;

        if (applyImmediately)
        {
            ApplyRatio(currentRatio);
        }
    }

    private void CacheFullTransform()
    {
        if (fillTransform == null)
        {
            Debug.LogError($"{name}: fillTransform が未設定です。", this);
            enabled = false;
            initialized = false;
            return;
        }

        fullScale = useInitialScaleAsFullScale ? fillTransform.localScale : manualFullScale;

        if (cacheInitialPosition)
        {
            initialPosition = fillTransform.localPosition;
        }

        if (warnIfFillAxisScaleIsZero)
        {
            float axisScale = fillAxis == FillAxis.X ? fullScale.x : fullScale.y;
            if (Mathf.Approximately(axisScale, 0f))
            {
                Debug.LogWarning(
                    $"{name}: Fillの満タン時Scaleの{fillAxis}軸が0です。" +
                    "この軸をゲージ幅として使う場合、HP割合を反映しても見た目が伸びません。" +
                    "Prefab/Scene上のFill Scaleを満タン時の大きさに設定してください。",
                    this);
            }
        }

        initialized = true;
    }

    private void ApplyRatio(float ratio)
    {
        if (fillTransform == null)
        {
            return;
        }

        Vector3 scale = fullScale;
        float clampedRatio = Mathf.Clamp01(ratio);

        if (fillAxis == FillAxis.X)
        {
            scale.x = fullScale.x * clampedRatio;
        }
        else
        {
            scale.y = fullScale.y * clampedRatio;
        }

        fillTransform.localScale = scale;

        if (cacheInitialPosition)
        {
            fillTransform.localPosition = initialPosition;
        }
    }
}
