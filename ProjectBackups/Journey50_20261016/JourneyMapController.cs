using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JourneyMapController : MonoBehaviour
{
    [SerializeField] private JourneyLevelNode[] levelNodes;
    [SerializeField] private Button backButton;
    [SerializeField] private Text progressLabel;

    private JourneyLevelCatalog catalog;
    private bool loading;

    private void Start()
    {
        catalog = JourneyLevelCatalog.Load();
        if (catalog == null)
        {
            Debug.LogError("[Journey] Missing level catalog. Regenerate Journey content using the Editor tool.");
            return;
        }

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(ReturnToMenu);

        int completed = 0;
        for (int i = 1; i <= catalog.Count; i++)
        {
            if (JourneyProgress.Stars(i) > 0) completed++;
        }
        progressLabel.text = completed + " / " + catalog.Count + " LEVELS COMPLETE";

        for (int i = 0; i < levelNodes.Length; i++)
        {
            if (levelNodes[i] != null)
            {
                int level = levelNodes[i].LevelNumber;
                levelNodes[i].Configure(level,
                    level <= JourneyProgress.HighestUnlocked && level <= catalog.Count,
                    JourneyProgress.Stars(level), this,
                    level <= catalog.Count && JourneyAttemptStore.HasAttempt(catalog.Get(level)));
            }
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!loading && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            ReturnToMenu();
        }
    }
    public void Select(int level)
    {
        if (loading || catalog == null || !JourneyProgress.Select(level, catalog.Count)) return;
        loading = true;
        SceneManager.LoadScene("JourneyGameplay");
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
