using UnityEngine;
using TurnBasedBattle;

/// <summary>
/// HPやMPなどのゲージをUI要素にバインドし、値の変動を同期させる。
/// </summary>
public class BattleGaugeBinder : MonoBehaviour
{
    [Header("Units")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    [Header("Gauges")]
    [SerializeField] private WorldBarGauge playerHpGauge;
    [SerializeField] private WorldBarGauge playerMpGauge;
    [SerializeField] private WorldBarGauge enemyHpGauge;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindAnyObjectByType<BattleManager>();
        }
    }

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.OnEnemyUnitChanged += HandleEnemyUnitChanged;
        }

        SyncUnitsFromBattleManager();
        RefreshImmediate();
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnEnemyUnitChanged -= HandleEnemyUnitChanged;
        }
    }

    private void Start()
    {
        SyncUnitsFromBattleManager();
        RefreshImmediate();
    }

    private void LateUpdate()
    {
        SyncUnitsFromBattleManager();
        Refresh();
    }

    private void Refresh()
    {
        if (playerUnit != null)
        {
            if (playerHpGauge != null)
            {
                playerHpGauge.SetValue(playerUnit.CurrentHP, playerUnit.MaxHP);
            }

            if (playerMpGauge != null)
            {
                playerMpGauge.SetValue(playerUnit.CurrentMP, playerUnit.MaxMP);
            }
        }

        if (enemyUnit != null && enemyHpGauge != null)
        {
            enemyHpGauge.SetValue(enemyUnit.CurrentHP, enemyUnit.MaxHP);
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

    private void HandleEnemyUnitChanged(BattleUnit nextEnemyUnit)
    {
        if (nextEnemyUnit == null)
        {
            return;
        }

        enemyUnit = nextEnemyUnit;
        WorldBarGauge nextEnemyGauge = nextEnemyUnit.GetComponentInChildren<WorldBarGauge>(true);
        if (nextEnemyGauge != null)
        {
            enemyHpGauge = nextEnemyGauge;
        }

        RefreshImmediate();
    }

    private void SyncUnitsFromBattleManager()
    {
        if (battleManager == null)
        {
            return;
        }

        if (battleManager.CurrentPlayerUnit != null)
        {
            playerUnit = battleManager.CurrentPlayerUnit;
        }

        if (battleManager.CurrentEnemyUnit != null && battleManager.CurrentEnemyUnit != enemyUnit)
        {
            HandleEnemyUnitChanged(battleManager.CurrentEnemyUnit);
        }
    }
}
