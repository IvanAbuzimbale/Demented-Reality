using UnityEditor;
using UnityEngine;

/// <summary>
/// Quality of Life improvements for the Unity Hierarchy window:
///   - Automatically displays component icons to the right of the object name
///   - Middle click toggles the object (SetActive)
///   - Visual overlay for disabled objects (dark layer)
/// </summary>
[InitializeOnLoad]
public static class HierarchyQOL
{
    private const float IconSize      = 16f;

    // Overlay colors for disabled state
    private static readonly Color SelfDisabledColor   = new Color(0f, 0f, 0f, 0.40f);
    private static readonly Color ParentDisabledColor = new Color(0f, 0f, 0f, 0.18f);

    // -------------------------------------------------------------------------
    // Constructor is called when the Editor starts (InitializeOnLoad)
    // -------------------------------------------------------------------------
    static HierarchyQOL()
    {
        EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
    }

    // -------------------------------------------------------------------------
    // Callback Unity calls for each row in the Hierarchy window
    // -------------------------------------------------------------------------
    private static void OnHierarchyGUI(EntityId instanceID, Rect rowRect)
    {
        GameObject go = EditorUtility.EntityIdToObject(instanceID) as GameObject;
        if (go == null) return;

        // --- Middle click: toggle object active state ----------------------------
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 2 && rowRect.Contains(e.mousePosition))
        {
            Undo.RecordObject(go, "Toggle GameObject Active");
            go.SetActive(!go.activeSelf);
            e.Use();
            EditorApplication.RepaintHierarchyWindow();
            return;
        }

        // --- Visual overlay for disabled objects ----------------------------
        if (!go.activeSelf)
        {
            // Object is directly disabled (SetActive(false))
            EditorGUI.DrawRect(rowRect, SelfDisabledColor);
        }
        else if (!go.activeInHierarchy)
        {
            // Parent is disabled, so this object is also inactive
            EditorGUI.DrawRect(rowRect, ParentDisabledColor);
        }

        // --- Custom hierarchy icon (overrides Unity's default) -----------------
        DrawCustomIcon(go, rowRect);
    }

    // -------------------------------------------------------------------------
    // If the object has a custom icon set via SetIconForObject, draws it
    // at the position where Unity normally renders the default GameObject icon.
    // -------------------------------------------------------------------------
    private static void DrawCustomIcon(GameObject go, Rect rowRect)
    {
        Texture2D icon = EditorGUIUtility.GetIconForObject(go) as Texture2D;
        if (icon == null) return;

        // Unity normally draws the object icon to the right of the foldout arrow,
        // at rowRect.x (already includes indent). Icon is 16x16, vertically centered.
        float iconY    = rowRect.y + (rowRect.height - IconSize) * 0.5f;
        var   iconRect = new Rect(rowRect.x, iconY, IconSize, IconSize);

        // Dark background to cover Unity's default icon underneath
        EditorGUI.DrawRect(iconRect, new Color(0.22f, 0.22f, 0.22f, 1f));
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
    }
}
