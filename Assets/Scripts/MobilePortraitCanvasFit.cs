using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fits authored portrait layouts to taller or shorter screens without
/// stretching the background artwork or shrinking the puzzle grid unnecessarily.
/// The reference resolution stays 1080x1920 and is fully editable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public sealed class MobilePortraitCanvasFit : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField, Range(1f, 8f)] private float fitResponse = 5f;

    private CanvasScaler scaler;
    private float originalMatch;
    private Vector2Int lastResolution;

    private void OnEnable()
    {
        scaler = GetComponent<CanvasScaler>();
        originalMatch = scaler.matchWidthOrHeight;
        lastResolution = Vector2Int.zero;
        Refresh();
    }

    private void LateUpdate()
    {
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        if (current != lastResolution) Refresh();
    }

    private void OnDisable()
    {
        if (scaler != null) scaler.matchWidthOrHeight = originalMatch;
    }

    private void Refresh()
    {
        lastResolution = new Vector2Int(Screen.width, Screen.height);
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        float match = CalculateMatch(lastResolution, referenceResolution, fitResponse);
        if (!Mathf.Approximately(scaler.matchWidthOrHeight, match))
            scaler.matchWidthOrHeight = match;
    }

    public static float CalculateMatch(Vector2 screenSize, Vector2 reference, float response)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f ||
            reference.x <= 0f || reference.y <= 0f)
            return .5f;

        float normalizedAspect = (screenSize.y / screenSize.x) /
            (reference.y / reference.x);
        return Mathf.Clamp01(.5f - (normalizedAspect - 1f) * Mathf.Max(0f, response));
    }
}
