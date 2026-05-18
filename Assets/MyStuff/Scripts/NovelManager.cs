using System.Collections;
using UnityEngine;

/// <summary>
/// ノベルゲーム全体の進行を管理する中心クラス。
/// 
/// 担当すること:
/// ・現在のページ番号を管理する
/// ・現在のブロック番号を管理する
/// ・入力されたら次のブロックへ進める
/// ・ページが終わったら次のページへ進める
/// ・背景、BGM、立ち絵、本文表示を各Managerに指示する
/// </summary>
public class NovelManager : MonoBehaviour
{
    [Header("再生するシナリオデータ")]
    [SerializeField] private NovelScenarioData scenarioData;

    [Header("UI管理")]
    [SerializeField] private NovelUIManager uiManager;

    [Header("立ち絵管理")]
    [SerializeField] private CharacterStandingManager standingManager;

    [Header("音楽管理")]
    [SerializeField] private NovelAudioManager audioManager;

    [Header("フェード管理")]
    [SerializeField] private FadeController fadeController;

    [Header("入力管理")]
    [SerializeField] private NovelInputHandler inputHandler;

    [Header("1文字を表示する間隔")]
    [SerializeField] private float characterDisplayInterval = 0.03f;

    private int currentPageIndex;
    private int currentBlockIndex;

    private NovelPageData currentPage;

    // 現在のページ内で、すでに表示済みの本文。
    // ページが変わるまで消さず、ブロックごとに追加していく。
    private string currentPageDisplayedText = "";

    // タイプライター表示中かどうか。
    private bool isTyping;

    // ページ切り替えフェード中かどうか。
    private bool isChangingPage;

    // シナリオが最後まで終わったかどうか。
    private bool isScenarioFinished;

    private Coroutine typingCoroutine;

    // タイプライター中に最終的に表示する全文。
    // 表示途中にボタンが押された場合、この文字列を一気に表示する。
    private string currentTypingTargetText = "";

    private void Start()
    {
        if (inputHandler != null)
        {
            inputHandler.OnNextPressed += HandleNextInput;
            inputHandler.OnBackToStartPressed += HandleBackToStartInput;
        }

        StartScenario();
    }

    private void OnDestroy()
    {
        if (inputHandler != null)
        {
            inputHandler.OnNextPressed -= HandleNextInput;
            inputHandler.OnBackToStartPressed -= HandleBackToStartInput;
        }
    }

    /// <summary>
    /// シナリオを最初から開始する。
    /// </summary>
    public void StartScenario()
    {
        if (scenarioData == null || scenarioData.pages == null || scenarioData.pages.Count == 0)
        {
            Debug.LogError("scenarioDataが未設定、またはページが空です。");
            return;
        }

        StopCurrentTyping();

        currentPageIndex = 0;
        currentBlockIndex = -1;
        isScenarioFinished = false;

        StartCoroutine(LoadPage(currentPageIndex, useFade: false));
    }

    /// <summary>
    /// 「次へ」入力を受け取ったときの処理。
    /// </summary>
    private void HandleNextInput()
    {
        if (isChangingPage)
        {
            return;
        }

        if (isScenarioFinished)
        {
            return;
        }

        // 文章が1文字ずつ表示されている途中なら、
        // 次のブロックへ進まず、現在のブロックを一気に最後まで表示する。
        if (isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        AdvanceBlockOrPage();
    }

    /// <summary>
    /// Escが押されたときの処理。
    /// 初期版ではタイトル画面がないため、シナリオ先頭へ戻す。
    /// </summary>
    private void HandleBackToStartInput()
    {
        if (scenarioData == null || scenarioData.pages == null || scenarioData.pages.Count == 0)
        {
            return;
        }

        StopCurrentTyping();

        currentPageIndex = 0;
        currentBlockIndex = -1;
        isScenarioFinished = false;

        NovelPageData firstPage = scenarioData.pages[currentPageIndex];

        bool useFade = firstPage.useFadeOnEnter;

        StartCoroutine(LoadPage(currentPageIndex, useFade));
    }

    /// <summary>
    /// 次のブロックへ進む。
    /// ページ内のブロックが終わっていたら、次のページへ進む。
    /// </summary>
    private void AdvanceBlockOrPage()
    {
        if (currentPage == null)
        {
            return;
        }

        currentBlockIndex++;

        // まだページ内に表示すべきブロックがある場合
        if (currentBlockIndex < currentPage.blocks.Count)
        {
            ShowBlock(currentPage.blocks[currentBlockIndex]);
            return;
        }

        // ページ内の最後のブロックまで表示済みで、さらに入力された場合
        MoveToNextPage();
    }

    /// <summary>
    /// 次のページへ進む。
    /// 最後のページだった場合はシナリオ終了。
    /// </summary>
    private void MoveToNextPage()
    {
        int nextPageIndex = currentPageIndex + 1;

        if (nextPageIndex >= scenarioData.pages.Count)
        {
            FinishScenario();
            return;
        }

        currentPageIndex = nextPageIndex;
        currentBlockIndex = -1;

        NovelPageData nextPage = scenarioData.pages[currentPageIndex];

        bool useFade = nextPage.useFadeOnEnter;

        StartCoroutine(LoadPage(currentPageIndex, useFade));
    }

    /// <summary>
    /// 指定ページを読み込む。
    /// 背景、BGM、立ち絵、本文欄を更新する。
    /// </summary>
    private IEnumerator LoadPage(int pageIndex, bool useFade)
    {
        if (pageIndex < 0 || pageIndex >= scenarioData.pages.Count)
        {
            yield break;
        }

        isChangingPage = true;
        StopCurrentTyping();

        if (useFade && fadeController != null)
        {
            yield return fadeController.FadeOut();
        }

        currentPage = scenarioData.pages[pageIndex];

        // ページが変わるので、ページ内の表示済み文章をリセットする。
        currentPageDisplayedText = "";

        if (uiManager != null)
        {
            uiManager.ClearBodyText();
            uiManager.SetSpeakerName("");
            uiManager.SetBackground(currentPage.backgroundSprite);
        }

        if (standingManager != null)
        {
            standingManager.ApplyPageStartStandings(currentPage.startStandings);
        }

        if (audioManager != null)
        {
            if (currentPage.bgmClip != null)
            {
                audioManager.PlayBgm(currentPage.bgmClip);
            }
            else if (currentPage.stopBgmIfClipIsEmpty)
            {
                audioManager.StopBgm();
            }
        }

        if (useFade && fadeController != null)
        {
            yield return fadeController.FadeIn();
        }

        isChangingPage = false;

        // ページに入ったら、最初のブロックを自動で表示開始する。
        // これにより「背景だけ出て、最初の入力待ち」にはならない。
        AdvanceBlockOrPage();
    }

    /// <summary>
    /// 1ブロック分の表示処理。
    /// 立ち絵変更を反映してから、本文をタイプライター表示する。
    /// </summary>
    private void ShowBlock(NovelBlockData block)
    {
        if (block == null)
        {
            return;
        }

        if (standingManager != null)
        {
            if (block.hideAllStandings)
            {
                standingManager.HideAll();
            }

            standingManager.HidePositions(block.hidePositions);
            standingManager.ApplyStandingChanges(block.standingChanges);
        }

        if (uiManager != null)
        {
            uiManager.SetSpeakerName(block.speakerName);
        }

        typingCoroutine = StartCoroutine(TypeBlockText(block.bodyText));
    }

    /// <summary>
    /// 1ブロック分の本文を、1文字ずつ表示する。
    /// 同じページ内では前の文章を消さず、下に追加していく。
    /// </summary>
    private IEnumerator TypeBlockText(string blockText)
    {
        isTyping = true;

        if (blockText == null)
        {
            blockText = "";
        }

        string separator = "";

        // すでに同じページ内に文章が表示されている場合は、
        // ブロックとブロックの間に空行を入れる。
        if (!string.IsNullOrEmpty(currentPageDisplayedText))
        {
            separator = "\n\n";
        }

        string baseText = currentPageDisplayedText;
        currentTypingTargetText = baseText + separator + blockText;

        int startLength = baseText.Length;
        int targetLength = currentTypingTargetText.Length;

        for (int i = startLength; i <= targetLength; i++)
        {
            string visibleText = currentTypingTargetText.Substring(0, i);

            if (uiManager != null)
            {
                uiManager.SetBodyText(visibleText);
            }

            yield return new WaitForSeconds(characterDisplayInterval);
        }

        currentPageDisplayedText = currentTypingTargetText;
        isTyping = false;
        typingCoroutine = null;
    }

    /// <summary>
    /// タイプライター表示中の文章を、一気に最後まで表示する。
    /// </summary>
    private void CompleteTypingImmediately()
    {
        if (!isTyping)
        {
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        currentPageDisplayedText = currentTypingTargetText;

        if (uiManager != null)
        {
            uiManager.SetBodyText(currentPageDisplayedText);
        }

        isTyping = false;
    }

    /// <summary>
    /// 現在のタイプライター処理を止める。
    /// ページ移動や最初に戻る処理で使う。
    /// </summary>
    private void StopCurrentTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        currentTypingTargetText = "";
    }

    /// <summary>
    /// シナリオ終了時の処理。
    /// 初期版ではログを出すだけ。
    /// </summary>
    private void FinishScenario()
    {
        isScenarioFinished = true;
        Debug.Log("シナリオが最後まで終了しました。");
    }
}