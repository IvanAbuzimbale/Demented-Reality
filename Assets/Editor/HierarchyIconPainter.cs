using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Right-click context menu "Create Icon" in the Hierarchy.
/// Opens a pixel-art painter window; on Save, the icon is applied
/// to the selected GameObject via EditorGUIUtility.SetIconForObject
/// and stored as a PNG in Assets/Editor/HierarchyIcons/.
/// </summary>
public class HierarchyIconPainterWindow : EditorWindow
{
    private const int CanvasSize = 16;
    private const int CellSize   = 22;
    private const string IconFolder = "Assets/Editor/HierarchyIcons";

    private Color[]      _pixels     = new Color[CanvasSize * CanvasSize];
    private Color        _brushColor = Color.white;
    private GameObject[] _targets    = new GameObject[0];

    // Icon library
    private enum Tab { Draw, Library }
    private Tab          _tab             = Tab.Draw;
    private Texture2D[]  _libraryIcons;
    private string[]     _libraryPaths;
    private Vector2      _libraryScroll;
    private const int    LibIconSize      = 32;
    private const int    LibIconsPerRow   = 6;

    // When editing an existing library icon, this is the path to overwrite on Save
    private string       _editingPath     = null;

    // Drawing tools
    private enum DrawTool { Pencil, Eraser, Fill, Eyedropper, Line, Rect }
    private DrawTool _activeTool    = DrawTool.Pencil;
    private int      _dragStartPx   = -1;
    private int      _dragStartPy   = -1;
    private Color[]  _pixelSnapshot = null;

    // -------------------------------------------------------------------------
    // Context menu — appears as first item in Hierarchy right-click
    // -------------------------------------------------------------------------
    [MenuItem("GameObject/Create Icon", false, -100)]
    private static void OpenFromContextMenu()
    {
        if (Selection.gameObjects.Length > 0) Open(Selection.gameObjects);
    }

    [MenuItem("GameObject/Create Icon", true)]
    private static bool ValidateOpenFromContextMenu() => Selection.activeGameObject != null;

    // -------------------------------------------------------------------------
    public static void Open(GameObject[] targets)
    {
        var win = GetWindow<HierarchyIconPainterWindow>(true, "Icon Painter");
        win.minSize = new Vector2(CanvasSize * CellSize + 48, CanvasSize * CellSize + 320);
        win.maxSize = new Vector2(600, 800);
        win._targets = targets;

        // Pre-load existing custom icon from the first selected object
        var existing = EditorGUIUtility.GetIconForObject(targets[0]);
        if (existing != null)
        {
            var readable = MakeReadable(existing);
            win._pixels = readable.GetPixels(0, 0, CanvasSize, CanvasSize);
        }
        else
        {
            for (int i = 0; i < win._pixels.Length; i++)
                win._pixels[i] = new Color(0, 0, 0, 0);
        }

        win.RefreshLibrary();
    }

    // -------------------------------------------------------------------------
    private void OnGUI()
    {
        GUILayout.Space(6);
        string label = _targets.Length == 1
            ? $"Object:  {_targets[0].name}"
            : $"Objects:  {_targets.Length} selected";
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        GUILayout.Space(4);

        // ---- Tab bar --------------------------------------------------------
        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Draw", "Library" });
        GUILayout.Space(6);

        if (_tab == Tab.Library)
        {
            DrawLibraryTab();
            return;
        }

        // =====================================================================
        // DRAW TAB
        // =====================================================================

        // ---- Tool selector --------------------------------------------------
        string[] toolLabels = { "Pencil", "Eraser", "Fill", "Pick", "Line", "Rect" };
        _activeTool = (DrawTool)GUILayout.Toolbar((int)_activeTool, toolLabels, GUILayout.Height(22));
        GUILayout.Space(4);

        // ---- Pixel canvas ---------------------------------------------------
        float canvasW    = CanvasSize * CellSize;
        float startX     = (position.width - canvasW) * 0.5f;
        Rect  canvasRect = GUILayoutUtility.GetRect(canvasW, CanvasSize * CellSize);
        canvasRect.x = startX;

        Event e = Event.current;

        // Compute hovered pixel (screen-space; py 0 = top row)
        int hoverPx = -1, hoverPy = -1;
        if (canvasRect.Contains(e.mousePosition))
        {
            hoverPx = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.x - canvasRect.x) / CellSize), 0, CanvasSize - 1);
            hoverPy = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.y - canvasRect.y) / CellSize), 0, CanvasSize - 1);
        }

        // Dispatch tool input BEFORE drawing so line/rect preview is already in _pixels
        if (hoverPx >= 0)
            HandleCanvasEvent(e, hoverPx, hoverPy);

        // Draw cells
        for (int py = 0; py < CanvasSize; py++)
        {
            for (int px = 0; px < CanvasSize; px++)
            {
                int  idx  = (CanvasSize - 1 - py) * CanvasSize + px;
                Rect cell = new(
                    canvasRect.x + px * CellSize,
                    canvasRect.y + py * CellSize,
                    CellSize, CellSize);

                Color display = _pixels[idx];
                if (display.a < 0.01f)
                    display = (px + py) % 2 == 0
                        ? new Color(0.55f, 0.55f, 0.55f)
                        : new Color(0.35f, 0.35f, 0.35f);

                // Hover highlight for Fill / Eyedropper
                if (px == hoverPx && py == hoverPy &&
                    (_activeTool == DrawTool.Fill || _activeTool == DrawTool.Eyedropper))
                    display = Color.Lerp(display, Color.white, 0.35f);

                EditorGUI.DrawRect(cell, display);

                // Grid lines
                EditorGUI.DrawRect(new Rect(cell.xMax - 1, cell.y,        1,          cell.height), new Color(0, 0, 0, 0.18f));
                EditorGUI.DrawRect(new Rect(cell.x,        cell.yMax - 1,  cell.width, 1),           new Color(0, 0, 0, 0.18f));
            }
        }

        GUILayout.Space(10);

        // ---- Brush colour ---------------------------------------------------
        _brushColor = EditorGUILayout.ColorField("Brush Colour", _brushColor);

        // ---- Quick-colour palette -------------------------------------------
        GUILayout.Space(4);
        GUILayout.Label("Quick Colours");
        DrawPalette();

        GUILayout.Space(10);

        // ---- Action buttons -------------------------------------------------
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear", GUILayout.Height(26)))
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = new Color(0, 0, 0, 0);
            Repaint();
        }
        if (GUILayout.Button("Save Icon", GUILayout.Height(26)))
            SaveIcon();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        string hint = _activeTool switch
        {
            DrawTool.Pencil     => "Left-click: paint  |  Right-click: erase",
            DrawTool.Eraser     => "Left / Right-click: erase",
            DrawTool.Fill       => "Left-click: flood fill  |  Right-click: fill transparent",
            DrawTool.Eyedropper => "Left-click a pixel to pick its colour (switches to Pencil)",
            DrawTool.Line       => "Left-click drag: draw line",
            DrawTool.Rect       => "Left-click drag: draw rectangle outline",
            _                   => ""
        };
        EditorGUILayout.HelpBox(hint, MessageType.None);
    }

    // =========================================================================
    // LIBRARY TAB
    // =========================================================================
    private void DrawLibraryTab()
    {
        if (_libraryIcons == null || _libraryIcons.Length == 0)
        {
            EditorGUILayout.HelpBox("No icons saved yet. Draw and save one first.", MessageType.Info);
            if (GUILayout.Button("Refresh")) RefreshLibrary();
            return;
        }

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{_libraryIcons.Length} icon(s) found", EditorStyles.miniLabel);
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshLibrary();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        _libraryScroll = GUILayout.BeginScrollView(_libraryScroll);

        int col = 0;
        GUILayout.BeginHorizontal();

        for (int i = 0; i < _libraryIcons.Length; i++)
        {
            var icon = _libraryIcons[i];
            if (icon == null) continue;

            string name = Path.GetFileNameWithoutExtension(_libraryPaths[i]);
            string path = _libraryPaths[i];

            const int BtnH   = 16;
            const int BtnGap = 2;
            int       totalH = LibIconSize + 4 + BtnH + BtnGap + BtnH + 2;

            Rect r = GUILayoutUtility.GetRect(LibIconSize + 4, totalH,
                        GUILayout.Width(LibIconSize + 4), GUILayout.Height(totalH));

            // Background
            EditorGUI.DrawRect(new Rect(r.x, r.y, LibIconSize + 4, LibIconSize + 4),
                new Color(0.18f, 0.18f, 0.18f));

            // Icon preview
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, LibIconSize, LibIconSize),
                icon, ScaleMode.ScaleToFit, true);

            float btn1Y = r.y + LibIconSize + 4;
            float btn2Y = btn1Y + BtnH + BtnGap;

            // "Use" button
            if (GUI.Button(new Rect(r.x, btn1Y, LibIconSize + 4, BtnH),
                new GUIContent("Use", name), EditorStyles.miniButton))
            {
                ApplyExistingIcon(icon);
            }

            // "Edit" button — loads pixels into canvas, switches to Draw tab
            if (GUI.Button(new Rect(r.x, btn2Y, LibIconSize + 4, BtnH),
                new GUIContent("Edit", name), EditorStyles.miniButton))
            {
                LoadIconForEditing(icon, path);
            }

            col++;
            if (col >= LibIconsPerRow)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                col = 0;
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }

    private void RefreshLibrary()
    {
        if (!Directory.Exists(IconFolder))
        {
            _libraryIcons = new Texture2D[0];
            _libraryPaths = new string[0];
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
        var icons = new System.Collections.Generic.List<Texture2D>();
        var paths = new System.Collections.Generic.List<string>();

        foreach (var guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var    t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t != null) { icons.Add(t); paths.Add(p); }
        }

        _libraryIcons = icons.ToArray();
        _libraryPaths = paths.ToArray();
    }

    private void ApplyExistingIcon(Texture2D icon)
    {
        if (_targets.Length == 0) return;
        foreach (var go in _targets)
        {
            EditorGUIUtility.SetIconForObject(go, icon);
            EditorUtility.SetDirty(go);
        }
        EditorApplication.RepaintHierarchyWindow();
        Debug.Log($"[HierarchyQOL] Applied existing icon to {_targets.Length} object(s)");
        Close();
    }

    private void LoadIconForEditing(Texture2D icon, string path)
    {
        var readable = MakeReadable(icon);
        _pixels      = readable.GetPixels(0, 0, CanvasSize, CanvasSize);
        _editingPath = path;
        _tab         = Tab.Draw;
        Repaint();
    }

    // -------------------------------------------------------------------------
    private static readonly Color[] Palette =
    {
        Color.white,                  Color.black,
        Color.red,                    new(0.85f, 0.25f, 0.25f),
        new(0.2f, 0.75f, 0.2f),       new(0.1f, 0.5f, 0.1f),
        new(0.3f, 0.5f, 1.0f),        new(0.1f, 0.2f, 0.8f),
        Color.yellow,                 new(1f, 0.55f, 0f),
        Color.cyan,                   Color.magenta,
        new(0.6f, 0.3f, 0.1f),        new(0.5f, 0f, 0.5f),
        Color.gray,                   new(0, 0, 0, 0), // transparent
    };

    private void DrawPalette()
    {
        const int s = 22;
        GUILayout.BeginHorizontal();
        foreach (Color c in Palette)
        {
            Rect r = GUILayoutUtility.GetRect(s, s, GUILayout.Width(s), GUILayout.Height(s));

            if (c.a < 0.01f)
            {
                bool even = ((int)(r.x / s) + (int)(r.y / s)) % 2 == 0;
                EditorGUI.DrawRect(r, even ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.35f, 0.35f, 0.35f));
            }
            else
                EditorGUI.DrawRect(r, c);

            // White border when selected
            if (c == _brushColor)
                DrawBorder(r, Color.white, 1);

            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                _brushColor = c;
        }
        GUILayout.EndHorizontal();
    }

    private static void DrawBorder(Rect r, Color c, float t)
    {
        EditorGUI.DrawRect(new Rect(r.x,         r.y,          r.width, t),      c);
        EditorGUI.DrawRect(new Rect(r.x,         r.yMax - t,   r.width, t),      c);
        EditorGUI.DrawRect(new Rect(r.x,         r.y,          t, r.height),     c);
        EditorGUI.DrawRect(new Rect(r.xMax - t,  r.y,          t, r.height),     c);
    }

    // =========================================================================
    // DRAWING TOOL IMPLEMENTATIONS
    // =========================================================================
    private void HandleCanvasEvent(Event e, int screenPx, int screenPy)
    {
        // screenPy is 0 at the top row; texPy is flipped for texture-space indexing
        int texPy    = CanvasSize - 1 - screenPy;
        bool isDown  = e.type == EventType.MouseDown;
        bool isDrag  = e.type == EventType.MouseDrag;
        bool isUp    = e.type == EventType.MouseUp;

        switch (_activeTool)
        {
            case DrawTool.Pencil:
                if ((isDown || isDrag) && e.button == 0) { PixelSet(screenPx, texPy, _brushColor);           e.Use(); Repaint(); }
                if ((isDown || isDrag) && e.button == 1) { PixelSet(screenPx, texPy, new Color(0, 0, 0, 0)); e.Use(); Repaint(); }
                break;

            case DrawTool.Eraser:
                if ((isDown || isDrag) && (e.button == 0 || e.button == 1))
                    { PixelSet(screenPx, texPy, new Color(0, 0, 0, 0)); e.Use(); Repaint(); }
                break;

            case DrawTool.Fill:
                if (isDown && e.button == 0) { FloodFill(screenPx, texPy, _brushColor);           e.Use(); Repaint(); }
                if (isDown && e.button == 1) { FloodFill(screenPx, texPy, new Color(0, 0, 0, 0)); e.Use(); Repaint(); }
                break;

            case DrawTool.Eyedropper:
                if (isDown && e.button == 0)
                {
                    _brushColor = _pixels[texPy * CanvasSize + screenPx];
                    _activeTool = DrawTool.Pencil;
                    e.Use(); Repaint();
                }
                break;

            case DrawTool.Line:
                if (isDown && e.button == 0)
                {
                    _pixelSnapshot = (Color[])_pixels.Clone();
                    _dragStartPx = screenPx; _dragStartPy = texPy;
                    e.Use();
                }
                else if (isDrag && e.button == 0 && _pixelSnapshot != null)
                {
                    System.Array.Copy(_pixelSnapshot, _pixels, _pixels.Length);
                    BresenhamLine(_dragStartPx, _dragStartPy, screenPx, texPy, _brushColor);
                    e.Use(); Repaint();
                }
                else if (isUp && e.button == 0) { _pixelSnapshot = null; }
                break;

            case DrawTool.Rect:
                if (isDown && e.button == 0)
                {
                    _pixelSnapshot = (Color[])_pixels.Clone();
                    _dragStartPx = screenPx; _dragStartPy = texPy;
                    e.Use();
                }
                else if (isDrag && e.button == 0 && _pixelSnapshot != null)
                {
                    System.Array.Copy(_pixelSnapshot, _pixels, _pixels.Length);
                    RectOutline(
                        Mathf.Min(_dragStartPx, screenPx), Mathf.Min(_dragStartPy, texPy),
                        Mathf.Max(_dragStartPx, screenPx), Mathf.Max(_dragStartPy, texPy),
                        _brushColor);
                    e.Use(); Repaint();
                }
                else if (isUp && e.button == 0) { _pixelSnapshot = null; }
                break;
        }
    }

    private void PixelSet(int x, int y, Color c)
    {
        if (x >= 0 && x < CanvasSize && y >= 0 && y < CanvasSize)
            _pixels[y * CanvasSize + x] = c;
    }

    private void BresenhamLine(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            PixelSet(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private void RectOutline(int x0, int y0, int x1, int y1, Color c)
    {
        for (int x = x0; x <= x1; x++) { PixelSet(x, y0, c); PixelSet(x, y1, c); }
        for (int y = y0; y <= y1; y++) { PixelSet(x0, y, c); PixelSet(x1, y, c); }
    }

    private void FloodFill(int startX, int startY, Color newColor)
    {
        Color target = _pixels[startY * CanvasSize + startX];
        if (ColorEq(target, newColor)) return;

        var q = new System.Collections.Generic.Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(startX, startY));

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (p.x < 0 || p.x >= CanvasSize || p.y < 0 || p.y >= CanvasSize) continue;
            if (!ColorEq(_pixels[p.y * CanvasSize + p.x], target)) continue;

            _pixels[p.y * CanvasSize + p.x] = newColor;
            q.Enqueue(new Vector2Int(p.x + 1, p.y));
            q.Enqueue(new Vector2Int(p.x - 1, p.y));
            q.Enqueue(new Vector2Int(p.x, p.y + 1));
            q.Enqueue(new Vector2Int(p.x, p.y - 1));
        }
    }

    private static bool ColorEq(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f &&
        Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;

    // -------------------------------------------------------------------------
    private void SaveIcon()
    {
        if (_targets.Length == 0) { Debug.LogWarning("[HierarchyQOL] No target GameObject."); return; }

        if (!Directory.Exists(IconFolder))
            Directory.CreateDirectory(IconFolder);

        // Build the 16x16 texture from the canvas pixels
        var tex = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Point
        };
        tex.SetPixels(_pixels);
        tex.Apply();

        // If editing an existing library icon, overwrite it; otherwise create new
        string path;
        if (!string.IsNullOrEmpty(_editingPath))
        {
            path = _editingPath;
        }
        else
        {
            string gid  = GlobalObjectId.GetGlobalObjectIdSlow(_targets[0]).ToString();
            string safe = gid.Replace('/', '_').Replace(':', '_');
            path = $"{IconFolder}/{safe}.png";
        }

        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // Configure importer for crisp pixel-art icon
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType        = TextureImporterType.GUI;
            imp.filterMode         = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize     = 32;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        // Apply icon to all selected objects
        foreach (var go in _targets)
        {
            EditorGUIUtility.SetIconForObject(go, saved);
            EditorUtility.SetDirty(go);
        }
        EditorApplication.RepaintHierarchyWindow();

        Debug.Log($"[HierarchyQOL] Icon saved → {path}, applied to {_targets.Length} object(s)");
        _editingPath = null;
        RefreshLibrary();
        Close();
    }

    // -------------------------------------------------------------------------
    // Blits the source texture into a CPU-readable Texture2D of CanvasSize x CanvasSize
    private static Texture2D MakeReadable(Texture src)
    {
        var rt   = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var result = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, Mathf.Min(src.width, CanvasSize), Mathf.Min(src.height, CanvasSize)), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
