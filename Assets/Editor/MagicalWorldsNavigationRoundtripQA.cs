using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsNavigationRoundtripQA
{
    private const string OnceKey = "MagicalWorlds.Navigation.Roundtrip.Once.v6";
    private const string PendingKey = "MagicalWorlds.Navigation.Roundtrip.Pending.v1";
    private static double started;
    private static int frameCount;
    private static bool usedPause;

    static MagicalWorldsNavigationRoundtripQA()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += StartAutomaticallyOnce;
    }

    private static void StartAutomaticallyOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += StartAutomaticallyOnce;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorPrefs.GetBool(OnceKey, false) ||
            SceneManager.GetActiveScene().name != "MainMenu" ||
            SceneManager.GetActiveScene().isDirty)
        {
            return;
        }

        EditorPrefs.SetBool(OnceKey, true);
        StartTest();
    }

    [MenuItem("Tools/Magical Worlds/Main Menu/Test Navigation Roundtrip")]
    private static void StartTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Menu Navigation QA] Stop Play Mode before starting navigation roundtrip.");
            return;
        }
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Debug.LogWarning("[Menu Navigation QA] Open MainMenu scene first.");
            return;
        }
        EditorPrefs.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(PendingKey, false))
        {
            started = EditorApplication.timeSinceStartup;
            EditorApplication.delayCall += StartAtMainMenu;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= WaitForClassic;
            EditorApplication.update -= WaitForMenu;
        }
    }

    private static void StartAtMainMenu()
    {
        try
        {
            var menu = UnityEngine.Object.FindFirstObjectByType<MagicalWorldsMenuController>();
            Require(menu != null, "Main Menu controller missing at startup");
            Button play = FindDeep(menu.transform, "Button_Play").GetComponent<Button>();
            Require(play != null && play.IsActive() && play.interactable, "Play button cannot be pressed");
            Require(UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>() == null,
                "Classic gameplay was created in the Main Menu scene");

            play.onClick.Invoke();
            frameCount = 0;
            EditorApplication.update += WaitForClassic;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private static void WaitForClassic()
    {
        if (Timeout())
        {
            Fail(new InvalidOperationException("Timed out waiting for Classic scene"));
            return;
        }
        if (SceneManager.GetActiveScene().name != "ClassicGameplay")
        {
            return;
        }

        frameCount++;
        if (frameCount < 3)
        {
            return;
        }
        EditorApplication.update -= WaitForClassic;
        try
        {
            BlockBlastPrototype classic = UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>();
            Require(classic != null, "ClassicGameplay has no gameplay controller");
            BlockBlastUIReferences view = classic.GetComponentInChildren<BlockBlastUIReferences>(true);
            Require(view != null && view.IsComplete, "Classic gameplay has incomplete authored references");

            // Verify the actual pause button and its Main Menu callback where
            // gameplay state permits it.
            bool dragTested = TestClassicDrag(classic, view);
            view.PauseButton.onClick.Invoke();
            if (dragTested) CheckDragCancelled(classic, view);
            if (view.DialogPanel.activeSelf)
            {
                view.DialogMenuButton.onClick.Invoke();
                usedPause = true;
            }
            else
            {
                // A previously saved completed run or open Guide may suppress Pause.
                Require(view.GameOverMenuButton != null, "Game Over Main Menu control missing");
                view.GameOverMenuButton.onClick.Invoke();
                usedPause = false;
            }

            frameCount = 0;
            EditorApplication.update += WaitForMenu;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private static bool TestClassicDrag(BlockBlastPrototype classic, BlockBlastUIReferences view)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(BlockBlastPrototype);
        var game = (BlockBlastClone.BlockBlastModel)type.GetField("game", flags).GetValue(classic);
        bool paused = (bool)type.GetField("isPaused", flags).GetValue(classic);
        bool ending = (bool)type.GetField("ending", flags).GetValue(classic);
        if (game == null || game.GameOver || paused || ending)
        {
            Debug.Log("[Classic Feel Runtime QA] Skipped drag: restored session is not in an active playable state.");
            return false;
        }

        int slot = -1;
        for (int i = 0; i < game.Hand.Length; i++)
        {
            if (game.Hand[i] != null)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0)
        {
            Debug.Log("[Classic Feel Runtime QA] Skipped drag: no available hand piece.");
            return false;
        }

        var piece = game.Hand[slot];
        RectTransform canvas = classic.GetComponentInChildren<Canvas>(true).GetComponent<RectTransform>();
        RectTransform board = view.Board;
        Vector2 first = (Vector2)type.GetMethod("CellPosition", flags).Invoke(classic, new object[] {0, 0});
        const float step = (916f - 24f - 42f) / 8f + 6f;
        Vector2 center = first + new Vector2((piece.Width - 1) * step * .5f,
            -(piece.Height - 1) * step * .5f);
        float lift = (float)type.GetField("dragLift", flags).GetValue(classic);
        Vector2 ghostCanvas = canvas.InverseTransformPoint(board.TransformPoint(center));
        Vector2 pointerCanvas = ghostCanvas - Vector2.up * lift;

        var pointer = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current)
        {
            pointerId = 71,
            position = RectTransformUtility.WorldToScreenPoint(null, canvas.TransformPoint(pointerCanvas))
        };

        Vector2 startPosition = pointer.position;
        classic.StartPieceDrag(slot, pointer);
        Require((int)type.GetField("hoverCol", flags).GetValue(classic) == 0 &&
            (int)type.GetField("hoverRow", flags).GetValue(classic) == 0,
            "Pointer drag failed to snap to the board's top-left origin.");
        Require(((System.Collections.IList)type.GetField("visibleDragPreviews", flags).GetValue(classic)).Count > 0,
            "Placement highlights were never registered for cleanup.");
        Require(ActivePreviewCount(view) > 0, "Placement highlights should be visible during a drag.");

        var wrongPointer = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current)
        {
            pointerId = 72,
            position = Vector2.zero
        };
        classic.ContinuePieceDrag(wrongPointer);
        classic.FinishPieceDrag(wrongPointer);
        Require((int)type.GetField("draggingSlot", flags).GetValue(classic) == slot,
            "A secondary pointer incorrectly ended the current drag.");

        // Reproduce the reported bug: hover a board cell, move outside the
        // board without placing, then release. No preview may remain active.
        pointer.position = new Vector2(-5000f, -5000f);
        classic.ContinuePieceDrag(pointer);
        Require(ActivePreviewCount(view) == 0,
            "Moving the dragged piece outside the board left stale placement highlights.");
        classic.FinishPieceDrag(pointer);
        Require(ActivePreviewCount(view) == 0,
            "Invalid release left a highlighted cell without placing a block.");

        // Start another drag so the existing Pause regression can verify
        // that cancelling a live drag also removes the registered previews.
        pointer.position = startPosition;
        classic.StartPieceDrag(slot, pointer);
        Require(ActivePreviewCount(view) > 0,
            "A second drag failed to show a new placement preview.");

        Debug.Log("[Classic Feel Runtime QA] PASSED: tracked hover preview, off-board cleanup, invalid release, " +
            "second-pointer rejection and second drag pickup.");
        return true;
    }

    private static int ActivePreviewCount(BlockBlastUIReferences view)
    {
        int count = 0;
        foreach (Image cell in view.Previews)
        {
            if (cell != null && cell.gameObject.activeSelf)
            {
                count++;
            }
        }
        return count;
    }
    private static void CheckDragCancelled(BlockBlastPrototype classic, BlockBlastUIReferences view)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(BlockBlastPrototype);
        Require((int)type.GetField("draggingSlot", flags).GetValue(classic) == -1,
            "Pause did not cancel the active piece drag.");
        var previews = (System.Collections.IList)type.GetField("visibleDragPreviews", flags).GetValue(classic);
        Require(previews.Count == 0, "Pause did not clear active placement previews.");
        Require(ActivePreviewCount(view) == 0, "Pause left placement highlights visible on the board.");
        Require(type.GetField("ghost", flags).GetValue(classic) == null ||
            (UnityEngine.Object)type.GetField("ghost", flags).GetValue(classic) == null,
            "Pause left the dragged ghost visible.");
        Debug.Log("[Classic Feel Runtime QA] PASSED: Pause cancelled drag and removed all previews.");
    }
    private static void WaitForMenu()
    {
        if (Timeout())
        {
            Fail(new InvalidOperationException("Timed out waiting for return to Main Menu"));
            return;
        }
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }

        frameCount++;
        if (frameCount < 3)
        {
            return;
        }
        EditorApplication.update -= WaitForMenu;
        try
        {
            Require(UnityEngine.Object.FindFirstObjectByType<MagicalWorldsMenuController>() != null,
                "Menu controller missing on return");
            Require(UnityEngine.Object.FindFirstObjectByType<BlockBlastPrototype>() == null,
                "Classic gameplay leaked into Main Menu scene");
            Debug.Log("[Menu Navigation QA] PASSED: MainMenu -> Play button -> ClassicGameplay -> " +
                (usedPause ? "Pause > Main Menu" : "Game Over menu action (saved-session fallback)") +
                " -> MainMenu. No duplicate Classic controller.");
            Finish();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private static void Fail(Exception ex)
    {
        Debug.LogError("[Menu Navigation QA] FAILED: " + ex);
        Finish();
    }

    private static void Finish()
    {
        EditorApplication.update -= WaitForClassic;
        EditorApplication.update -= WaitForMenu;
        EditorPrefs.SetBool(PendingKey, false);
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static bool Timeout()
    {
        return EditorApplication.timeSinceStartup - started > 20.0;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform match = FindDeep(child, name);
            if (match != null)
            {
                return match;
            }
        }
        return null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
