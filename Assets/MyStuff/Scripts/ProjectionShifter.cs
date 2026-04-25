using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// プレイヤーターン / 敵ターンの見た目切り替え演出。
/// 元の ProjectionShifter の見た目を保ちつつ、BattleManager から Coroutine として待てるようにした版。
/// </summary>
public class ProjectionShifter : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject middleLine;
    [SerializeField] private GameObject upperLine;
    [SerializeField] private GameObject lowerLine;
    [SerializeField] private GameObject hpBrock;
    [SerializeField] private GameObject casterPanel;
    [SerializeField] private GameObject magicPanel;

    [SerializeField] private GameObject phaseShifterPlayerTurn;
    [SerializeField] private GameObject phaseShifterEnemyTurn;

    [SerializeField] private GameObject casterPanelHikaePosition;
    [SerializeField] private GameObject casterPanelPosition;
    [SerializeField] private GameObject magicPanelHikaePosition;
    [SerializeField] private GameObject magicPanelPosition;

    [SerializeField] private GameObject enemyObj;

    [Header("Scales")]
    [SerializeField] private float playerTurnEnemyScale = 1f;
    [SerializeField] private float enemyTurnEnemyScale = 1.15f;
    [SerializeField] private float phaseShifterScale = 0.08f;

    [Header("Timing")]
    [SerializeField] private float openDuration = 0.3f;
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private float panelMoveDuration = 0.3f;

    [Header("Rotation")]
    [SerializeField] private Vector3 minusRotation = new Vector3(0f, 0f, -3f);
    [SerializeField] private Vector3 plusRotation = new Vector3(0f, 0f, 3f);

    [Header("Initial")]
    [SerializeField] private bool startAsPlayerTurn = true;

    [Header("演出完了時に呼ばれる（インスペクタから設定可能）")]
    [SerializeField] private UnityEvent onShiftComplete;

    private bool isPlayerTurn;
    private bool isShifting;
    private Sequence currentSequence;

    public bool IsShifting => isShifting;
    public bool IsPlayerTurn => isPlayerTurn;

    private void Awake()
    {
        SnapTo(startAsPlayerTurn);
    }

    public void SnapToPlayerTurn()
    {
        SnapTo(true);
    }

    public void SnapToEnemyTurn()
    {
        SnapTo(false);
    }

    public void BackLineShift(Action onComplete = null)
    {
        if (isShifting)
        {
            return;
        }

        if (isPlayerTurn)
        {
            PlayShift(false, onComplete);
        }
        else
        {
            PlayShift(true, onComplete);
        }
    }

    public IEnumerator ShiftToEnemyTurnRoutine(Action onComplete = null)
    {
        if (isPlayerTurn == false && isShifting == false)
        {
            onComplete?.Invoke();
            yield break;
        }

        bool finished = false;
        PlayShift(false, () =>
        {
            finished = true;
            onComplete?.Invoke();
        });

        while (!finished)
        {
            yield return null;
        }
    }

    public IEnumerator ShiftToPlayerTurnRoutine(Action onComplete = null)
    {
        if (isPlayerTurn && isShifting == false)
        {
            onComplete?.Invoke();
            yield break;
        }

        bool finished = false;
        PlayShift(true, () =>
        {
            finished = true;
            onComplete?.Invoke();
        });

        while (!finished)
        {
            yield return null;
        }
    }

    private void PlayShift(bool toPlayerTurn, Action onComplete)
    {
        if (isShifting)
        {
            return;
        }

        isShifting = true;
        currentSequence?.Kill();

        if (toPlayerTurn)
        {
            PlayShiftToPlayerTurn(onComplete);
        }
        else
        {
            PlayShiftToEnemyTurn(onComplete);
        }
    }

    private void PlayShiftToEnemyTurn(Action onComplete)
    {
        if (phaseShifterEnemyTurn != null)
        {
            phaseShifterEnemyTurn.SetActive(true);
            phaseShifterEnemyTurn.transform.localScale = new Vector3(-phaseShifterScale, 0f, phaseShifterScale);
        }

        MovePanelToHidden(casterPanel, casterPanelHikaePosition, true);
        MovePanelToHidden(magicPanel, magicPanelHikaePosition, true);
        ScaleEnemy(enemyTurnEnemyScale);

        currentSequence = DOTween.Sequence();

        if (phaseShifterEnemyTurn != null)
        {
            currentSequence.Append(phaseShifterEnemyTurn.transform.DOScale(
                new Vector3(-phaseShifterScale, phaseShifterScale, phaseShifterScale), openDuration));
        }
        else
        {
            currentSequence.AppendInterval(openDuration);
        }

        JoinTurnLineRotation(currentSequence, plusRotation, minusRotation);

        currentSequence
            .AppendInterval(holdDuration)
            .AppendCallback(() => { })
            .Append(phaseShifterEnemyTurn != null
                ? phaseShifterEnemyTurn.transform.DOScale(new Vector3(-phaseShifterScale, 0f, phaseShifterScale), closeDuration)
                : DOVirtual.DelayedCall(closeDuration, () => { }))
            .AppendCallback(() =>
            {
                if (phaseShifterEnemyTurn != null)
                {
                    phaseShifterEnemyTurn.SetActive(false);
                }
            })
            .OnComplete(() =>
            {
                isPlayerTurn = false;
                isShifting = false;
                currentSequence = null;
                onShiftComplete?.Invoke();
                onComplete?.Invoke();
            });
    }

    private void PlayShiftToPlayerTurn(Action onComplete)
    {
        if (phaseShifterPlayerTurn != null)
        {
            phaseShifterPlayerTurn.SetActive(true);
            phaseShifterPlayerTurn.transform.localScale = new Vector3(phaseShifterScale, 0f, phaseShifterScale);
        }

        MovePanelToVisible(casterPanel, casterPanelPosition);
        MovePanelToVisible(magicPanel, magicPanelPosition);
        ScaleEnemy(playerTurnEnemyScale);

        currentSequence = DOTween.Sequence();

        if (phaseShifterPlayerTurn != null)
        {
            currentSequence.Append(phaseShifterPlayerTurn.transform.DOScale(
                new Vector3(phaseShifterScale, phaseShifterScale, phaseShifterScale), openDuration));
        }
        else
        {
            currentSequence.AppendInterval(openDuration);
        }

        JoinTurnLineRotation(currentSequence, minusRotation, plusRotation);

        currentSequence
            .AppendInterval(holdDuration)
            .Append(phaseShifterPlayerTurn != null
                ? phaseShifterPlayerTurn.transform.DOScale(new Vector3(phaseShifterScale, 0f, phaseShifterScale), closeDuration)
                : DOVirtual.DelayedCall(closeDuration, () => { }))
            .AppendCallback(() =>
            {
                if (phaseShifterPlayerTurn != null)
                {
                    phaseShifterPlayerTurn.SetActive(false);
                }
            })
            .OnComplete(() =>
            {
                isPlayerTurn = true;
                isShifting = false;
                currentSequence = null;
                onShiftComplete?.Invoke();
                onComplete?.Invoke();
            });
    }

    private void SnapTo(bool toPlayerTurn)
    {
        currentSequence?.Kill();
        currentSequence = null;
        isShifting = false;
        isPlayerTurn = toPlayerTurn;

        if (phaseShifterPlayerTurn != null)
        {
            phaseShifterPlayerTurn.SetActive(false);
        }

        if (phaseShifterEnemyTurn != null)
        {
            phaseShifterEnemyTurn.SetActive(false);
        }

        if (toPlayerTurn)
        {
            SetPanelImmediate(casterPanel, casterPanelPosition, true);
            SetPanelImmediate(magicPanel, magicPanelPosition, true);
            SetEnemyScaleImmediate(playerTurnEnemyScale);
            SetTurnLineRotationImmediate(minusRotation, plusRotation);
        }
        else
        {
            SetPanelImmediate(casterPanel, casterPanelHikaePosition, false);
            SetPanelImmediate(magicPanel, magicPanelHikaePosition, false);
            SetEnemyScaleImmediate(enemyTurnEnemyScale);
            SetTurnLineRotationImmediate(plusRotation, minusRotation);
        }
    }

    private void MovePanelToHidden(GameObject panel, GameObject hidePosition, bool disableAfterMove)
    {
        if (panel == null || hidePosition == null)
        {
            return;
        }

        panel.SetActive(true);
        panel.transform.DOMove(hidePosition.transform.position, panelMoveDuration)
            .SetUpdate(false)
            .OnComplete(() =>
            {
                if (disableAfterMove)
                {
                    panel.SetActive(false);
                }
            });
    }

    private void MovePanelToVisible(GameObject panel, GameObject showPosition)
    {
        if (panel == null || showPosition == null)
        {
            return;
        }

        panel.SetActive(true);
        panel.transform.DOMove(showPosition.transform.position, panelMoveDuration).SetUpdate(false);
    }

    private void ScaleEnemy(float targetScale)
    {
        if (enemyObj == null)
        {
            return;
        }

        enemyObj.transform.DOScale(new Vector3(targetScale, targetScale, targetScale), panelMoveDuration).SetUpdate(false);
    }

    private void SetEnemyScaleImmediate(float targetScale)
    {
        if (enemyObj == null)
        {
            return;
        }

        enemyObj.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
    }

    private void SetPanelImmediate(GameObject panel, GameObject targetPosition, bool active)
    {
        if (panel == null)
        {
            return;
        }

        if (targetPosition != null)
        {
            panel.transform.position = targetPosition.transform.position;
        }

        panel.SetActive(active);
    }

    private void JoinTurnLineRotation(Sequence sequence, Vector3 upperTargetRotation, Vector3 lowerTargetRotation)
    {
        if (sequence == null)
        {
            return;
        }

        if (upperLine != null)
        {
            sequence.Join(upperLine.transform.DORotate(upperTargetRotation, openDuration, RotateMode.Fast));
        }

        if (lowerLine != null)
        {
            sequence.Join(lowerLine.transform.DORotate(lowerTargetRotation, openDuration, RotateMode.Fast));
        }
    }

    private void SetTurnLineRotationImmediate(Vector3 upperRotation, Vector3 lowerRotation)
    {
        if (upperLine != null)
        {
            upperLine.transform.eulerAngles = upperRotation;
        }

        if (lowerLine != null)
        {
            lowerLine.transform.eulerAngles = lowerRotation;
        }
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
        isShifting = false;
    }
}
