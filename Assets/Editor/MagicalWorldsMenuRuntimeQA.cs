using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MagicalWorldsMenuRuntimeQA
{
    private const string Once = "MagicalWorlds.MenuRuntimeQA.OneShot.v2";

    static MagicalWorldsMenuRuntimeQA()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !EditorPrefs.GetBool(Once, false))
            {
                EditorApplication.delayCall += RunTest;
            }
        };
    }

    [MenuItem("Tools/Magical Worlds/Main Menu/Run Runtime Smoke Test")]
    public static void RunTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Menu Runtime QA] Enter Play Mode in MainMenu scene first.");
            return;
        }

        EditorPrefs.SetBool(Once, true);
        try
        {
            var menu = UnityEngine.Object.FindFirstObjectByType<MagicalWorldsMenuController>();
            if (menu == null)
            {
                Debug.LogWarning("[Menu Runtime QA] No Menu Controller in active scene; skipped.");
                return;
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("EventSystem.current is null");
            }

            var raycaster = menu.GetComponentInChildren<GraphicRaycaster>();
            if (raycaster == null)
            {
                throw new InvalidOperationException("Menu Canvas has no GraphicRaycaster");
            }
            var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.208f)
            };
            raycaster.Raycast(pointer, hits);
            string names = "";
            foreach (RaycastResult hit in hits)
            {
                if (names.Length > 0)
                {
                    names += ", ";
                }
                names += hit.gameObject.name;
            }

            Transform journeyTransform = FindDeep(menu.transform, "Button_Journey");
            Vector2 journeyCenter = RectTransformUtility.WorldToScreenPoint(null, journeyTransform.position);
            hits.Clear();
            pointer.position = journeyCenter;
            raycaster.Raycast(pointer, hits);
            string exactHits = hits.Count > 0 ? hits[0].gameObject.name : "NONE";
            Transform popup = FindDeep(menu.transform, "Popup Overlay");
            if (popup == null)
            {
                throw new InvalidOperationException("Popup Overlay missing");
            }

            menu.ShowJourney();
            if (!popup.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Journey button handler did not open popup");
            }
            menu.ClosePopup();
            if (popup.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Popup close did not work");
            }

            Debug.Log("[Menu Runtime QA] PASSED. Journey popup opens and closes; EventSystem and Canvas Raycaster exist. " +
                "Screen: " + Screen.width + "x" + Screen.height +
                "; assumed pointer hits: " + names + "; actual Journey center: " + journeyCenter + "; center raycast first: " + exactHits);
        }
        catch (Exception ex)
        {
            Debug.LogError("[Menu Runtime QA] FAILED: " + ex);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform found = FindDeep(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}
