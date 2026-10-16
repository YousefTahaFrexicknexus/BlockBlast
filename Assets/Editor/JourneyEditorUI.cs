using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class JourneyEditorUI
{
    public static readonly Color Gold = Hex("#FFE4A0");
    private static Sprite rounded;
    private static Font font;

    public static void Setup()
    {
        rounded = Resources.Load<Sprite>("MagicalWorlds/UI/RoundedPanel");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static RectTransform Canvas(Transform parent, string name, int order = 0)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;
        CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        return rect;
    }

    public static void EventSystem()
    {
        GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static RectTransform Group(string name, Transform parent)
    {
        return Rect(name, parent, Vector2.zero, new Vector2(1080,1920));
    }

    public static RectTransform Panel(string name, Transform parent, Vector2 pos, Vector2 size,
        Color color, bool raycast = false)
    {
        RectTransform rect = Rect(name, parent, pos, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = rounded;
        if (rounded != null) image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = raycast;
        return rect;
    }

    public static void Art(string name, Transform parent, Sprite sprite)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, new Vector2(1080,1920));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    public static Text Text(string name, Transform parent, string text, Vector2 pos,
        Vector2 size, int fontSize, Color color)
    {
        RectTransform rect = Rect(name, parent, pos, size);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.text = text;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 20;
        label.resizeTextMaxSize = fontSize;
        return label;
    }

    public static Button Button(string name, Transform parent, string text, Vector2 pos,
        Vector2 size, Color color, int fontSize)
    {
        RectTransform rect = Panel(name, parent, pos, size, color, true);
        Button button = rect.gameObject.AddComponent<Button>();
        Text("Label", rect, text, Vector2.zero, size - new Vector2(12, 8), fontSize, Color.white);
        return button;
    }

    public static void Assign(UnityEngine.Object owner, string property, UnityEngine.Object reference)
    {
        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) throw new InvalidOperationException("Missing serialized field: " + property);
        field.objectReferenceValue = reference;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void AssignArray(UnityEngine.Object owner, string property, JourneyLevelNode[] references)
    {
        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) throw new InvalidOperationException("Missing serialized field: " + property);
        field.arraySize = references.Length;
        for (int i = 0; i < references.Length; i++)
            field.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
