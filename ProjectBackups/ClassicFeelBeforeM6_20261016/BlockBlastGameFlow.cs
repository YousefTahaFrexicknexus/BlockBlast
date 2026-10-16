using System;
using BlockBlastClone;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class BlockBlastPrototype
{
    private const string SaveKey = "BlockBlastClassicSaveV1";
    private const string TutorialKey = "BlockBlastClassicTutorialSeenV1";

    private enum GameDialog
    {
        None,
        Pause,
        Guide,
        ConfirmRestart
    }

    private GameDialog dialog;
    private bool isPaused;
    private bool guideOpenedFromPause;
    private GameObject dialogPanel;
    private Text dialogTitle;
    private Text dialogBody;
    private Button dialogPrimary;
    private Button dialogSecondary;
    private Button dialogTertiary;
    private Button dialogSound;
    private Button dialogVibration;
    private Button dialogMenu;

    private bool RestoreSession()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return false;
        }

        try
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            BlockBlastSnapshot saved = JsonUtility.FromJson<BlockBlastSnapshot>(json);
            if (game.TryRestore(saved))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Block Puzzle] Invalid saved game; starting fresh: " + ex.Message);
        }

        PlayerPrefs.DeleteKey(SaveKey);
        return false;
    }

    private void PersistSession()
    {
        if (game == null)
        {
            return;
        }
        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(game.ExportSnapshot()));
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Block Puzzle] Failed to save current run: " + ex.Message);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            PersistSession();
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            PersistSession();
        }
    }

    private void OnApplicationQuit()
    {
        PersistSession();
    }

    private void BuildFlowUI()
    {
        RectTransform fullOverlay = Panel("Dialog Overlay", canvasRect, Vector2.zero,
            new Vector2(1080, 1920), new Color(.025f, .045f, .13f, .94f), true);
        fullOverlay.anchorMin = Vector2.zero;
        fullOverlay.anchorMax = Vector2.one;
        fullOverlay.offsetMin = Vector2.zero;
        fullOverlay.offsetMax = Vector2.zero;
        dialogPanel = fullOverlay.gameObject;

        RectTransform card = Panel("Dialog Card", fullOverlay,
            new Vector2(0, 0), new Vector2(920, 1080), Hex("#1A2C68"), false, true);
        Panel("Card Header Accent", card, new Vector2(0, 473), new Vector2(820, 10), Hex("#62C7FF"), false, true);

        dialogTitle = Label("Dialog Title", card, new Vector2(0, 322), new Vector2(815, 115),
            "PAUSED", 72, TextWhite, FontStyle.Bold);
        dialogBody = Label("Dialog Description", card, new Vector2(0, 75), new Vector2(760, 305),
            "", 39, TextDim);

        dialogSound = MakeButton("Pause Sound", card, new Vector2(-222, 434),
            new Vector2(358, 66), "SFX ON", Hex("#304779"), 27);
        dialogSound.onClick.AddListener(() => { ToggleSound(); UpdateDialogSettingsLabels(); });
        dialogVibration = MakeButton("Pause Vibration", card, new Vector2(222, 434),
            new Vector2(358, 66), "VIBE OFF", Hex("#304779"), 27);
        dialogVibration.onClick.AddListener(() => { ToggleHaptics(); UpdateDialogSettingsLabels(); });
        UpdateDialogSettingsLabels();

        dialogPrimary = MakeButton("Dialog Primary", card, new Vector2(0, -200),
            new Vector2(735, 120), "CONTINUE", Hex("#3276F5"), 43);
        dialogSecondary = MakeButton("Dialog Secondary", card, new Vector2(0, -345),
            new Vector2(735, 114), "HOW TO PLAY", Hex("#35538F"), 40);
        dialogTertiary = MakeButton("Dialog Tertiary", card, new Vector2(0, -477),
            new Vector2(735, 100), "NEW GAME", Hex("#314376"), 37);

        dialogMenu = MakeButton("Dialog Main Menu", fullOverlay, new Vector2(0, -620),
            new Vector2(580, 96), "MAIN MENU", Hex("#5C4BC6"), 36);
        dialogMenu.onClick.AddListener(ReturnToMainMenu);

        dialogPanel.SetActive(false);
    }

    private void ShowInitialFlow(bool resumed)
    {
        if (resumed && game.GameOver)
        {
            finalText.text = game.Score.ToString("N0");
            gameOverPanel.SetActive(true);
            gameOverAlpha.alpha = 1f;
            ending = true;
        }

        if (PlayerPrefs.GetInt(TutorialKey, 0) == 0 && !game.GameOver)
        {
            OpenGuide(false);
        }
        else
        {
            Notify(resumed ? "WELCOME BACK!" : "DRAG A PIECE TO PLACE IT");
        }
    }

    private void SetDialogAction(Button button, string title, UnityEngine.Events.UnityAction action)
    {
        button.gameObject.SetActive(true);
        button.GetComponentInChildren<Text>().text = title;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void DisplayDialog(GameDialog kind)
    {
        dialog = kind;
        isPaused = kind != GameDialog.None;
        dialogPanel.SetActive(isPaused);
        if (!isPaused)
        {
            return;
        }

        dialogPrimary.gameObject.SetActive(false);
        dialogSecondary.gameObject.SetActive(false);
        dialogTertiary.gameObject.SetActive(false);
        dialogMenu.gameObject.SetActive(kind == GameDialog.Pause);
        dialogSound.gameObject.SetActive(kind == GameDialog.Pause);
        dialogVibration.gameObject.SetActive(kind == GameDialog.Pause);
        UpdateDialogSettingsLabels();

        switch (kind)
        {
            case GameDialog.Pause:
                dialogTitle.text = "PAUSED";
                dialogBody.text = "YOUR PROGRESS IS SAVED\n\nSCORE  " + game.Score.ToString("N0") +
                    "\nBEST  " + best.ToString("N0");
                SetDialogAction(dialogPrimary, "CONTINUE", CloseDialog);
                SetDialogAction(dialogSecondary, "HOW TO PLAY", () => OpenGuide(true));
                SetDialogAction(dialogTertiary, "NEW GAME", () => DisplayDialog(GameDialog.ConfirmRestart));
                break;
            case GameDialog.Guide:
                dialogTitle.text = "HOW TO PLAY";
                dialogBody.text = "1  DRAG A BLOCK SHAPE\n     ONTO THE 8 x 8 BOARD\n\n" +
                    "2  COMPLETE FULL ROWS\n     OR COLUMNS TO CLEAR\n\n" +
                    "3  USE ALL THREE PIECES\n     TO GET A NEW HAND\n\n" +
                    "KEEP SPACE OPEN TO SURVIVE!";
                SetDialogAction(dialogPrimary, guideOpenedFromPause ? "BACK TO PAUSE" : "LET'S PLAY", CloseGuide);
                break;
            case GameDialog.ConfirmRestart:
                dialogTitle.text = "START OVER?";
                dialogBody.text = "THIS WILL ERASE YOUR\nCURRENT BOARD AND SCORE.\n\nYOUR BEST SCORE\nWILL BE KEPT.";
                SetDialogAction(dialogPrimary, "START NEW GAME", ConfirmRestart);
                SetDialogAction(dialogSecondary, "KEEP PLAYING", () => DisplayDialog(GameDialog.Pause));
                break;
        }
    }

    private void UpdateDialogSettingsLabels()
    {
        if (dialogSound != null)
        {
            dialogSound.GetComponentInChildren<Text>().text = soundEnabled ? "SFX ON" : "SFX OFF";
        }
        if (dialogVibration != null)
        {
            dialogVibration.GetComponentInChildren<Text>().text = hapticsEnabled ? "VIBE ON" : "VIBE OFF";
        }
    }

    private void ReturnToMainMenu()
    {
        PersistSession();
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError("[Magical Worlds] MainMenu is missing from Build Settings.");
        }
    }

    private void OpenPause()
    {
        if (ending || game.GameOver || isPaused)
        {
            return;
        }
        CancelActiveDrag();
        selectedSlot = -1;
        for (int i = 0; i < 3; i++) DrawTray(i);
        PersistSession();
        DisplayDialog(GameDialog.Pause);
    }

    private void OpenGuide(bool fromPause)
    {
        guideOpenedFromPause = fromPause;
        DisplayDialog(GameDialog.Guide);
    }

    private void CloseGuide()
    {
        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.Save();
        DisplayDialog(guideOpenedFromPause ? GameDialog.Pause : GameDialog.None);
        if (!guideOpenedFromPause)
        {
            Notify("DRAG A PIECE TO PLACE IT");
        }
    }

    private void CloseDialog()
    {
        DisplayDialog(GameDialog.None);
    }

    private void ConfirmRestart()
    {
        DisplayDialog(GameDialog.None);
        Restart();
    }

    private void CancelActiveDrag()
    {
        draggingSlot = -1;
        hoverNear = false;
        hoverValid = false;
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
        DrawBoard();
    }

    private void UpdateFlowKeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }
        if (dialog == GameDialog.Guide)
        {
            CloseGuide();
        }
        else if (dialog == GameDialog.ConfirmRestart)
        {
            DisplayDialog(GameDialog.Pause);
        }
        else if (dialog == GameDialog.Pause)
        {
            CloseDialog();
        }
        else
        {
            OpenPause();
        }
    }
}
