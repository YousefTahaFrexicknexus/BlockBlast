using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JourneyLevelNode : MonoBehaviour
{
    [SerializeField, Min(1)] private int levelNumber = 1;
    public int LevelNumber => levelNumber;
    [SerializeField] private Button selectButton;
    [SerializeField] private Text numberText;
    [SerializeField] private Text starsText;
    [SerializeField] private Text stateText;

    private int number;
    private JourneyMapController map;

#if UNITY_EDITOR
    public void ConfigureInEditor(int number)
    {
        levelNumber = Mathf.Max(1, number);
        if (numberText != null) numberText.text = levelNumber.ToString();
        if (starsText != null) starsText.text = "";
        if (stateText != null) stateText.text = levelNumber == 1 ? "PLAY" : "LOCKED";
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
    public void Configure(int level, bool unlocked, int stars, JourneyMapController owner)
    {
        number = level;
        map = owner;
        numberText.text = unlocked ? level.ToString() : "LOCKED";
        starsText.text = unlocked && stars > 0 ? new string('★', stars) + new string('☆', 3 - stars) : "";
        stateText.text = unlocked ? (stars > 0 ? "COMPLETE" : "PLAY") : "LOCKED";
        selectButton.interactable = unlocked;
        selectButton.onClick.RemoveAllListeners();
        if (unlocked) selectButton.onClick.AddListener(SelectLevel);
    }

    private void SelectLevel()
    {
        if (map != null) map.Select(number);
    }
}

