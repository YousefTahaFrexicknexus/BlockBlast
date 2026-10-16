using UnityEngine;
using UnityEngine.UI;

// Serialized UI references live on the authored Canvas. Designers can edit
// hierarchy names without breaking runtime bindings. Rewire from children only
// after intentionally changing the structure.
[DisallowMultipleComponent]
public sealed class BlockBlastUIReferences : MonoBehaviour
{
    [Header("Board - 8 x 8")]
    public RectTransform Board;
    public Image[] Cells = new Image[64];
    public Image[] Previews = new Image[64];

    [Header("Piece Trays")]
    public RectTransform[] Trays = new RectTransform[3];
    public RectTransform[] PieceGraphics = new RectTransform[3];

    [Header("HUD")]
    public Text Score;
    public Text Best;
    public Text Combo;
    public Text Instruction;
    public Text SoundLabel;
    public Text HapticsLabel;
    public Button SoundButton;
    public Button HapticsButton;
    public Button PauseButton;

    [Header("Game Over")]
    public GameObject GameOverPanel;
    public CanvasGroup GameOverGroup;
    public Text FinalScore;
    public Button RestartButton;
    public Button GameOverMenuButton;

    [Header("Dialogs")]
    public GameObject DialogPanel;
    public Text DialogTitle;
    public Text DialogDescription;
    public Button DialogPrimary;
    public Button DialogSecondary;
    public Button DialogTertiary;
    public Button DialogSound;
    public Button DialogVibration;
    public Button DialogMenuButton;

    public bool IsComplete
    {
        get
        {
            if (Board == null || Cells == null || Cells.Length != 64 ||
                Previews == null || Previews.Length != 64 ||
                Trays == null || Trays.Length != 3 ||
                PieceGraphics == null || PieceGraphics.Length != 3 ||
                Score == null || Best == null || Combo == null || Instruction == null ||
                SoundLabel == null || HapticsLabel == null || SoundButton == null ||
                HapticsButton == null || PauseButton == null || GameOverPanel == null ||
                GameOverGroup == null || FinalScore == null || RestartButton == null || GameOverMenuButton == null ||
                DialogPanel == null || DialogTitle == null || DialogDescription == null ||
                DialogPrimary == null || DialogSecondary == null || DialogTertiary == null ||
                DialogSound == null || DialogVibration == null || DialogMenuButton == null)
            {
                return false;
            }
            for (int i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] == null || Previews[i] == null)
                {
                    return false;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (Trays[i] == null || PieceGraphics[i] == null)
                {
                    return false;
                }
            }
            return true;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild UI References from Hierarchy")]
    public void RebuildReferences()
    {
        Board = Find<RectTransform>(transform, "8 x 8 Grid");
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;
                Cells[index] = Find<Image>(Board, "Cell " + row + ", " + col);
                Previews[index] = Find<Image>(Board, "Preview " + row + ", " + col);
            }
        }
        for (int i = 0; i < 3; i++)
        {
            Trays[i] = Find<RectTransform>(transform, "Piece Tray " + (i + 1));
            PieceGraphics[i] = Find<RectTransform>(Trays[i], "Piece Graphics");
        }

        Score = Find<Text>(transform, "Score");
        Best = Find<Text>(transform, "Best");
        Combo = Find<Text>(transform, "Combo");
        Instruction = Find<Text>(transform, "Instruction");
        SoundButton = Find<Button>(transform, "Sound Toggle");
        HapticsButton = Find<Button>(transform, "Haptics Toggle");
        SoundLabel = SoundButton != null ? SoundButton.GetComponentInChildren<Text>(true) : null;
        HapticsLabel = HapticsButton != null ? HapticsButton.GetComponentInChildren<Text>(true) : null;
        PauseButton = Find<Button>(transform, "Pause");

        RectTransform over = Find<RectTransform>(transform, "Game Over Overlay");
        GameOverPanel = over != null ? over.gameObject : null;
        GameOverGroup = over != null ? over.GetComponent<CanvasGroup>() : null;
        FinalScore = Find<Text>(over, "Final Score");
        RestartButton = Find<Button>(over, "Restart");
        GameOverMenuButton = Find<Button>(over, "Game Over - Main Menu");

        RectTransform dialog = Find<RectTransform>(transform, "Dialog Overlay");
        DialogPanel = dialog != null ? dialog.gameObject : null;
        DialogTitle = Find<Text>(dialog, "Dialog Title");
        DialogDescription = Find<Text>(dialog, "Dialog Description");
        DialogPrimary = Find<Button>(dialog, "Dialog Primary");
        DialogSecondary = Find<Button>(dialog, "Dialog Secondary");
        DialogTertiary = Find<Button>(dialog, "Dialog Tertiary");
        DialogSound = Find<Button>(dialog, "Pause Sound");
        DialogVibration = Find<Button>(dialog, "Pause Vibration");
        DialogMenuButton = Find<Button>(dialog, "Dialog Main Menu");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static T Find<T>(Transform root, string name) where T : Component
    {
        if (root == null)
        {
            return null;
        }
        Transform t = FindByName(root, name);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static Transform FindByName(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform match = FindByName(child, name);
            if (match != null)
            {
                return match;
            }
        }
        return null;
    }
#endif
}