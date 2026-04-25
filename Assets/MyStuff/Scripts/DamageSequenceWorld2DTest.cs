using UnityEngine;

/// <summary>
/// 動作確認用の簡易テスト。
/// 1: enemy に単発
/// 2: player と enemy に同時 popup
/// 3: enemy に 3連続
/// </summary>
public class DamageSequenceWorld2DTest : MonoBehaviour
{
    [SerializeField] private DamageSequencePlayer damageSequencePlayer;
    [SerializeField] private DamageTargetAnchor playerTarget;
    [SerializeField] private DamageTargetAnchor enemyTarget;

    private void Update()
    {
        if (damageSequencePlayer == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && enemyTarget != null)
        {
            damageSequencePlayer.PlayHit(enemyTarget, 128);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && playerTarget != null && enemyTarget != null)
        {
            damageSequencePlayer.PlayMultiHit(new[]
            {
                new DamageSequencePlayer.DamageHitRequest { target = enemyTarget, damage = 246 },
                new DamageSequencePlayer.DamageHitRequest { target = playerTarget, damage = 135 },
            });
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) && enemyTarget != null)
        {
            damageSequencePlayer.PlayHitsSequentially(new[]
            {
                new DamageSequencePlayer.DamageHitRequest { target = enemyTarget, damage = 91 },
                new DamageSequencePlayer.DamageHitRequest { target = enemyTarget, damage = 103 },
                new DamageSequencePlayer.DamageHitRequest { target = enemyTarget, damage = 117 },
            });
        }
    }
}
