using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-destructive upgrade for existing Classic pause popup prefab.
/// Preserves CONTINUE, HOW TO PLAY and NEW GAME, adding a separate return button.
/// </summary>
public static class MagicalWorldsClassicPauseMenuUpgrade
{
    private const string DialogPrefab = "Assets/MagicalWorlds/Prefabs/Classic/Popup_Dialog.prefab";

    public static void EnsurePauseMenuButton()
    {
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(DialogPrefab);
        if (original == null || original.transform.Find("Dialog Main Menu") != null)
        {
            return;
        }
        GameObject editing = PrefabUtility.LoadPrefabContents(DialogPrefab);
        try
        {
            RectTransform overlay = editing.GetComponent<RectTransform>();
            if (overlay == null)
            {
                throw new InvalidOperationException("Dialog prefab root has no RectTransform.");
            }

            GameObject go = new GameObject("Dialog Main Menu", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(overlay, false);
            rect.localScale = Vector3.one;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(580, 96);
            rect.anchoredPosition = new Vector2(0, -620);

            Image background = go.GetComponent<Image>();
            background.sprite = Resources.Load<Sprite>("MagicalWorlds/UI/RoundedPanel");
            background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(.36f, .29f, .78f, 1f);
            background.raycastTarget = true;

            GameObject textObject = new GameObject("MAIN MENU", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.localScale = Vector3.one;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "MAIN MENU";
            text.raycastTarget = false;

            if (PrefabUtility.SaveAsPrefabAsset(editing, DialogPrefab) == null)
            {
                throw new InvalidOperationException("Could not save upgraded Pause popup prefab.");
            }
            Debug.Log("[Magical Worlds Classic] Added separate MAIN MENU action to Pause popup.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(editing);
        }
    }
}
