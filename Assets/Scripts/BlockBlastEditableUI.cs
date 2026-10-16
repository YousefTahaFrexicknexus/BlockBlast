using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed partial class BlockBlastPrototype
{
    // The game logic works with authored scene objects. Dynamic pieces, previews and
    // effects are still instantiated at runtime, but permanent UI remains editable.
    private void BindAuthoredUI()
    {
        Transform uiRoot = transform.Find("Block Blast Canvas");
        if (uiRoot == null)
        {
            throw new InvalidOperationException("Classic UI Canvas is missing. Rebuild it using Tools > Magical Worlds > Hierarchy.");
        }

        canvas = uiRoot.GetComponent<Canvas>();
        canvasRect = (RectTransform)uiRoot;
        BlockBlastUIReferences authored = uiRoot.GetComponent<BlockBlastUIReferences>();
        if (authored != null && authored.IsComplete)
        {
            BindSerializedUI(authored);
            return;
        }

        boardRect = FindRequired<RectTransform>(uiRoot, "8 x 8 Grid");
        scoreText = FindRequired<Text>(uiRoot, "Score");
        bestText = FindRequired<Text>(uiRoot, "Best");
        comboText = FindRequired<Text>(uiRoot, "Combo");
        feedbackText = FindRequired<Text>(uiRoot, "Instruction");
        muteText = FindRequired<Text>(FindRequired<Button>(uiRoot, "Sound Toggle").transform, "");
        hapticText = FindRequired<Text>(FindRequired<Button>(uiRoot, "Haptics Toggle").transform, "");
        gameOverPanel = FindRequired<RectTransform>(uiRoot, "Game Over Overlay").gameObject;
        gameOverAlpha = gameOverPanel.GetComponent<CanvasGroup>();
        finalText = FindRequired<Text>(gameOverPanel.transform, "Final Score");
        RectTransform overlay = FindRequired<RectTransform>(uiRoot, "Dialog Overlay");
        dialogPanel = overlay.gameObject;
        dialogTitle = FindRequired<Text>(overlay, "Dialog Title");
        dialogBody = FindRequired<Text>(overlay, "Dialog Description");
        dialogPrimary = FindRequired<Button>(overlay, "Dialog Primary");
        dialogSecondary = FindRequired<Button>(overlay, "Dialog Secondary");
        dialogTertiary = FindRequired<Button>(overlay, "Dialog Tertiary");
        dialogSound = FindRequired<Button>(overlay, "Pause Sound");
        dialogVibration = FindRequired<Button>(overlay, "Pause Vibration");
        dialogMenu = FindRequired<Button>(overlay, "Dialog Main Menu");

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                cells[row, col] = FindRequired<Image>(boardRect, "Cell " + row + ", " + col);
                preview[row, col] = FindRequired<Image>(boardRect, "Preview " + row + ", " + col);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            trays[i] = FindRequired<RectTransform>(uiRoot, "Piece Tray " + (i + 1));
            trayPieceLayers[i] = FindRequired<RectTransform>(trays[i], "Piece Graphics");
            trays[i].GetComponent<BlockTrayPointer>().Initialize(this, i);
        }

        boardRect.GetComponent<BlockBoardPointer>().Initialize(this);
        boardRestPosition = boardRect.anchoredPosition;

        RewireButton(FindRequired<Button>(uiRoot, "Sound Toggle"), ToggleSound);
        RewireButton(FindRequired<Button>(uiRoot, "Haptics Toggle"), ToggleHaptics);
        RewireButton(FindRequired<Button>(uiRoot, "Pause"), OpenPause);
        RewireButton(FindRequired<Button>(gameOverPanel.transform, "Restart"), Restart);
        RewireButton(FindRequired<Button>(gameOverPanel.transform, "Game Over - Main Menu"), ReturnToMainMenu);

        RewireButton(dialogSound, () =>
        {
            ToggleSound();
            UpdateDialogSettingsLabels();
        });
        RewireButton(dialogMenu, ReturnToMainMenu);
        RewireButton(dialogVibration, () =>
        {
            ToggleHaptics();
            UpdateDialogSettingsLabels();
        });

        // Primary/secondary dialog actions are assigned by DisplayDialog at runtime.
        dialogPrimary.onClick.RemoveAllListeners();
        dialogSecondary.onClick.RemoveAllListeners();
        dialogTertiary.onClick.RemoveAllListeners();
        RefreshSoundText();
        RefreshHapticsText();
        UpdateDialogSettingsLabels();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject input = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private void BindSerializedUI(BlockBlastUIReferences view)
    {
        boardRect = view.Board;
        scoreText = view.Score;
        bestText = view.Best;
        comboText = view.Combo;
        feedbackText = view.Instruction;
        muteText = view.SoundLabel;
        hapticText = view.HapticsLabel;
        gameOverPanel = view.GameOverPanel;
        gameOverAlpha = view.GameOverGroup;
        finalText = view.FinalScore;
        dialogPanel = view.DialogPanel;
        dialogTitle = view.DialogTitle;
        dialogBody = view.DialogDescription;
        dialogPrimary = view.DialogPrimary;
        dialogSecondary = view.DialogSecondary;
        dialogTertiary = view.DialogTertiary;
        dialogSound = view.DialogSound;
        dialogVibration = view.DialogVibration;
        dialogMenu = view.DialogMenuButton;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;
                cells[row, col] = view.Cells[index];
                preview[row, col] = view.Previews[index];
            }
        }
        for (int slot = 0; slot < 3; slot++)
        {
            trays[slot] = view.Trays[slot];
            trayPieceLayers[slot] = view.PieceGraphics[slot];
            trays[slot].GetComponent<BlockTrayPointer>().Initialize(this, slot);
        }

        boardRect.GetComponent<BlockBoardPointer>().Initialize(this);
        boardRestPosition = boardRect.anchoredPosition;
        RewireButton(view.SoundButton, ToggleSound);
        RewireButton(view.HapticsButton, ToggleHaptics);
        RewireButton(view.PauseButton, OpenPause);
        RewireButton(view.RestartButton, Restart);
        RewireButton(view.GameOverMenuButton, ReturnToMainMenu);
        RewireButton(dialogSound, () =>
        {
            ToggleSound();
            UpdateDialogSettingsLabels();
        });
        RewireButton(dialogMenu, ReturnToMainMenu);
        RewireButton(dialogVibration, () =>
        {
            ToggleHaptics();
            UpdateDialogSettingsLabels();
        });

        dialogPrimary.onClick.RemoveAllListeners();
        dialogSecondary.onClick.RemoveAllListeners();
        dialogTertiary.onClick.RemoveAllListeners();
        RefreshSoundText();
        RefreshHapticsText();
        UpdateDialogSettingsLabels();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject input = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
    private static void RewireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static T FindRequired<T>(Transform root, string name) where T : Component
    {
        Transform found = String.IsNullOrEmpty(name) ? root : FindDescendant(root, name);
        if (found == null)
        {
            throw new InvalidOperationException("Missing UI element '" + name + "' under " + root.name + ". Rebuild or fix the Classic UI Prefab.");
        }
        T component = found.GetComponent<T>();
        if (component == null && String.IsNullOrEmpty(name))
        {
            component = found.GetComponentInChildren<T>(true);
        }
        if (component == null)
        {
            throw new InvalidOperationException("UI element '" + name + "' is missing " + typeof(T).Name + ".");
        }
        return component;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform match = FindDescendant(child, name);
            if (match != null)
            {
                return match;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    // Editor generator uses the same layout code as the runtime fallback. This
    // prevents a second hard-coded "designer" layout from drifting out of sync.
    public void BuildEditorVisualLayout(Sprite permanentRoundedSprite)
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        roundedSprite = permanentRoundedSprite;
        ownsRoundedSprite = false;
        BuildUI();
    }
#endif
}