using UnityEngine;

/// <summary>
/// Anchors selected editable UI sections around display cutouts and the usable
/// portrait viewport while preserving the original 1080x1920 layout.
/// Background art, puzzle grid, and result overlays remain full-screen.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MobileSafeAreaDock : MonoBehaviour
{
    public enum Edge { Top, Bottom, Left, Right }

    [SerializeField] private Edge edge = Edge.Top;
    [SerializeField, Range(0f, 1f)] private float influence = 1f;
    [SerializeField, Min(0f)] private float maximumShift = 155f;
    [SerializeField] private bool followViewportEdge = true;
    [SerializeField, Min(1f)] private float referenceHeight = 1920f;

    private RectTransform target;
    private Canvas canvas;
    private Vector2 authoredPosition;
    private Rect previousSafeArea;
    private Vector2Int previousResolution;
    private float previousScale;
    private float previousLogicalHeight;
    private bool initialized;

    public Edge DockEdge => edge;
    public float MaximumShift => maximumShift;

    private void OnEnable()
    {
        target = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        authoredPosition = target.anchoredPosition;
        initialized = true;
        previousResolution = Vector2Int.zero;
        ApplyCurrent();
    }

    private void LateUpdate()
    {
        if (!initialized) return;
        Rect safe = Screen.safeArea;
        Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        float scale = root != null ? Mathf.Max(.0001f, root.scaleFactor) : 1f;
        RectTransform rootRect = root != null ? root.GetComponent<RectTransform>() : null;
        float logicalHeight = rootRect != null ? rootRect.rect.height : referenceHeight;
        if (safe != previousSafeArea || resolution != previousResolution ||
            !Mathf.Approximately(scale, previousScale) ||
            !Mathf.Approximately(logicalHeight, previousLogicalHeight))
        {
            Apply(safe, resolution, scale, logicalHeight);
        }
    }

    private void OnDisable()
    {
        if (initialized && target != null) target.anchoredPosition = authoredPosition;
        initialized = false;
    }

    private void ApplyCurrent()
    {
        if (!initialized) return;
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        float scale = root != null ? Mathf.Max(.0001f, root.scaleFactor) : 1f;
        RectTransform rect = root != null ? root.GetComponent<RectTransform>() : null;
        float height = rect != null ? rect.rect.height : referenceHeight;
        Apply(Screen.safeArea, new Vector2Int(Screen.width, Screen.height), scale, height);
    }

    private void Apply(Rect safe, Vector2Int resolution, float scale, float logicalHeight)
    {
        previousSafeArea = safe;
        previousResolution = resolution;
        previousScale = scale;
        previousLogicalHeight = logicalHeight;
        Vector2 offset = CalculateViewportOffset(edge, safe, resolution, scale,
            maximumShift, logicalHeight, referenceHeight, followViewportEdge);
        target.anchoredPosition = authoredPosition + offset * influence;
    }

    public static Vector2 CalculateViewportOffset(Edge edge, Rect safe, Vector2 resolution,
        float canvasScale, float maxShift, float logicalHeight, float referenceHeight,
        bool followEdge = true)
    {
        float inset = CalculateShift(edge, safe, resolution, canvasScale, maxShift);
        float extra = followEdge && (edge == Edge.Top || edge == Edge.Bottom)
            ? Mathf.Max(0f, (logicalHeight - referenceHeight) * .5f) : 0f;

        switch (edge)
        {
            case Edge.Top: return Vector2.up * (extra - inset);
            case Edge.Bottom: return Vector2.up * (inset - extra);
            case Edge.Left: return Vector2.right * inset;
            default: return Vector2.left * inset;
        }
    }

    public static float CalculateShift(Edge edge, Rect safe, Vector2 resolution,
        float canvasScale, float maxShift)
    {
        if (resolution.x <= 0f || resolution.y <= 0f || canvasScale <= 0f)
            return 0f;

        float inset = 0f;
        switch (edge)
        {
            case Edge.Top: inset = resolution.y - safe.yMax; break;
            case Edge.Bottom: inset = safe.yMin; break;
            case Edge.Left: inset = safe.xMin; break;
            case Edge.Right: inset = resolution.x - safe.xMax; break;
        }
        return Mathf.Clamp(inset / canvasScale, 0f, Mathf.Max(0f, maxShift));
    }
}
