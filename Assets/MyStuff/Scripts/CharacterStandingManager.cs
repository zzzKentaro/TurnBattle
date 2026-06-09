using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 立ち絵の表示・非表示・差し替えを担当するクラス。
/// 初期版では Left / Center / Right の3か所に立ち絵を出せる。
/// </summary>
public class CharacterStandingManager : MonoBehaviour
{
    [Header("左の立ち絵Image")]
    [SerializeField] private Image leftImage;

    [Header("中央の立ち絵Image")]
    [SerializeField] private Image centerImage;

    [Header("右の立ち絵Image")]
    [SerializeField] private Image rightImage;

    private void Awake()
    {
        HideAll();
    }

    /// <summary>
    /// ページ開始時の立ち絵状態を反映する。
    /// ページが変わったら一度すべて消し、そのページの初期立ち絵を表示する。
    /// </summary>
    public void ApplyPageStartStandings(List<StandingCharacterData> startStandings)
    {
        HideAll();

        if (startStandings == null)
        {
            return;
        }

        foreach (StandingCharacterData data in startStandings)
        {
            ApplyStandingData(data);
        }
    }

    /// <summary>
    /// ブロック開始時の立ち絵変更を反映する。
    /// </summary>
    public void ApplyStandingChanges(List<StandingCharacterData> changes)
    {
        if (changes == null)
        {
            return;
        }

        foreach (StandingCharacterData data in changes)
        {
            ApplyStandingData(data);
        }
    }

    /// <summary>
    /// 立ち絵データ1つを反映する。
    /// visibleがtrueなら表示・差し替え、falseなら非表示。
    /// </summary>
    private void ApplyStandingData(StandingCharacterData data)
    {
        if (data == null)
        {
            return;
        }

        if (data.visible)
        {
            Show(data.position, data.characterSprite);
        }
        else
        {
            Hide(data.position);
        }
    }

    /// <summary>
    /// 指定位置に立ち絵を表示する。
    /// すでに表示されている場合は差し替える。
    /// </summary>
    public void Show(StandingPosition position, Sprite sprite)
    {
        Image targetImage = GetImageByPosition(position);

        if (targetImage == null)
        {
            Debug.LogWarning($"{position} のImageが設定されていません。");
            return;
        }

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
        targetImage.gameObject.SetActive(sprite != null);
    }

    /// <summary>
    /// 指定位置の立ち絵を非表示にする。
    /// </summary>
    public void Hide(StandingPosition position)
    {
        Image targetImage = GetImageByPosition(position);

        if (targetImage == null)
        {
            return;
        }

        targetImage.sprite = null;
        targetImage.enabled = false;
        targetImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 複数位置の立ち絵を非表示にする。
    /// </summary>
    public void HidePositions(List<StandingPosition> positions)
    {
        if (positions == null)
        {
            return;
        }

        foreach (StandingPosition position in positions)
        {
            Hide(position);
        }
    }

    /// <summary>
    /// 全立ち絵を非表示にする。
    /// </summary>
    public void HideAll()
    {
        Hide(StandingPosition.Left);
        Hide(StandingPosition.Center);
        Hide(StandingPosition.Right);
    }

    /// <summary>
    /// StandingPositionに対応するImageを返す。
    /// </summary>
    private Image GetImageByPosition(StandingPosition position)
    {
        switch (position)
        {
            case StandingPosition.Left:
                return leftImage;

            case StandingPosition.Center:
                return centerImage;

            case StandingPosition.Right:
                return rightImage;

            default:
                return null;
        }
    }
}