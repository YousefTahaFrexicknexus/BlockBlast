using System.Collections;
using System.Collections.Generic;
using BlockBlastClone;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed partial class BlockBlastPrototype : MonoBehaviour
{
    private const float BoardSize = 916f;
    private const float BoardPadding = 12f;
    private const float Gap = 6f;
    private const float CellSize = (BoardSize - BoardPadding * 2f - Gap * 7f) / 8f;
    private const float Step = CellSize + Gap;
    [Header("Classic Feel - Drag & Placement")]
    [SerializeField, Range(60f, 300f)] private float dragLift = 185f;
    [SerializeField, Range(8f, 70f)] private float dragFollowSpeed = 30f;
    [SerializeField, Range(0.75f, 1.5f)] private float dragStartScale = 0.94f;
    [SerializeField, Range(0.75f, 1.5f)] private float dragValidScale = 1.02f;
    [SerializeField, Range(0.75f, 1.5f)] private float dragInvalidScale = 0.97f;
    [SerializeField, Range(0f, 20f)] private float previewPulseSpeed = 9f;
    [SerializeField, Range(1f, 1.2f)] private float invalidNudgeScale = 1.035f;
    [SerializeField, Range(0.05f, 0.5f)] private float invalidNudgeDuration = 0.11f;

    [Header("Classic Feel - Scoring Feedback")]
    [SerializeField, Range(0f, 140f)] private float floatingScoreLift = 55f;
    [SerializeField, Range(30f, 200f)] private float floatingScoreRise = 112f;
    [SerializeField, Range(0.3f, 1.6f)] private float floatingScoreDuration = 0.8f;
    private const string BestKey = "BlockBlastPrototypeBest";
    private const string SoundKey = "BlockBlastPrototypeSound";
    private const string HapticKey = "BlockBlastPrototypeHaptics";

    private static readonly Color Back = Hex("#10183B");
    private static readonly Color BoardBack = Hex("#19285B");
    private static readonly Color Empty = Hex("#2C3E78");
    private static readonly Color TextWhite = Hex("#FFFFFF");
    private static readonly Color TextDim = Hex("#A8BCE8");
    private static readonly Color Gold = Hex("#FFDB76");
    private static readonly Color PreviewGood = Hex("#A2FFD5");
    private static readonly Color PreviewBad = Hex("#FF6E8D");
    private static readonly Color[] Colors =
    {
        new Color(0, 0, 0, 0),
        Hex("#40C8FF"), Hex("#FEC849"), Hex("#F89145"), Hex("#C979F7"),
        Hex("#66D889"), Hex("#FF7097"), Hex("#4A83FF")
    };

    private BlockBlastModel game;
    private JourneySession journeySession;
    public BlockBlastModel CurrentModel => game;

    public void SetJourneyInputLocked(bool locked)
    {
        if (journeySession == null) return;
        ending = locked;
        if (locked) CancelActiveDrag();
    }
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform boardRect;
    private RectTransform ghost;
    private RectTransform[] trays = new RectTransform[3];
    private RectTransform[] trayPieceLayers = new RectTransform[3];
    private BlockPiece[] renderedPieces = new BlockPiece[3];
    private Image[,] cells = new Image[8, 8];
    private Image[,] preview = new Image[8, 8];
    private Text scoreText;
    private Text bestText;
    private Text comboText;
    private Text feedbackText;
    private Text muteText;
    private Text hapticText;
    private GameObject gameOverPanel;
    private CanvasGroup gameOverAlpha;
    private Text finalText;
    private Font uiFont;
    private Sprite roundedSprite;
    private bool ownsRoundedSprite;
    private AudioSource audioSource;
    private AudioClip pickupClip;
    private AudioClip placeClip;
    private AudioClip clearClip;
    private AudioClip invalidClip;
    private bool soundEnabled;
    private bool hapticsEnabled;
    private int best;
    private int displayedScore;
    private int selectedSlot = -1;
    private int draggingSlot = -1;
    private int activeDragPointerId = int.MinValue;
    private int hoverRow = -1;
    private int hoverCol = -1;
    private bool hoverValid;
    private bool hoverNear;
    private Vector2 ghostTarget;
    private readonly List<Image> visibleDragPreviews = new List<Image>(64);
    private readonly List<Color> visibleDragPreviewColors = new List<Color>(64);
    private Coroutine feedbackRoutine;
    private Coroutine scoreRoutine;
    private Coroutine overRoutine;
    private Coroutine boardImpactRoutine;
    private Coroutine comboRoutine;
    private Coroutine invalidNudgeRoutine;
    private Coroutine[] trayTweens = new Coroutine[3];
    private readonly HashSet<GameObject> activeEffects = new HashSet<GameObject>();
    private Vector2 boardRestPosition;
    private bool ending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnSceneLoad()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene != "ClassicGameplay" && scene != "SampleScene")
        {
            return;
        }
        if (FindFirstObjectByType<BlockBlastPrototype>() == null)
        {
            new GameObject("Block Blast - Classic Prototype").AddComponent<BlockBlastPrototype>();
        }
    }

    private static Color Hex(string code)
    {
        ColorUtility.TryParseHtmlString(code, out Color color);
        return color;
    }

    private void Awake()
    {
        journeySession = GetComponentInParent<JourneySession>();
        game = journeySession != null ? new BlockBlastModel(journeySession.Seed) : new BlockBlastModel();
        if (journeySession != null) journeySession.TryRestoreAttempt(game);
        bool resumed = journeySession == null && RestoreSession();
        displayedScore = game.Score;
        best = PlayerPrefs.GetInt(BestKey, 0);
        soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
        hapticsEnabled = PlayerPrefs.GetInt(HapticKey, 0) != 0;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        roundedSprite = Resources.Load<Sprite>("MagicalWorlds/UI/RoundedPanel");
        ownsRoundedSprite = roundedSprite == null;
        if (ownsRoundedSprite)
        {
            roundedSprite = CreateRoundedSprite();
        }
        SetupAudio();
        if (transform.Find("Block Blast Canvas") != null)
        {
            BindAuthoredUI();
        }
        else
        {
            BuildUI();
        }
        RefreshUI();
        scoreText.text = displayedScore.ToString("N0");
        if (journeySession == null) ShowInitialFlow(resumed);
        else Notify(journeySession.HasResumedAttempt ? "JOURNEY ATTEMPT RESUMED!" : "COMPLETE THE LEVEL OBJECTIVE!");
    }

    private void OnDestroy()
    {
        if (ownsRoundedSprite && roundedSprite != null)
        {
            Texture2D texture = roundedSprite.texture;
            Destroy(roundedSprite);
            Destroy(texture);
        }
        PersistSession();
        if (pickupClip != null) Destroy(pickupClip);
        if (placeClip != null) Destroy(placeClip);
        if (clearClip != null) Destroy(clearClip);
        if (invalidClip != null) Destroy(invalidClip);
    }

    private void Update()
    {
        UpdateFlowKeys();
        if (ghost == null)
        {
            return;
        }
        float follow = 1f - Mathf.Exp(-dragFollowSpeed * Time.unscaledDeltaTime);
        ghost.anchoredPosition = Vector2.Lerp(ghost.anchoredPosition, ghostTarget, follow);
        float pulse = previewPulseSpeed <= 0f ? 0f : Mathf.Sin(Time.unscaledTime * previewPulseSpeed) * .014f;
        float targetScale = !hoverNear ? dragStartScale : (hoverValid ? dragValidScale : dragInvalidScale);
        ghost.localScale = Vector3.one * Mathf.Lerp(ghost.localScale.x, targetScale + pulse, follow);

        if (previewPulseSpeed > 0f)
        {
            float alpha = .88f + .12f * Mathf.Sin(Time.unscaledTime * previewPulseSpeed);
            for (int i = 0; i < visibleDragPreviews.Count; i++)
            {
                Image image = visibleDragPreviews[i];
                if (image == null)
                {
                    continue;
                }
                Color color = visibleDragPreviewColors[i];
                color.a *= alpha;
                image.color = color;
            }
        }
    }
    private static Sprite CreateRoundedSprite()
    {
        const int dimension = 64;
        Texture2D texture = new Texture2D(dimension, dimension, TextureFormat.RGBA32, false);
        texture.name = "Runtime Rounded Rect";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[dimension * dimension];
        const float radius = 14f;
        for (int y = 0; y < dimension; y++)
        {
            for (int x = 0; x < dimension; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - (dimension * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - (dimension * 0.5f - radius), 0f);
                float coverage = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.6f);
                pixels[y * dimension + x] = new Color(1f, 1f, 1f, coverage);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, dimension, dimension), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * 14f);
    }

    private RectTransform MakeRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return rect;
    }

    private RectTransform Panel(string name, Transform parent, Vector2 pos, Vector2 size, Color color, bool raycast = false, bool rounded = false)
    {
        RectTransform rect = MakeRect(name, parent, pos, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        if (rounded)
        {
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
        }
        return rect;
    }

    private Text Label(string name, Transform parent, Vector2 pos, Vector2 size, string value, int fontSize, Color color, FontStyle style = FontStyle.Normal)
    {
        RectTransform rect = MakeRect(name, parent, pos, size);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.text = value;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = fontSize;
        return label;
    }

    private Button MakeButton(string name, Transform parent, Vector2 pos, Vector2 size, string value, Color color, int fontSize = 42)
    {
        RectTransform rect = Panel(name, parent, pos, size, color, true, true);
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock settings = button.colors;
        settings.normalColor = Color.white;
        settings.highlightedColor = Hex("#DBEEFF");
        settings.pressedColor = Hex("#A0BEF1");
        settings.selectedColor = Color.white;
        button.colors = settings;
        Label(value, rect, Vector2.zero, size, value, fontSize, TextWhite, FontStyle.Bold);
        return button;
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("Block Blast Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localScale = Vector3.one;
        canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasObj.GetComponent<RectTransform>();

        RectTransform backdrop = Panel("Background", canvasRect, Vector2.zero, new Vector2(1080, 1920), Back);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = Vector2.zero;
        backdrop.offsetMax = Vector2.zero;
        ApplyPainterlyBackground(backdrop);

        Panel("Top Glow", canvasRect, new Vector2(0, 945), new Vector2(950, 300), Hex("#202F66"), false, true);
        Label("Title", canvasRect, new Vector2(-5, 816), new Vector2(820, 108), "BLOCK PUZZLE", 77, TextWhite, FontStyle.Bold);
        Label("Subtitle", canvasRect, new Vector2(0, 740), new Vector2(880, 48), "CLASSIC MODE", 30, Hex("#68C9FF"), FontStyle.Bold);
        Button mute = MakeButton("Sound Toggle", canvasRect, new Vector2(446, 842), new Vector2(100, 82), "", Hex("#314985"), 22);
        muteText = mute.GetComponentInChildren<Text>();
        mute.onClick.AddListener(ToggleSound);
        RefreshSoundText();
        Button haptic = MakeButton("Haptics Toggle", canvasRect, new Vector2(-446, 842), new Vector2(105, 82), "", Hex("#314985"), 19);
        hapticText = haptic.GetComponentInChildren<Text>();
        haptic.onClick.AddListener(ToggleHaptics);
        RefreshHapticsText();

        Label("Score Caption", canvasRect, new Vector2(-238, 653), new Vector2(345, 48), "SCORE", 29, TextDim);
        scoreText = Label("Score", canvasRect, new Vector2(-238, 590), new Vector2(400, 85), "0", 78, TextWhite, FontStyle.Bold);
        Label("Best Caption", canvasRect, new Vector2(236, 653), new Vector2(345, 48), "BEST", 29, TextDim);
        bestText = Label("Best", canvasRect, new Vector2(236, 590), new Vector2(400, 85), "0", 78, Gold, FontStyle.Bold);

        Panel("Board Shadow", canvasRect, new Vector2(0, 25), new Vector2(936, 936), Hex("#091033"), false, true);
        boardRestPosition = new Vector2(0, 38);
        boardRect = Panel("8 x 8 Grid", canvasRect, boardRestPosition, new Vector2(BoardSize, BoardSize), BoardBack, true, true);
        boardRect.gameObject.AddComponent<BlockBoardPointer>().Initialize(this);
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Vector2 location = CellPosition(r, c);
                RectTransform block = Panel("Cell " + r + ", " + c, boardRect, location, new Vector2(CellSize, CellSize), Empty, false, true);
                cells[r, c] = block.GetComponent<Image>();
                RectTransform glow = Panel("Preview " + r + ", " + c, boardRect, location, new Vector2(CellSize, CellSize), Color.clear, false, true);
                preview[r, c] = glow.GetComponent<Image>();
                glow.gameObject.SetActive(false);
            }
        }

        comboText = Label("Combo", canvasRect, new Vector2(-40, -462), new Vector2(720, 58), "", 36, Gold, FontStyle.Bold);
        MakeButton("Pause", canvasRect, new Vector2(425, -464), new Vector2(173, 76), "PAUSE", Hex("#314985"), 30).onClick.AddListener(OpenPause);
        for (int i = 0; i < 3; i++)
        {
            trays[i] = Panel("Piece Tray " + (i + 1), canvasRect, new Vector2((i - 1) * 338f, -645), new Vector2(318, 231), Hex("#22336D"), true, true);
            trays[i].gameObject.AddComponent<BlockTrayPointer>().Initialize(this, i);
            trayPieceLayers[i] = MakeRect("Piece Graphics", trays[i], Vector2.zero, new Vector2(310, 225));
        }
        feedbackText = Label("Instruction", canvasRect, new Vector2(0, -839), new Vector2(1000, 62), "DRAG A PIECE TO PLACE IT", 29, TextDim);

        gameOverPanel = Panel("Game Over Overlay", canvasRect, Vector2.zero, new Vector2(1080, 1920), new Color(0.035f, 0.065f, 0.18f, 0.96f), true).gameObject;
        RectTransform overlay = gameOverPanel.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        gameOverAlpha = gameOverPanel.AddComponent<CanvasGroup>();
        Label("Over Title", overlay, new Vector2(0, 245), new Vector2(950, 130), "NO MORE MOVES!", 72, TextWhite, FontStyle.Bold);
        Label("Final Score Label", overlay, new Vector2(0, 121), new Vector2(950, 60), "YOUR SCORE", 30, TextDim, FontStyle.Bold);
        finalText = Label("Final Score", overlay, new Vector2(0, 26), new Vector2(950, 135), "0", 93, Gold, FontStyle.Bold);
        MakeButton("Restart", overlay, new Vector2(0, -185), new Vector2(620, 132), "PLAY AGAIN", Hex("#3276F5")).onClick.AddListener(Restart);
        MakeButton("Game Over - Main Menu", overlay, new Vector2(0, -350), new Vector2(620, 112), "MAIN MENU", Hex("#5C4BC6")).onClick.AddListener(ReturnToMainMenu);
        gameOverPanel.SetActive(false);
        BuildFlowUI();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("Block Puzzle EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private Vector2 CellPosition(int row, int col)
    {
        return new Vector2(-BoardSize * .5f + BoardPadding + CellSize * .5f + col * Step,
            BoardSize * .5f - BoardPadding - CellSize * .5f - row * Step);
    }

    private void RefreshUI()
    {
        DrawBoard();
        for (int i = 0; i < 3; i++)
        {
            DrawTray(i);
        }
        if (journeySession == null && game.Score > best)
        {
            best = game.Score;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }
        bestText.text = best.ToString("N0");
        comboText.text = game.Combo >= 2 ? "COMBO  x" + game.Combo : "";
    }

    private void DrawBoard()
    {
        Color emptyCellColor = MagicalWorlds.Journey.JourneyBoardPalette.EmptyCell;
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                cells[r, c].color = game.Board[r, c] == 0 ?
                    emptyCellColor : Colors[game.Board[r, c]];
                preview[r, c].gameObject.SetActive(false);
            }
        }
    }

    private void DrawTray(int slot)
    {
        RectTransform tray = trays[slot];
        tray.GetComponent<Image>().color = slot == selectedSlot ?
            MagicalWorlds.Journey.JourneyBoardPalette.SelectedTray :
            MagicalWorlds.Journey.JourneyBoardPalette.Tray;
        tray.localScale = Vector3.one;
        if (game.Hand[slot] == renderedPieces[slot])
        {
            return;
        }
        renderedPieces[slot] = game.Hand[slot];
        RectTransform layer = trayPieceLayers[slot];
        for (int i = layer.childCount - 1; i >= 0; i--)
        {
            Destroy(layer.GetChild(i).gameObject);
        }
        BlockPiece piece = game.Hand[slot];
        if (piece == null)
        {
            return;
        }
        const float size = 48f;
        const float stride = 53f;
        foreach (Vector2Int cell in piece.Cells)
        {
            float x = (cell.x - (piece.Width - 1) * .5f) * stride;
            float y = ((piece.Height - 1) * .5f - cell.y) * stride;
            Panel("Piece Square", layer, new Vector2(x, y), new Vector2(size, size), Colors[piece.Color], false, true);
        }
    }

    public void SelectPiece(int slot)
    {
        if (game.GameOver || ending || isPaused || draggingSlot >= 0 || game.Hand[slot] == null)
        {
            return;
        }
        selectedSlot = slot == selectedSlot ? -1 : slot;
        for (int i = 0; i < 3; i++)
        {
            DrawTray(i);
        }
        if (selectedSlot >= 0)
        {
            PlayClip(pickupClip, 0.45f);
            Notify("TAP THE GRID TO PLACE THE PIECE");
        }
        else
        {
            Notify("DRAG A PIECE TO PLACE IT");
        }
    }

    public void TapBoard(PointerEventData e)
    {
        if (selectedSlot < 0 || game.GameOver || ending || isPaused)
        {
            return;
        }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, e.position, e.pressEventCamera, out Vector2 local))
        {
            return;
        }
        int col = Mathf.RoundToInt((local.x - CellPosition(0, 0).x) / Step);
        int row = Mathf.RoundToInt((CellPosition(0, 0).y - local.y) / Step);
        Place(selectedSlot, row, col);
    }

    public void StartPieceDrag(int slot, PointerEventData e)
    {
        if (game.GameOver || ending || isPaused || draggingSlot >= 0 || game.Hand[slot] == null)
        {
            return;
        }
        ClearDragPreviews(true);
        draggingSlot = slot;
        activeDragPointerId = e.pointerId;
        selectedSlot = -1;
        for (int i = 0; i < trays.Length; i++)
        {
            trays[i].GetComponent<Image>().color = MagicalWorlds.Journey.JourneyBoardPalette.Tray;
        }
        PlayClip(pickupClip, 0.5f);
        BlockPiece piece = game.Hand[slot];
        ghost = MakeRect("Dragged Piece", canvasRect, Vector2.zero, new Vector2(piece.Width * Step, piece.Height * Step));
        ghost.SetAsLastSibling();
        foreach (Vector2Int cell in piece.Cells)
        {
            float x = (cell.x - (piece.Width - 1) * .5f) * Step;
            float y = ((piece.Height - 1) * .5f - cell.y) * Step;
            Panel("Dragged Square", ghost, new Vector2(x, y), new Vector2(CellSize, CellSize), Colors[piece.Color], false, true);
        }
        ghost.localScale = Vector3.one * dragStartScale;
        ContinuePieceDrag(e, true);
    }

    public void ContinuePieceDrag(PointerEventData e)
    {
        ContinuePieceDrag(e, false);
    }

    private void ContinuePieceDrag(PointerEventData e, bool force)
    {
        if (draggingSlot < 0 || ghost == null || e == null || e.pointerId != activeDragPointerId)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, e.pressEventCamera, out Vector2 point))
        {
            return;
        }

        ghostTarget = point + Vector2.up * dragLift;
        if (force)
        {
            ghost.anchoredPosition = ghostTarget;
        }

        // Convert the lifted ghost's Canvas-space center into the board's
        // local space. This stays correct if designers move, nest or scale
        // the Board prefab without requiring new hit-test offsets.
        Vector2 boardSpaceCenter = boardRect.InverseTransformPoint(canvasRect.TransformPoint(ghostTarget));
        BlockPiece piece = game.Hand[draggingSlot];
        Vector2Int origin = BlockBlastDragMath.PieceOrigin(boardSpaceCenter,
            CellPosition(0, 0), piece.Width, piece.Height, Step);
        bool near = BlockBlastDragMath.NearBoard(boardSpaceCenter, BoardSize, Step);
        bool valid = near && game.CanPlace(piece, origin.y, origin.x);

        // Avoid rebuilding all 64 cells every time a finger moves inside
        // one snapped grid cell. Only changed positions update the preview.
        bool changed = hoverRow != origin.y || hoverCol != origin.x ||
            hoverNear != near || hoverValid != valid;
        hoverCol = origin.x;
        hoverRow = origin.y;
        hoverNear = near;
        hoverValid = valid;

        if (!changed)
        {
            return;
        }

        ClearDragPreviews();
        ShowPlacementPreview(piece);
        foreach (Transform child in ghost)
        {
            Image image = child.GetComponent<Image>();
            Color color = hoverNear && !hoverValid ? PreviewBad : Colors[piece.Color];
            color.a = hoverNear && !hoverValid ? .70f : .91f;
            image.color = color;
        }
    }

    private void ClearDragPreviews(bool clearUntracked = false)
    {
        foreach (Image image in visibleDragPreviews)
        {
            if (image != null)
            {
                image.gameObject.SetActive(false);
            }
        }
        visibleDragPreviews.Clear();
        visibleDragPreviewColors.Clear();

        // Also recover orphaned highlights from an interrupted drag or older
        // scenes where previews were enabled without being registered.
        // This full sweep runs only on drag start/end/cancellation.
        if (clearUntracked)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (preview[row, col] != null)
                    {
                        preview[row, col].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetDragPreview(Image image, Color color)
    {
        int index = visibleDragPreviews.IndexOf(image);
        if (index < 0)
        {
            visibleDragPreviews.Add(image);
            visibleDragPreviewColors.Add(color);
        }
        else
        {
            visibleDragPreviewColors[index] = color;
        }
        image.gameObject.SetActive(true);
        image.color = color;
    }
    private void ShowPlacementPreview(BlockPiece piece)
    {
        if (!hoverNear)
        {
            return;
        }
        foreach (Vector2Int offset in piece.Cells)
        {
            int r = hoverRow + offset.y;
            int c = hoverCol + offset.x;
            if (r >= 0 && r < 8 && c >= 0 && c < 8)
            {
                SetDragPreview(preview[r, c], hoverValid
                    ? new Color(0.52f, 1f, 0.83f, .67f)
                    : new Color(1f, .35f, .47f, .79f));
            }
        }

        if (!hoverValid)
        {
            return;
        }

        // Preview the exact rows/columns this placement will clear.
        for (int r = 0; r < 8; r++)
        {
            bool full = true;
            for (int c = 0; c < 8; c++)
            {
                if (game.Board[r, c] == 0 && !IsOccupiedByPreview(piece, r, c))
                {
                    full = false;
                    break;
                }
            }
            if (full)
            {
                for (int c = 0; c < 8; c++)
                {
                    ShowLineHint(r, c);
                }
            }
        }
        for (int c = 0; c < 8; c++)
        {
            bool full = true;
            for (int r = 0; r < 8; r++)
            {
                if (game.Board[r, c] == 0 && !IsOccupiedByPreview(piece, r, c))
                {
                    full = false;
                    break;
                }
            }
            if (full)
            {
                for (int r = 0; r < 8; r++)
                {
                    ShowLineHint(r, c);
                }
            }
        }
    }

    private bool IsOccupiedByPreview(BlockPiece piece, int row, int col)
    {
        foreach (Vector2Int cell in piece.Cells)
        {
            if (row == hoverRow + cell.y && col == hoverCol + cell.x)
            {
                return true;
            }
        }
        return false;
    }

    private void ShowLineHint(int row, int col)
    {
        SetDragPreview(preview[row, col], new Color(1f, .84f, .35f, .55f));
    }

    public void FinishPieceDrag(PointerEventData e)
    {
        if (draggingSlot < 0 || e == null || e.pointerId != activeDragPointerId)
        {
            return;
        }

        ContinuePieceDrag(e);
        int slot = draggingSlot;
        bool allowed = hoverValid;
        bool wasNear = hoverNear;
        int row = hoverRow;
        int col = hoverCol;
        draggingSlot = -1;
        activeDragPointerId = int.MinValue;
        hoverRow = -1;
        hoverCol = -1;
        hoverNear = false;
        hoverValid = false;

        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }

        ClearDragPreviews(true);
        if (allowed)
        {
            Place(slot, row, col);
        }
        else
        {
            InvalidPlacement(!wasNear);
        }
    }
    private void InvalidPlacement(bool outsideBoard = false)
    {
        PlayClip(invalidClip, .27f);
        Notify(outsideBoard ? "MOVE THE PIECE ONTO THE BOARD" : "THE WHOLE PIECE MUST FIT IN EMPTY CELLS");
        if (invalidNudgeRoutine != null)
        {
            StopCoroutine(invalidNudgeRoutine);
        }
        invalidNudgeRoutine = StartCoroutine(NudgeTrays());
    }

    private IEnumerator NudgeTrays()
    {
        for (int i = 0; i < 3; i++)
        {
            if (trays[i] != null)
            {
                trays[i].localScale = Vector3.one * invalidNudgeScale;
            }
        }
        yield return new WaitForSecondsRealtime(invalidNudgeDuration);
        for (int i = 0; i < 3; i++)
        {
            if (trays[i] != null)
            {
                trays[i].localScale = Vector3.one;
            }
        }
        invalidNudgeRoutine = null;
    }
    private void Place(int slot, int row, int col)
    {
        BlockPiece piece = slot >= 0 && slot < game.Hand.Length ? game.Hand[slot] : null;
        if (piece == null || !game.TryPlace(slot, row, col, out PlacementResult result))
        {
            InvalidPlacement();
            return;
        }

        List<Vector2Int> placed = new List<Vector2Int>();
        foreach (Vector2Int cell in piece.Cells)
        {
            placed.Add(new Vector2Int(col + cell.x, row + cell.y));
        }

        selectedSlot = -1;
        PersistSession();
        RefreshUI();
        AnimateScore(game.Score);
        PlayClip(result.LinesCleared > 0 ? clearClip : placeClip, result.LinesCleared > 0 ? .76f : .5f);
        StartCoroutine(PlacementPop(placed, piece.Color));
        StartCoroutine(FloatingPoints(result.ScoreAdded, result.LinesCleared > 0, GetPlacementFeedbackAnchor(placed)));
        if (result.LinesCleared > 0)
        {
            StartCoroutine(FlashCleared(result.ClearedCells));
            if (boardImpactRoutine != null)
            {
                StopCoroutine(boardImpactRoutine);
                boardRect.anchoredPosition = boardRestPosition;
            }
            boardImpactRoutine = StartCoroutine(BoardImpact(Mathf.Min(13, 4 + result.LinesCleared * 3)));
            AnimateCombo();
            Notify(result.LinesCleared > 1 ? "AMAZING! " + result.LinesCleared + " LINES!" : "LINE CLEAR!");
            #if UNITY_ANDROID || UNITY_IOS
            if (hapticsEnabled)
            {
                Handheld.Vibrate();
            }
            #endif
        }
        else if (result.NewHand)
        {
            Notify("NEW PIECES!");
        }
        else
        {
            Notify("NICE PLACEMENT!");
        }

        if (result.NewHand)
        {
            for (int i = 0; i < 3; i++)
            {
                trayTweens[i] = StartCoroutine(TrayDealPop(i, .06f * i));
            }
        }
        if (journeySession != null)
        {
            journeySession.OnPlaced(result, game);
        }
        else if (result.GameOver)
        {
            ending = true;
            overRoutine = StartCoroutine(ShowGameOver());
        }
    }

    private void AnimateScore(int target)
    {
        if (scoreRoutine != null)
        {
            StopCoroutine(scoreRoutine);
        }
        scoreRoutine = StartCoroutine(CountScore(displayedScore, target));
    }

    private IEnumerator CountScore(int start, int target)
    {
        float timer = 0f;
        const float duration = .45f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = 1f - (1f - t) * (1f - t) * (1f - t);
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            scoreText.text = displayedScore.ToString("N0");
            scoreText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.PI * Mathf.Clamp01(timer / duration)) * .065f);
            yield return null;
        }
        displayedScore = target;
        scoreText.text = target.ToString("N0");
        scoreText.rectTransform.localScale = Vector3.one;
        scoreRoutine = null;
    }

    private IEnumerator PlacementPop(List<Vector2Int> placed, int colorIndex)
    {
        List<RectTransform> overlays = new List<RectTransform>();
        foreach (Vector2Int cell in placed)
        {
            RectTransform block = Panel("Placement Pop", boardRect, CellPosition(cell.y, cell.x), new Vector2(CellSize, CellSize), Colors[colorIndex], false, true);
            activeEffects.Add(block.gameObject);
            overlays.Add(block);
        }
        float timer = 0f;
        while (timer < .22f)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / .22f);
            float scale = 1f + .15f * Mathf.Sin(Mathf.PI * t);
            foreach (RectTransform block in overlays)
            {
                if (block != null)
                {
                    block.localScale = Vector3.one * scale;
                    Image img = block.GetComponent<Image>();
                    Color color = Colors[colorIndex];
                    color.a = 1f - t;
                    img.color = color;
                }
            }
            yield return null;
        }
        foreach (RectTransform block in overlays)
        {
            if (block != null) DestroyEffect(block.gameObject);
        }
    }

    private IEnumerator FlashCleared(Vector2Int[] cleared)
    {
        if (cleared == null || cleared.Length == 0) yield break;
        List<RectTransform> highlights = new List<RectTransform>();
        foreach (Vector2Int cell in cleared)
        {
            RectTransform rect = Panel("Line Flash", boardRect, CellPosition(cell.y, cell.x), new Vector2(CellSize, CellSize), Color.white, false, true);
            activeEffects.Add(rect.gameObject);
            highlights.Add(rect);
        }
        float timer = 0f;
        while (timer < .39f)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / .39f);
            for (int i = 0; i < highlights.Count; i++)
            {
                RectTransform rect = highlights[i];
                if (rect == null) continue;
                rect.localScale = Vector3.one * (1f + .27f * t);
                rect.GetComponent<Image>().color = new Color(1f, .97f, .65f, (1f - t) * .97f);
            }
            yield return null;
        }
        foreach (RectTransform block in highlights)
        {
            if (block != null) DestroyEffect(block.gameObject);
        }
    }

    private IEnumerator BoardImpact(float strength)
    {
        float timer = 0f;
        while (timer < .24f)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / .24f);
            float wave = Mathf.Sin(t * Mathf.PI * 7f) * (1f - t);
            boardRect.anchoredPosition = boardRestPosition + new Vector2(wave * strength, Mathf.Sin(t * Mathf.PI * 5f) * strength * (1f - t));
            yield return null;
        }
        boardRect.anchoredPosition = boardRestPosition;
        boardImpactRoutine = null;
    }

    // The popup tracks the real placed squares, even when the board is nested
    // inside edited/scaled UI prefabs. No fixed Canvas screen coordinates.
    private Vector2 GetPlacementFeedbackAnchor(List<Vector2Int> placed)
    {
        if (placed == null || placed.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 average = Vector2.zero;
        foreach (Vector2Int cell in placed)
        {
            average += CellPosition(cell.y, cell.x);
        }
        average /= placed.Count;

        Vector2 canvasPoint = canvasRect.InverseTransformPoint(boardRect.TransformPoint(average));
        canvasPoint += Vector2.up * floatingScoreLift;

        // Avoid overlapping the top HUD or pushing the score off a portrait screen.
        return new Vector2(Mathf.Clamp(canvasPoint.x, -390f, 390f),
            Mathf.Clamp(canvasPoint.y, -590f, 555f));
    }

    private IEnumerator FloatingPoints(int points, bool special, Vector2 anchor)
    {
        Text floating = Label("Floating Score", canvasRect, anchor, new Vector2(320, 96),
            "+" + points, special ? 70 : 50, special ? Gold : PreviewGood, FontStyle.Bold);
        activeEffects.Add(floating.gameObject);
        Vector2 start = floating.rectTransform.anchoredPosition;
        float timer = 0f;
        while (timer < floatingScoreDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / floatingScoreDuration);
            floating.rectTransform.anchoredPosition = start + Vector2.up * floatingScoreRise * (1f - (1f - t) * (1f - t));
            Color c = special ? Gold : PreviewGood;
            c.a = Mathf.Clamp01((1f - t) * 2.1f);
            floating.color = c;
            yield return null;
        }
        if (floating != null) DestroyEffect(floating.gameObject);
    }

    private void AnimateCombo()
    {
        if (game.Combo < 2)
        {
            return;
        }

        if (comboRoutine != null)
        {
            StopCoroutine(comboRoutine);
            comboText.rectTransform.localScale = Vector3.one;
        }
        comboRoutine = StartCoroutine(ComboPunch());
    }

    private IEnumerator ComboPunch()
    {
        float timer = 0f;
        while (timer < .3f)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / .3f);
            comboText.rectTransform.localScale = Vector3.one * (1f + .22f * Mathf.Sin(Mathf.PI * t));
            yield return null;
        }
        comboText.rectTransform.localScale = Vector3.one;
        comboRoutine = null;
    }

    private IEnumerator TrayDealPop(int slot, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (trays[slot] == null) yield break;
        float timer = 0f;
        while (timer < .28f)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / .28f);
            trays[slot].localScale = Vector3.one * (.8f + .2f * t + .12f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        trays[slot].localScale = Vector3.one;
        trayTweens[slot] = null;
    }

    private IEnumerator ShowGameOver()
    {
        yield return new WaitForSecondsRealtime(.68f);
        finalText.text = game.Score.ToString("N0");
        gameOverPanel.SetActive(true);
        gameOverAlpha.alpha = 0f;
        float timer = 0f;
        while (timer < .35f)
        {
            timer += Time.unscaledDeltaTime;
            gameOverAlpha.alpha = Mathf.Clamp01(timer / .35f);
            yield return null;
        }
        gameOverAlpha.alpha = 1f;
        overRoutine = null;
    }

    private void Notify(string message)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }
        feedbackRoutine = StartCoroutine(Feedback(message));
    }

    private IEnumerator Feedback(string message)
    {
        feedbackText.text = message;
        feedbackText.color = Gold;
        yield return new WaitForSecondsRealtime(1.35f);
        feedbackText.text = game != null && game.GameOver ? "GAME OVER"
            : selectedSlot >= 0 ? "TAP THE GRID TO PLACE THE PIECE"
            : "DRAG A PIECE TO PLACE IT";
        feedbackText.color = TextDim;
        feedbackRoutine = null;
    }

    private void Restart()
    {
        if (journeySession != null)
        {
            journeySession.Retry();
            return;
        }
        StopAllCoroutines();
        foreach (GameObject effect in activeEffects)
        {
            if (effect != null) Destroy(effect);
        }
        activeEffects.Clear();
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
        draggingSlot = -1;
        activeDragPointerId = int.MinValue;
        selectedSlot = -1;
        ending = false;
        hoverValid = false;
        hoverNear = false;
        boardRect.anchoredPosition = boardRestPosition;
        boardImpactRoutine = null;
        invalidNudgeRoutine = null;
        comboRoutine = null;
        ClearDragPreviews();
        scoreText.rectTransform.localScale = Vector3.one;
        comboText.rectTransform.localScale = Vector3.one;
        scoreRoutine = null;
        feedbackRoutine = null;
        overRoutine = null;
        for (int i = 0; i < 3; i++)
        {
            trays[i].localScale = Vector3.one;
            trayTweens[i] = null;
        }
        gameOverAlpha.alpha = 0f;
        gameOverPanel.SetActive(false);
        game.Reset();
        PersistSession();
        displayedScore = 0;
        scoreText.text = "0";
        RefreshUI();
        Notify("NEW GAME!");
    }

    private void DestroyEffect(GameObject effect)
    {
        activeEffects.Remove(effect);
        Destroy(effect);
    }

    private void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        PlayerPrefs.SetInt(SoundKey, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshSoundText();
        if (soundEnabled) PlayClip(pickupClip, .6f);
    }

    private void ToggleHaptics()
    {
        hapticsEnabled = !hapticsEnabled;
        PlayerPrefs.SetInt(HapticKey, hapticsEnabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshHapticsText();
    }

    private void RefreshHapticsText()
    {
        if (hapticText != null)
        {
            hapticText.text = hapticsEnabled ? "VIBE ON" : "VIBE OFF";
        }
    }

    private void RefreshSoundText()
    {
        if (muteText != null)
        {
            muteText.text = soundEnabled ? "SFX ON" : "SFX OFF";
        }
    }

    private void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.55f;
        pickupClip = GenerateTone("Pickup", 600, 850, .095f, .16f);
        placeClip = GenerateTone("Place", 290, 440, .13f, .24f);
        clearClip = GenerateTone("Line Clear", 650, 1180, .34f, .31f);
        invalidClip = GenerateTone("Invalid", 230, 140, .12f, .17f);
    }

    private static AudioClip GenerateTone(string clipName, float fromHz, float toHz, float duration, float gain)
    {
        const int rate = 22050;
        int count = Mathf.CeilToInt(duration * rate);
        AudioClip clip = AudioClip.Create(clipName, count, 1, rate, false);
        float[] samples = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Mathf.Max(1, count - 1);
            float frequency = Mathf.Lerp(fromHz, toHz, t);
            phase += frequency * Mathf.PI * 2f / rate;
            float envelope = Mathf.Pow(1f - t, 2f) * Mathf.Clamp01(t * 30f);
            samples[i] = Mathf.Sin(phase) * envelope * gain;
        }
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (soundEnabled && clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
