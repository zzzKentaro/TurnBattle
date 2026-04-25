using UnityEngine;

/// <summary>
/// ワールド座標ベースの被弾演出用アンカー。
/// UI / Canvas は一切使わず、effect と damage popup の出現位置を返す。
/// </summary>
public class DamageTargetAnchor : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] private Transform worldAnchor;

    [Header("World Offsets")]
    [SerializeField] private Vector3 effectWorldOffset = Vector3.zero;
    [SerializeField] private Vector3 damageWorldOffset = new Vector3(0f, 0.8f, 0f);

    [Header("Optional Facing")]
    [SerializeField] private bool flipOffsetByLocalScaleX = false;

    public Transform AnchorTransform => worldAnchor != null ? worldAnchor : transform;

    public Vector3 GetEffectWorldPosition()
    {
        return GetBaseWorldPosition() + ApplyFacing(effectWorldOffset);
    }

    public Vector3 GetDamageWorldPosition()
    {
        return GetBaseWorldPosition() + ApplyFacing(damageWorldOffset);
    }

    private Vector3 GetBaseWorldPosition()
    {
        Transform anchor = worldAnchor != null ? worldAnchor : transform;
        return anchor.position;
    }

    private Vector3 ApplyFacing(Vector3 offset)
    {
        if (!flipOffsetByLocalScaleX)
        {
            return offset;
        }

        Transform anchor = worldAnchor != null ? worldAnchor : transform;
        float sign = Mathf.Sign(anchor.lossyScale.x);
        if (Mathf.Approximately(sign, 0f))
        {
            sign = 1f;
        }

        return new Vector3(offset.x * sign, offset.y, offset.z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 effectPos = GetEffectWorldPosition();
        Vector3 damagePos = GetDamageWorldPosition();

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        Gizmos.DrawSphere(effectPos, 0.06f);
        Gizmos.DrawLine(GetBaseWorldPosition(), effectPos);

        Gizmos.color = new Color(0.2f, 1f, 1f, 0.9f);
        Gizmos.DrawSphere(damagePos, 0.06f);
        Gizmos.DrawLine(GetBaseWorldPosition(), damagePos);
    }
#endif
}
