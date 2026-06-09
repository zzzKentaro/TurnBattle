using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 立ち絵を表示する位置。
/// 初期版では Left / Center / Right の3か所だけ扱う。
/// </summary>
public enum StandingPosition
{
    Left,
    Center,
    Right
}

/// <summary>
/// 立ち絵1枚分の表示設定。
/// ページ開始時の初期立ち絵にも、ブロックごとの立ち絵変更にも使う。
/// </summary>
[Serializable]
public class StandingCharacterData
{
    [Header("表示位置")]
    public StandingPosition position;

    [Header("表示する立ち絵画像")]
    public Sprite characterSprite;

    [Header("trueなら表示 / falseなら非表示")]
    public bool visible = true;
}

/// <summary>
/// 1ブロック分の文章データ。
/// プレイヤーがボタンを押すたびに、このブロック単位で文章が追加表示される。
/// </summary>
[Serializable]
public class NovelBlockData
{
    [Header("話者名。地の文の場合は空欄でよい")]
    public string speakerName;

    [Header("表示する本文")]
    [TextArea(3, 10)]
    public string bodyText;

    [Header("このブロック開始時に表示・差し替えする立ち絵")]
    public List<StandingCharacterData> standingChanges = new List<StandingCharacterData>();

    [Header("このブロック開始時に非表示にする立ち絵位置")]
    public List<StandingPosition> hidePositions = new List<StandingPosition>();

    [Header("trueなら、このブロック開始時にすべての立ち絵を消す")]
    public bool hideAllStandings;
}

/// <summary>
/// 1ページ分のデータ。
/// 背景画像やBGMが切り替わる大きな単位。
/// </summary>
[Serializable]
public class NovelPageData
{
    [Header("管理用ページ名。ゲーム中には表示しない")]
    public string pageName;

    [Header("このページへ移動するときにフェードする")]
    public bool useFadeOnEnter = true;

    [Header("このページの背景画像")]
    public Sprite backgroundSprite;

    [Header("このページで再生するBGM")]
    public AudioClip bgmClip;

    [Header("BGMが未設定の場合、現在のBGMを止める")]
    public bool stopBgmIfClipIsEmpty;

    [Header("このページ開始時の立ち絵状態")]
    public List<StandingCharacterData> startStandings = new List<StandingCharacterData>();

    [Header("このページに含まれる文章ブロック")]
    public List<NovelBlockData> blocks = new List<NovelBlockData>();
}

/// <summary>
/// ノベルゲーム全体のシナリオデータ。
/// Projectビューで右クリック → Create → Novel → Novel Scenario Data から作成できる。
/// </summary>
[CreateAssetMenu(fileName = "NewNovelScenarioData", menuName = "Novel/Novel Scenario Data")]
public class NovelScenarioData : ScriptableObject
{
    [Header("シナリオに含まれるページ一覧")]
    public List<NovelPageData> pages = new List<NovelPageData>();
}