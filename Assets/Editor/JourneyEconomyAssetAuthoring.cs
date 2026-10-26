using MagicalWorlds.Journey;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JourneyEconomyAssetAuthoring
{
    public const string AssetPath =
        "Assets/Resources/MagicalWorlds/Economy/JourneyEconomyConfig.asset";

    static JourneyEconomyAssetAuthoring()
    {
        EditorApplication.delayCall += Ensure;
    }

    [MenuItem("Tools/Magical Worlds/Economy/Ensure Economy Settings")]
    public static void Ensure()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += Ensure;
            return;
        }

        JourneyEconomyConfig existing =
            AssetDatabase.LoadAssetAtPath<JourneyEconomyConfig>(AssetPath);
        if (existing != null)
        {
            // Upgrade the first authored asset once, without overwriting
            // later designer edits to its serialized checkpoint settings.
            string yaml = System.IO.File.ReadAllText(AssetPath);
            if (!yaml.Contains("checkpointBonusCoins:"))
            {
                SerializedObject fields = new SerializedObject(existing);
                fields.FindProperty("checkpointBonusCoins").intValue = 100;
                fields.FindProperty("checkpointEveryLevels").intValue = 10;
                fields.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log("[Economy] Added permanent checkpoint bonus settings to asset.");
            }
            return;
        }
        string folder = "Assets/Resources/MagicalWorlds/Economy";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources/MagicalWorlds", "Economy");
        JourneyEconomyConfig created =
            ScriptableObject.CreateInstance<JourneyEconomyConfig>();
        AssetDatabase.CreateAsset(created, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Economy] Created Inspector-editable Journey reward settings.");
    }
}
