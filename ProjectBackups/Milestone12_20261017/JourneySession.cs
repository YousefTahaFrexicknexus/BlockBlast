using System;
using System.Collections;
using BlockBlastClone;
using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JourneySession : MonoBehaviour
{
    [Header("Authored Journey HUD")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Text levelTitle;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text movesText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Button mapButton;

    [Header("Result Panel Prefab")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultDescription;
    [SerializeField] private Text starsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Text nextButtonLabel;
    [SerializeField] private Button rewardedContinueButton;
    [SerializeField] private Text rewardedContinueLabel;

    private BlockBlastPrototype game;
    private JourneyLevelCatalog catalog;
    private JourneyLevelDefinition level;
    private int clearedLines;
    private bool completed;
    private int extraMovesGranted;
    private bool rewardedContinueUsed;
    private Coroutine pendingResult;
    private string playtestSessionId;
    public bool HasResumedAttempt { get; private set; }

    public int Seed => Level != null ? Level.randomSeed : 101;
    public JourneyLevelDefinition Level
    {
        get
        {
            if (level == null)
            {
                if (catalog == null) catalog = JourneyLevelCatalog.Load();
                if (catalog != null)
                {
                    level = catalog.Get(Mathf.Clamp(JourneyProgress.SelectedLevel, 1, catalog.Count));
                }
            }
            return level;
        }
    }

    // Called during the Classic model's Awake, before the Journey HUD starts.
    public bool TryRestoreAttempt(BlockBlastModel model)
    {
        HasResumedAttempt = false;
        if (JourneyPlaytestContext.IsPreview) return false;
        if (model == null || Level == null ||
            !JourneyAttemptStore.TryLoad(Level, out JourneyAttemptSnapshot saved))
        {
            return false;
        }

        if (!model.TryRestore(saved.board))
        {
            Debug.LogWarning("[Journey] Saved board failed validation; a new attempt will start.");
            JourneyAttemptStore.Clear(Level.levelNumber);
            return false;
        }

        clearedLines = saved.clearedLines;
        extraMovesGranted = saved.extraMovesGranted;
        rewardedContinueUsed = saved.rewardedContinueUsed;
        HasResumedAttempt = true;
        return true;
    }

    public void PersistAttempt()
    {
        if (!JourneyPlaytestContext.IsPreview && !completed && game != null && Level != null)
        {
            JourneyAttemptStore.Save(Level, game.CurrentModel, clearedLines,
                extraMovesGranted, rewardedContinueUsed);
        }
    }
    private void Awake()
    {
        catalog = JourneyLevelCatalog.Load();
        Bind(mapButton, ReturnToMap);
        Bind(retryButton, Retry);
        Bind(nextButton, Next);
        Bind(returnButton, ReturnToMap);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (rewardedContinueButton != null) rewardedContinueButton.interactable = false;
    }

    private void Start()
    {
        game = GetComponentInChildren<BlockBlastPrototype>(true);
        if (game == null || Level == null)
        {
            Debug.LogError("[Journey] The session is missing Classic gameplay or a level catalog.");
            enabled = false;
            return;
        }
        playtestSessionId = Guid.NewGuid().ToString("N");
        JourneyPlaytestTelemetry.Record(playtestSessionId, HasResumedAttempt ? "Resume" : "Start",
            Level, game.CurrentModel.Score, clearedLines, game.CurrentModel.Moves, 0);
        ConfigureJourneyPresentation();
        RefreshHUD();
    }

    private void ConfigureJourneyPresentation()
    {
        // Journey shares the Classic board but owns the upper HUD.
        Transform classicCanvas = game.transform.Find("Block Blast Canvas");
        if (classicCanvas != null)
        {
            Transform classicHeader = classicCanvas.Find("02 - Header and Score");
            if (classicHeader != null) classicHeader.gameObject.SetActive(false);
        }

        Transform hudRootTransform = transform.Find("Journey HUD Canvas");
        Canvas hud = hudRootTransform != null ? hudRootTransform.GetComponent<Canvas>() : null;
        if (hud != null)
        {
            hud.sortingOrder = 80;
        }
    }
    public void OnPlaced(PlacementResult placement, BlockBlastModel model)
    {
        if (completed || Level == null || model == null) return;
        clearedLines += placement.LinesCleared;
        RefreshHUD();

        // The win condition always wins ties, including no remaining board moves.
        bool won = Level.IsComplete(model.Score, clearedLines);
        bool lost = Level.IsFailed(model.Score, clearedLines, model.Moves, placement.GameOver, extraMovesGranted);
        if (!won && !lost)
        {
            PersistAttempt();
            return;
        }

        if (!JourneyPlaytestContext.IsPreview) JourneyAttemptStore.Clear(Level.levelNumber);

        completed = true;
        game.SetJourneyInputLocked(true);
        if (won)
        {
            int stars = Level.CalculateStars(model.Moves);
            if (!JourneyPlaytestContext.IsPreview)
                JourneyProgress.Complete(Level.levelNumber, stars, catalog.Count);
            JourneyPlaytestTelemetry.Record(playtestSessionId, "Win", Level,
                model.Score, clearedLines, model.Moves, stars);            pendingResult = StartCoroutine(ShowResult(true, stars, model.Score, false));
        }
        else
        {
            JourneyPlaytestTelemetry.Record(playtestSessionId, "Loss", Level,
                model.Score, clearedLines, model.Moves, 0);
            pendingResult = StartCoroutine(ShowResult(false, 0, model.Score, placement.GameOver));
        }
    }

    private IEnumerator ShowResult(bool won, int stars, int score, bool noLegalMoves)
    {
        // Let the placement/line-clear animation complete before the overlay.
        yield return new WaitForSecondsRealtime(.65f);
        resultTitle.text = won ? "LEVEL COMPLETE!" : (noLegalMoves ? "NO SPACE LEFT" : "OUT OF MOVES");
        resultDescription.text = won ? "SCORE " + score.ToString("N0") +
            "\n" + Level.ObjectiveText : "TRY AGAIN TO COMPLETE\n" + Level.ObjectiveText;
        starsText.text = won ? new string('★', stars) + new string('☆', 3 - stars) : "☆☆☆";
        bool hasNext = won && Level.levelNumber < catalog.Count;
        nextButton.gameObject.SetActive(won);
        nextButtonLabel.text = hasNext ? "NEXT LEVEL" : "LEVEL MAP";
        if (rewardedContinueButton != null)
        {
            rewardedContinueButton.gameObject.SetActive(!won && !noLegalMoves && !rewardedContinueUsed);
            rewardedContinueButton.interactable = false; // No ad SDK is connected yet.
        }
        if (rewardedContinueLabel != null)
            rewardedContinueLabel.text = "WATCH AD: +3 MOVES (SOON)";
        resultPanel.SetActive(true);
        pendingResult = null;
    }

    // Connect this to the rewarded-ad provider's verified completion callback
    // in the monetization milestone. The visible button remains disabled until then.
    public void OnVerifiedRewardedAdCompleted()
    {
        if (!completed || rewardedContinueUsed || game == null || game.CurrentModel == null ||
            game.CurrentModel.GameOver || resultPanel == null || !resultPanel.activeSelf)
            return;
        rewardedContinueUsed = true;
        extraMovesGranted += 3;
        completed = false;
        resultPanel.SetActive(false);
        game.SetJourneyInputLocked(false);
        RefreshHUD();
        PersistAttempt();
    }
    private void RefreshHUD()
    {
        if (Level == null) return;
        BlockBlastModel model = game != null ? game.CurrentModel : null;
        int score = model != null ? model.Score : 0;
        int moves = model != null ? model.Moves : 0;
        levelTitle.text = "LEVEL " + Level.levelNumber + "  •  " + Level.levelName;
        objectiveText.text = Level.ObjectiveText;
        progressText.text = Level.ProgressText(score, clearedLines);
        movesText.text = "MOVES " + Mathf.Max(0, Level.moveLimit + extraMovesGranted - moves);
        if (progressFill != null) progressFill.fillAmount = Level.Progress(score, clearedLines);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void SetHudVisible(bool visible)
    {
        if (hudRoot != null) hudRoot.SetActive(visible);
    }
    public void ReturnToMap()
    {
        if (!completed && game != null && Level != null && game.CurrentModel != null)
            JourneyPlaytestTelemetry.Record(playtestSessionId, "Abandon", Level,
                game.CurrentModel.Score, clearedLines, game.CurrentModel.Moves, 0);
        PersistAttempt();
        SceneManager.LoadScene("JourneyMap");
    }

    public void Retry()
    {
        if (game != null && Level != null && game.CurrentModel != null)
            JourneyPlaytestTelemetry.Record(playtestSessionId, "Retry", Level,
                game.CurrentModel.Score, clearedLines, game.CurrentModel.Moves, 0);
        if (Level != null && !JourneyPlaytestContext.IsPreview) JourneyAttemptStore.Clear(Level.levelNumber);
        SceneManager.LoadScene("JourneyGameplay");
    }

    public void Next()
    {
        int next = Level.levelNumber + 1;
#if UNITY_EDITOR
        if (JourneyPlaytestContext.IsPreview && next <= catalog.Count)
        {
            JourneyPlaytestContext.SetPreview(next);
            SceneManager.LoadScene("JourneyGameplay");
            return;
        }
#endif
        if (next <= catalog.Count && JourneyProgress.Select(next, catalog.Count))
        {
            SceneManager.LoadScene("JourneyGameplay");
        }
        else
        {
            ReturnToMap();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PersistAttempt();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) PersistAttempt();
    }
    private void OnDestroy()
    {
        if (pendingResult != null) StopCoroutine(pendingResult);
    }
}
