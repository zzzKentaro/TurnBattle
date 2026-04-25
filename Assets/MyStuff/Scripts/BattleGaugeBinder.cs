using UnityEngine;
using TurnBasedBattle;

public class BattleGaugeBinder : MonoBehaviour
{
    [Header("Units")]
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    [Header("Gauges")]
    [SerializeField] private WorldBarGauge playerHpGauge;
    [SerializeField] private WorldBarGauge playerMpGauge;
    [SerializeField] private WorldBarGauge enemyHpGauge;

    private void Start()
    {
        RefreshImmediate();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (playerUnit != null)
        {
            if (playerHpGauge != null)
            {
                playerHpGauge.SetRatio((float)playerUnit.CurrentHP / Mathf.Max(1, playerUnit.MaxHP));
            }

            if (playerMpGauge != null)
            {
                playerMpGauge.SetRatio((float)playerUnit.CurrentMP / Mathf.Max(1, playerUnit.MaxMP));
            }
        }

        if (enemyUnit != null && enemyHpGauge != null)
        {
            enemyHpGauge.SetRatio((float)enemyUnit.CurrentHP / Mathf.Max(1, enemyUnit.MaxHP));
        }
    }

    private void RefreshImmediate()
    {
        if (playerUnit != null)
        {
            if (playerHpGauge != null)
            {
                playerHpGauge.SetImmediate((float)playerUnit.CurrentHP / Mathf.Max(1, playerUnit.MaxHP));
            }

            if (playerMpGauge != null)
            {
                playerMpGauge.SetImmediate((float)playerUnit.CurrentMP / Mathf.Max(1, playerUnit.MaxMP));
            }
        }

        if (enemyUnit != null && enemyHpGauge != null)
        {
            enemyHpGauge.SetImmediate((float)enemyUnit.CurrentHP / Mathf.Max(1, enemyUnit.MaxHP));
        }
    }
}