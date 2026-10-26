using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JourneyMapController : MonoBehaviour
{
    private const int LevelsPerPage = 6;

    [SerializeField] private JourneyLevelNode[] levelNodes;
    [SerializeField] private Button backButton;
    [SerializeField] private Text progressLabel;
    [Header("Paged map — six editable levels per page")]
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Text pageLabel;

    private JourneyLevelCatalog catalog;
    private bool loading;
    private int currentPage;

    public int CurrentPage => currentPage;
    public int TotalPages => catalog != null ? Mathf.CeilToInt((float)catalog.Count / LevelsPerPage) : 0;

    private void Start()
    {
        catalog = JourneyLevelCatalog.Load();
        if (catalog == null)
        {
            Debug.LogError("[Journey] Level catalog missing.");
            enabled = false;
            return;
        }

        Bind(backButton, ReturnToMenu);
        Bind(previousPageButton, PreviousPage);
        Bind(nextPageButton, NextPage);

        int completed = 0;
        for (int level = 1; level <= catalog.Count; level++)
        {
            if (JourneyProgress.Stars(level) > 0) completed++;
        }
        if (progressLabel != null)
            progressLabel.text = completed + " / " + catalog.Count + " LEVELS COMPLETE";

        if (levelNodes == null || levelNodes.Length != catalog.Count)
            Debug.LogError("[Journey] Map prefab node count differs from level catalog.");

        if (levelNodes != null)
        {
            foreach (JourneyLevelNode node in levelNodes)
            {
                if (node == null) continue;
                int level = node.LevelNumber;
                JourneyLevelDefinition definition = catalog.Get(level);
                if (definition == null)
                {
                    node.gameObject.SetActive(false);
                    continue;
                }

                bool unlocked = level <= JourneyProgress.HighestUnlocked;
                node.Configure(level, unlocked, JourneyProgress.Stars(level), this,
                    unlocked && JourneyAttemptStore.HasAttempt(definition));
            }
        }

        ShowPage((JourneyProgress.SelectedLevel - 1) / LevelsPerPage);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!loading && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            ReturnToMenu();
    }

    public void ShowPage(int page)
    {
        if (catalog == null) catalog = JourneyLevelCatalog.Load();
        if (catalog == null || loading) return;

        currentPage = Mathf.Clamp(page, 0, Mathf.Max(0, TotalPages - 1));
        if (levelNodes != null)
        {
            foreach (JourneyLevelNode node in levelNodes)
            {
                if (node == null) continue;
                bool shown = (node.LevelNumber - 1) / LevelsPerPage == currentPage;
                node.gameObject.SetActive(shown);
            }
        }

        int first = currentPage * LevelsPerPage + 1;
        int last = Mathf.Min(first + LevelsPerPage - 1, catalog.Count);
        if (pageLabel != null)
            pageLabel.text = "LEVELS " + first + " - " + last + "    (" +
                (currentPage + 1) + "/" + TotalPages + ")";
        if (previousPageButton != null) previousPageButton.interactable = currentPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = currentPage < TotalPages - 1;
    }

    public void NextPage()
    {
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        ShowPage(currentPage - 1);
    }

    public void Select(int level)
    {
        if (loading || catalog == null || !JourneyProgress.Select(level, catalog.Count)) return;
        loading = true;
        SceneManager.LoadScene("JourneyGameplay");
    }

    private void ReturnToMenu()
    {
        if (loading) return;
        loading = true;
        SceneManager.LoadScene("MainMenu");
    }
}
