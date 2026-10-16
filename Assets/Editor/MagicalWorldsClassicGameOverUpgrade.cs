using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-destructive upgrade for existing authored Classic prefabs.
/// Creates a game-over return button if one is not already present.
/// </summary>
public static class MagicalWorldsClassicGameOverUpgrade
{
    private const string PopupPrefab = "Assets/MagicalWorlds/Prefabs/Classic/Popup_GameOver.prefab";

    public static void EnsureGameOverMenuButton()
    {
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefab);
        if (original == null || original.transform.Find("Game Over - Main Menu") != null)
        {
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(PopupPrefab);
        try
        {
            RectTransform panel = contents.GetComponent<RectTransform>();
            if (panel == null)
            {
                throw new InvalidOperationException("Game Over prefab has no RectTransform.");
            }

            GameObject go = new GameObject("Game Over - Main Menu", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(panel, false);
            rect.localScale = Vector3.one;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 112f);
            rect.anchoredPosition = new Vector2(0f, -350f);

            Image background = go.GetComponent<Image>();
            background.sprite = Resources.Load<Sprite>("MagicalWorlds/UI/RoundedPanel");
            background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(0.36f, 0.29f, 0.78f, 1f);
            background.raycastTarget = true;

            GameObject label = new GameObject("MAIN MENU", typeof(RectTransform), typeof(Text));
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.localScale = Vector3.one;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "MAIN MENU";
            text.raycastTarget = false;

            if (PrefabUtility.SaveAsPrefabAsset(contents, PopupPrefab) == null)
            {
                throw new InvalidOperationException("Failed saving upgraded Game Over prefab.");
            }
            Debug.Log("[Magical Worlds Classic] Added editable Main Menu button to Game Over prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
