using System;
using System.Collections.Generic;
using System.IO;
using MagicalWorlds.Journey;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public sealed class JourneyPlaytestWindow : EditorWindow
{
    private const string ScenePath = "Assets/Scenes/JourneyGameplay.unity";
    private int levelNumber = 1;
    private Vector2 scroll;
    private Dictionary<int, Summary> stats = new Dictionary<int, Summary>();
    private bool showAll = true;

    private sealed class Summary
    {
        public int wins, losses, retries, abandons, totalMoves, totalStars;
        public int Completed => wins + losses;
    }

    static JourneyPlaytestWindow()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                JourneyPlaytestContext.Clear();
        };
    }

    [MenuItem("Tools/Magical Worlds/Journey/Playtest Dashboard")]
    public static void Open()
    {
        JourneyPlaytestWindow window = GetWindow<JourneyPlaytestWindow>("Journey Playtest");
        window.minSize = new Vector2(630, 450);
        window.Refresh();
    }

    private void OnEnable() { Refresh(); }

    private void OnGUI()
    {
        JourneyLevelCatalog catalog = AssetDatabase.LoadAssetAtPath<JourneyLevelCatalog>(
            "Assets/Resources/MagicalWorlds/Journey/LevelCatalog.asset");
        if (catalog == null || catalog.Count < 1)
        {
            EditorGUILayout.HelpBox("Journey level catalog was not found.", MessageType.Error);
            return;
        }
        levelNumber = Mathf.Clamp(levelNumber, 1, catalog.Count);
        EditorGUILayout.LabelField("Journey · 50-level playtest dashboard", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Preview mode doesn't change saved unlocks, stars, selected level, or unfinished attempts. Logs are written to ProjectReports for balancing.", MessageType.Info);
        levelNumber = EditorGUILayout.IntSlider("Preview level", levelNumber, 1, catalog.Count);
        JourneyLevelDefinition current = catalog.Get(levelNumber);
        EditorGUILayout.ObjectField("Level asset", current, typeof(JourneyLevelDefinition), false);
        if (current != null)
        {
            EditorGUILayout.LabelField("Goal", current.ObjectiveText);
            EditorGUILayout.LabelField("Moves / Seed", current.moveLimit + " moves   /   " + current.randomSeed);
        }

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
        if (GUILayout.Button("Play Selected Level", GUILayout.Height(32)))
            PlayLevel(levelNumber);
        if (GUILayout.Button("Open Journey Map", GUILayout.Height(32)))
            OpenScene("Assets/Scenes/JourneyMap.unity");
        GUI.enabled = EditorApplication.isPlaying;
        if (GUILayout.Button("Stop Playtest", GUILayout.Height(32)))
            EditorApplication.isPlaying = false;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Results")) Refresh();
        if (GUILayout.Button("Export CSV")) Export(catalog);
        if (GUILayout.Button("Open Report Folder")) OpenReportFolder();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        showAll = EditorGUILayout.Foldout(showAll, "Per-level playtest results", true);
        if (!showAll) return;
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Level / Name", GUILayout.Width(225));
        GUILayout.Label("Wins", GUILayout.Width(50));
        GUILayout.Label("Losses", GUILayout.Width(55));
        GUILayout.Label("Retries", GUILayout.Width(55));
        GUILayout.Label("Abandon", GUILayout.Width(60));
        GUILayout.Label("Win %", GUILayout.Width(55));
        GUILayout.Label("Avg Moves", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 1; i <= catalog.Count; i++)
        {
            Summary s = GetSummary(i);
            JourneyLevelDefinition level = catalog.Get(i);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(i.ToString("00") + "  " + (level != null ? level.levelName : "MISSING"),
                EditorStyles.linkLabel, GUILayout.Width(225)))
            {
                levelNumber = i;
                GUI.FocusControl(null);
            }
            GUILayout.Label(s.wins.ToString(), GUILayout.Width(50));
            GUILayout.Label(s.losses.ToString(), GUILayout.Width(55));
            GUILayout.Label(s.retries.ToString(), GUILayout.Width(55));
            GUILayout.Label(s.abandons.ToString(), GUILayout.Width(60));
            GUILayout.Label(s.Completed > 0 ? (100f * s.wins / s.Completed).ToString("F0") + "%" : "—",
                GUILayout.Width(55));
            GUILayout.Label(s.wins > 0 ? (s.totalMoves / (float)s.wins).ToString("F1") : "—",
                GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private static bool OpenScene(string scene)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(scene);
        return true;
    }

    private static void PlayLevel(int number)
    {
        if (!OpenScene(ScenePath)) return;
        JourneyPlaytestContext.SetPreview(number);
        Debug.Log("[Journey Playtest] Previewing level " + number + " without modifying progression.");
        EditorApplication.isPlaying = true;
    }

    private Summary GetSummary(int number)
    {
        if (!stats.TryGetValue(number, out Summary s))
        {
            s = new Summary();
            stats[number] = s;
        }
        return s;
    }

    private static string ReportPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..",
            JourneyPlaytestTelemetry.RelativeReport));

    private void Refresh()
    {
        stats.Clear();
        if (!File.Exists(ReportPath)) return;
        foreach (string line in File.ReadLines(ReportPath))
        {
            JourneyPlaytestEvent e;
            try { e = JsonUtility.FromJson<JourneyPlaytestEvent>(line); }
            catch { continue; }
            if (e == null || e.automated || e.level < 1 || e.level > 50) continue;
            Summary summary = GetSummary(e.level);
            if (e.eventType == "Win")
            {
                summary.wins++;
                summary.totalMoves += e.movesUsed;
                summary.totalStars += e.stars;
            }
            else if (e.eventType == "Loss") summary.losses++;
            else if (e.eventType == "Retry") summary.retries++;
            else if (e.eventType == "Abandon") summary.abandons++;
        }
        Repaint();
    }

    private void Export(JourneyLevelCatalog catalog)
    {
        Refresh();
        string path = Path.Combine(Path.GetDirectoryName(ReportPath), "JourneyPlaytestSummary.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (StreamWriter writer = new StreamWriter(path, false))
        {
            writer.WriteLine("Level,Name,MoveLimit,Goal,Wins,Losses,Retries,Abandons,WinRate,AvgWinningMoves,AvgStars");
            for (int i = 1; i <= catalog.Count; i++)
            {
                JourneyLevelDefinition level = catalog.Get(i);
                Summary s = GetSummary(i);
                float winRate = s.Completed > 0 ? (float)s.wins / s.Completed : 0f;
                float avgMoves = s.wins > 0 ? (float)s.totalMoves / s.wins : 0f;
                float avgStars = s.wins > 0 ? (float)s.totalStars / s.wins : 0f;
                string name = level != null ? level.levelName.Replace("\"", "\"\"") : "";
                writer.WriteLine(i + ",\"" + name + "\"," + level.moveLimit + ",\"" +
                    level.goal + "\"," + s.wins + "," + s.losses + "," + s.retries + "," +
                    s.abandons + "," + winRate.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    "," + avgMoves.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                    "," + avgStars.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        Debug.Log("[Journey Playtest] CSV exported: " + path);
        EditorUtility.RevealInFinder(path);
    }

    private static void OpenReportFolder()
    {
        string dir = Path.GetDirectoryName(ReportPath);
        Directory.CreateDirectory(dir);
        EditorUtility.RevealInFinder(dir);
    }
}
