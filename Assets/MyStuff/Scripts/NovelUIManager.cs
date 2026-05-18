using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ノベルゲーム画面のUI表示を担当するクラス。
/// 背景画像、本文、話者名などを操作する。
/// </summary>
public class NovelUIManager : MonoBehaviour
{
    [Header("背景画像")]
    [SerializeField] private Image backgroundImage;

    [Header("本文表示用TextMeshPro")]
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("話者名表示用TextMeshPro。地の文では非表示にする")]
    [SerializeField] private TextMeshProUGUI speakerNameText;

    /// <summary>
    /// 背景画像を変更する。
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("backgroundImageが設定されていません。");
            return;
        }

        backgroundImage.sprite = sprite;
        backgroundImage.enabled = sprite != null;
    }

    /// <summary>
    /// 本文をすべて消す。
    /// ページが変わったときに呼ぶ。
    /// </summary>
    public void ClearBodyText()
    {
        if (bodyText != null)
        {
            bodyText.text = "";
        }
    }

    /// <summary>
    /// 本文を指定した文字列に更新する。
    /// </summary>
    public void SetBodyText(string text)
    {
        if (bodyText != null)
        {
            bodyText.text = text;
        }
    }

    /// <summary>
    /// 話者名を表示する。
    /// 空欄の場合は話者名欄を非表示にする。
    /// </summary>
    public void SetSpeakerName(string speakerName)
    {
        if (speakerNameText == null)
        {
            return;
        }

        bool hasSpeakerName = !string.IsNullOrWhiteSpace(speakerName);

        speakerNameText.gameObject.SetActive(hasSpeakerName);
        speakerNameText.text = hasSpeakerName ? speakerName : "";
    }
}