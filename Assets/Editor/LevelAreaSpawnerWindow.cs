using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class LevelAreaSpawnerWindow : EditorWindow
{
    private struct PrefabFootprint
    {
        public Vector3 axis;
        public Vector3 center;
        public float length;
    }

    private struct PerimeterPiece
    {
        public GameObject prefab;
        public PrefabFootprint footprint;
        public float baseScale;
        public float baseLength;
        public float fittedLength;
    }

    private enum SpawnMode
    {
        Rectangle,
        Circle,
        Freehand,
        Brush,
        Line
    }

    private SpawnMode spawnMode = SpawnMode.Rectangle;
    private readonly List<GameObject> prefabs = new List<GameObject>();
    private Transform parent;
    private int objectCount = 10;
    private bool spawnOnPerimeterOnly;
    private bool placeConsecutivelyOnPerimeter;
    private bool placeConsecutivelyOnFreehand;
    private bool tiledPlacement;
    private bool groundPlacement;
    private bool groundOverlapSubstitution = true;
    private bool rectangleGroundTileable;
    private float rectangleGroundTileSize = 1f;
    private bool circleGroundTileable;
    private float circleGroundTileSize = 1f;
    private bool freehandGroundTileable;
    private float freehandGroundTileSize = 1f;
    private Material groundMaterial;
    private float freehandGroundWidth = 1f;
    private float assetSpacing = 1f;
    private float brushRadius = 1f;
    private float brushTiledTolerance = 0f;
    private bool randomizeSpawnRotation;
    private bool randomizeRotationX;
    private bool randomizeRotationY = true;
    private bool randomizeRotationZ;
    private float minScale = 1f;
    private float maxScale = 1f;
    private float tiledScale = 1f;
    private float placementY;
    private bool alignToSurface;
    private LayerMask surfaceMask = ~0;
    private bool randomSeedAuto = true;
    private int randomSeed = 1;
    private int activeRandomSeed;
    private bool hasActiveRandomSeed;
    private bool spawnerEnabled = true;
    private bool eraserEnabled;
    private float eraserRadius = 1f;
    private Vector2 inspectorScrollPosition;
    private Vector2 sceneSidebarPosition = new Vector2(6f, 76f);
    private bool sceneSidebarDragging;
    private Vector2 sceneSidebarDragOffset;
    private const float rectangleSideSnapMultiplier = 1.6f;
    private float groundCellSize = 0.25f;
    private bool rectangleSideSnapLocked;
    private Vector3 rectangleSideSnapStart;
    private Vector3 rectangleSideSnapEnd;
    private Vector3 rectangleSideSnapTangent;
    private float rectangleSideSnapCoveredDistance;

    private class RectangleGroundData : MonoBehaviour
    {
        public List<Rect> rectangles = new List<Rect>();
    }

    private struct CircleGroundShape
    {
        public Vector3 center;
        public float radius;
    }

    private class CircleGroundData : MonoBehaviour
    {
        public List<CircleGroundShape> circles = new List<CircleGroundShape>();
    }

    private class FreehandGroundPathData
    {
        public List<Vector3> points = new List<Vector3>();
        public bool closed;
    }

    private class FreehandGroundData : MonoBehaviour
    {
        public List<FreehandGroundPathData> paths = new List<FreehandGroundPathData>();
    }

    [System.Serializable]
    private struct GroundCell
    {
        public int x;
        public int z;

        public GroundCell(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
    }

    private class GroundRegionData : MonoBehaviour
    {
        public Material sourceMaterial;
        public float tileSize = 1f;
        public List<GroundCell> cells = new List<GroundCell>();
    }

    private bool isDragging;
    private Vector3 dragStart;
    private Vector3 dragEnd;
    private readonly List<Vector3> freehandPoints = new List<Vector3>();
    private bool freehandPathClosed;
    private bool brushTiledGridInitialized;
    private Vector3 brushTiledGridMin;
    private Vector3 brushTiledGridMax;
    private int sceneControlId;
    private Material prefabPreviewMaterial;
    private Texture2D rectangleIcon;
    private Texture2D circleIcon;
    private Texture2D freehandIcon;
    private Texture2D brushIcon;
    private Texture2D lineIcon;
    private Texture2D eraserIcon;

    [MenuItem("Tools/Level Designer/Area Spawner")]
    public static void Open()
    {
        GetWindow<LevelAreaSpawnerWindow>("Area Spawner");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadToolIcons();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        if (prefabPreviewMaterial != null)
        {
            DestroyImmediate(prefabPreviewMaterial);
            prefabPreviewMaterial = null;
        }

        if (groundFallbackMaterial != null)
        {
            DestroyImmediate(groundFallbackMaterial);
            groundFallbackMaterial = null;
        }

    }

    private void OnGUI()
    {
        inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);

        try
        {
            spawnerEnabled = EditorGUILayout.Toggle("Spawner Enabled", spawnerEnabled);
            EditorGUILayout.Space(6);

            DrawSelectedToolTitle();
            DrawSpawnModeSelector();

            if (eraserEnabled)
            {
                DrawEraserOptions();
                return;
            }

            placementY = EditorGUILayout.FloatField("Base Y", placementY);

            if (spawnMode == SpawnMode.Brush)
            {
                EditorGUILayout.HelpBox("Brush mode scatters objects within a stroke width while you draw a freehand path.", MessageType.None);
                tiledPlacement = EditorGUILayout.Toggle("Tiled", tiledPlacement);
                DrawOptionHelp("Tiled brush mode places tile-like prefabs edge to edge. It is useful for filling floors, paths, and broad painted areas.");

                if (tiledPlacement)
                {
                    EditorGUILayout.HelpBox("Tiled brush mode fills the brush area with tile-like prefabs using the selected tile scale.", MessageType.None);
                    brushTiledTolerance = EditorGUILayout.Slider("Tolerance", brushTiledTolerance, 0f, 10f);
                    DrawOptionHelp("Tolerance controls how strict the fill rule is. Lower values only accept cells fully inside the stroke, while higher values allow partial coverage.");
                }
                else
                {
                    brushRadius = Mathf.Max(0.1f, EditorGUILayout.FloatField("Brush Radius", brushRadius));
                    DrawOptionHelp("Brush Radius defines how wide the painted stroke is while you drag the mouse.");
                }

                if (tiledPlacement)
                {
                    brushRadius = GetBrushTiledEffectiveRadius();
                }
            }
            else if (spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Line)
            {
                EditorGUILayout.HelpBox("Consecutive Instancing is useful for drawing continuous fences, walls, barriers, paths, or similar modular objects along a line.", MessageType.None);
                if (spawnMode == SpawnMode.Freehand)
                {
                    groundPlacement = EditorGUILayout.Toggle("Ground", groundPlacement);
                    DrawOptionHelp("Ground mode creates one continuous mesh instead of placing prefab instances. It is useful for floors, platforms, and path surfaces.");
                    if (groundPlacement)
                    {
                        groundMaterial = (Material)EditorGUILayout.ObjectField("Material", groundMaterial, typeof(Material), false);
                        DrawOptionHelp("Material sets the surface used by the generated ground mesh.");
                        groundCellSize = Mathf.Max(0.05f, EditorGUILayout.FloatField("Cell Size", groundCellSize));
                        DrawOptionHelp("Cell Size controls the grid used to build the ground. Lower values make rounded borders smoother, higher values make the mesh cheaper but more stepped.");
                        freehandGroundWidth = Mathf.Max(0.1f, EditorGUILayout.FloatField("Ground Width", freehandGroundWidth));
                        DrawOptionHelp("Ground Width defines the thickness of the freehand surface drawn under your path.");
                        freehandGroundTileable = EditorGUILayout.Toggle("Tile Material", freehandGroundTileable);
                        DrawOptionHelp("Tile Material repeats the material instead of stretching it across the whole mesh.");
                        if (freehandGroundTileable)
                        {
                            freehandGroundTileSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Tile Size", freehandGroundTileSize));
                            DrawOptionHelp("Tile Size controls how large each repeated material tile appears on the mesh.");
                        }
                        placeConsecutivelyOnFreehand = false;
                    }
                    else
                    {
                        placeConsecutivelyOnFreehand = EditorGUILayout.Toggle("Consecutive Instancing", placeConsecutivelyOnFreehand);
                        DrawOptionHelp("Consecutive Instancing places prefabs one after another using their real length so they connect cleanly.");
                    }
                }
                else
                {
                    placeConsecutivelyOnFreehand = EditorGUILayout.Toggle("Consecutive Instancing", placeConsecutivelyOnFreehand);
                    DrawOptionHelp("Consecutive Instancing places prefabs one after another using their real length so they connect cleanly.");
                }
            }
            else if (spawnMode == SpawnMode.Rectangle || spawnMode == SpawnMode.Circle)
            {
                tiledPlacement = EditorGUILayout.Toggle("Tiled", tiledPlacement);
                DrawOptionHelp("Tiled mode places prefabs edge to edge like floor tiles and keeps them aligned by 90 degree steps.");

                if (tiledPlacement)
                {
                    EditorGUILayout.HelpBox("Tiled mode is best for floor-like layouts. It places prefabs edge-to-edge like tiles and uses only 90, 180, or 270 degree rotations.", MessageType.None);
                    groundPlacement = false;
                    spawnOnPerimeterOnly = false;
                    placeConsecutivelyOnPerimeter = false;
                }
                else
                {
                    groundPlacement = EditorGUILayout.Toggle("Ground", groundPlacement);
                    DrawOptionHelp("Ground mode creates one continuous surface mesh instead of spawning individual prefabs.");
                    if (groundPlacement)
                    {
                        groundMaterial = (Material)EditorGUILayout.ObjectField("Material", groundMaterial, typeof(Material), false);
                        DrawOptionHelp("Material sets the surface used by the generated ground mesh.");
                        if (spawnMode == SpawnMode.Rectangle)
                        {
                            groundCellSize = Mathf.Max(0.05f, EditorGUILayout.FloatField("Smoothing", groundCellSize));
                            DrawOptionHelp("Smoothing adjusts the grid used to build the ground. Lower values make borders smoother, higher values make the mesh cheaper but more stepped.");
                        }
                        else
                        {
                            groundCellSize = Mathf.Max(0.05f, EditorGUILayout.FloatField("Smoothing", groundCellSize));
                            DrawOptionHelp("Smoothing adjusts the grid used to build the ground. Lower values make borders smoother, higher values make the mesh cheaper but more stepped.");
                        }
                        if (spawnMode == SpawnMode.Rectangle)
                        {
                            rectangleGroundTileable = EditorGUILayout.Toggle("Tile Material", rectangleGroundTileable);
                            DrawOptionHelp("Tile Material repeats the material instead of stretching it across the rectangle.");
                            if (rectangleGroundTileable)
                            {
                                rectangleGroundTileSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Tile Size", rectangleGroundTileSize));
                                DrawOptionHelp("Tile Size controls how large each repeated material tile appears on the rectangle.");
                            }
                        }
                        else
                        {
                            circleGroundTileable = EditorGUILayout.Toggle("Tile Material", circleGroundTileable);
                            DrawOptionHelp("Tile Material repeats the material instead of stretching it across the circle.");
                            if (circleGroundTileable)
                            {
                                circleGroundTileSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Tile Size", circleGroundTileSize));
                                DrawOptionHelp("Tile Size controls how large each repeated material tile appears on the circle.");
                            }
                        }
                        spawnOnPerimeterOnly = false;
                        placeConsecutivelyOnPerimeter = false;
                    }
                    else
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField("Perimeter", EditorStyles.boldLabel);
                        EditorGUILayout.HelpBox("Perimeter options are useful for placing fences, walls, barriers, or similar modular objects along the shape border.", MessageType.None);
                        spawnOnPerimeterOnly = EditorGUILayout.Toggle("Perimeter Only", spawnOnPerimeterOnly);
                        DrawOptionHelp("Perimeter Only limits placement to the outer border of the shape.");

                        if (spawnOnPerimeterOnly)
                        {
                            placeConsecutivelyOnPerimeter = EditorGUILayout.Toggle("Consecutive Perimeter", placeConsecutivelyOnPerimeter);
                            DrawOptionHelp("Consecutive Perimeter places each prefab immediately after the previous one so the border stays continuous.");

                            if (placeConsecutivelyOnPerimeter)
                            {
                                EditorGUILayout.HelpBox("Consecutive Perimeter uses the prefab's real length, detects its longest axis, and stretches that axis to close each side of the perimeter without gaps.", MessageType.None);
                            }
                        }
                    }
                }
            }

            if (CanUseRandomSpawnRotation() && !IsGroundPlacementMode())
            {
                EditorGUILayout.Space(4);
                randomizeSpawnRotation = EditorGUILayout.Toggle("Random Rotation", randomizeSpawnRotation);
                DrawOptionHelp("Random Rotation applies random orientation to spawned prefabs.");

                if (randomizeSpawnRotation && !IsTiledPlacementMode())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Axes", GUILayout.Width(34f));
                    randomizeRotationX = EditorGUILayout.ToggleLeft("X", randomizeRotationX, GUILayout.Width(28f));
                    randomizeRotationY = EditorGUILayout.ToggleLeft("Y", randomizeRotationY, GUILayout.Width(28f));
                    randomizeRotationZ = EditorGUILayout.ToggleLeft("Z", randomizeRotationZ, GUILayout.Width(28f));
                    EditorGUILayout.EndHorizontal();
                }

                if (IsTiledPlacementMode() && randomizeSpawnRotation)
                {
                    EditorGUILayout.HelpBox("Tiled rotation uses only 90 degree steps so adjacent tiles always line up cleanly.", MessageType.None);
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Spawn", EditorStyles.boldLabel);
            DrawPrefabList();
            parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);

            if (spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Line)
            {
                if (!placeConsecutivelyOnFreehand && !IsGroundPlacementMode())
                {
                    if (spawnMode == SpawnMode.Line)
                    {
                        objectCount = EditorGUILayout.IntSlider("Object Count", objectCount, 1, 500);
                        DrawOptionHelp("Object Count limits how many prefabs can be spawned along the line.");
                    }
                }
            }
            else if (!IsGroundPlacementMode())
            {
                if (!IsConsecutivePerimeterMode() && !IsTiledPlacementMode())
                {
                    objectCount = EditorGUILayout.IntSlider("Object Count", objectCount, 1, 500);
                    DrawOptionHelp("Object Count limits how many prefabs can be spawned inside the shape.");

                    if (spawnMode != SpawnMode.Brush)
                    {
                        assetSpacing = Mathf.Max(0f, EditorGUILayout.FloatField("Asset Spacing", assetSpacing));
                        DrawOptionHelp("Asset Spacing controls how much empty space remains between prefabs.");
                    }
                }
            }

            if (IsGroundPlacementMode())
            {
                EditorGUILayout.HelpBox("Ground mode creates a single mesh object that matches the shape you draw and can use the selected material.", MessageType.None);
            }
            else if (IsConsecutiveFreehandMode())
            {
                EditorGUILayout.HelpBox("Consecutive Instancing uses the prefab's real length and automatically orients it along the drawn line.", MessageType.None);
            }

            if (IsTiledPlacementMode())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Tile Scale", GUILayout.Width(70f));
                tiledScale = Mathf.Max(0.01f, EditorGUILayout.FloatField(tiledScale));
                EditorGUILayout.EndHorizontal();
                DrawOptionHelp("Tile Scale defines the size of each tile-like prefab and also affects how many fit in the area.");
            }
            else if (!IsGroundPlacementMode())
            {
                DrawScaleRange();
            }
            if (!IsGroundPlacementMode())
            {
                EditorGUILayout.BeginVertical();
                randomSeedAuto = EditorGUILayout.Toggle("Auto Seed", randomSeedAuto);
                DrawOptionHelp("Auto Seed chooses a new random seed automatically after each spawn.");
                if (!IsTiledPlacementMode())
                {
                    if (randomSeedAuto)
                    {
                        EditorGUILayout.LabelField("Seed", GetDisplayedSeed().ToString());
                    }
                    else
                    {
                        randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);
            alignToSurface = EditorGUILayout.Toggle("Align To Surface", alignToSurface);
            DrawOptionHelp("Align To Surface projects spawned objects onto the detected surface instead of keeping a flat placement.");
            using (new EditorGUI.DisabledScope(!alignToSurface))
            {
                surfaceMask = LayerMaskField("Surface Mask", surfaceMask);
            }

            EditorGUILayout.HelpBox(
                "In the Scene View: left click and drag. Rectangle and Circle spawn in the area or perimeter; Freehand, Brush, and Line spawn along the drawn path.",
                MessageType.Info);

            if (GUILayout.Button("Clear Preview"))
            {
                isDragging = false;
                freehandPoints.Clear();
                freehandPathClosed = false;
                brushTiledGridInitialized = false;
                hasActiveRandomSeed = false;
                rectangleSideSnapLocked = false;
                SceneView.RepaintAll();
            }
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEraserOptions()
    {
        EditorGUILayout.Space(6);
        eraserRadius = Mathf.Max(0.1f, EditorGUILayout.FloatField("Eraser Radius", eraserRadius));
        EditorGUILayout.HelpBox("Click or drag in the Scene View to erase spawned prefabs inside the eraser radius.", MessageType.Info);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        DrawSceneViewSidebar(sceneView);

        if (!spawnerEnabled && !eraserEnabled)
        {
            if (isDragging)
            {
                isDragging = false;
                freehandPoints.Clear();
                freehandPathClosed = false;
                GUIUtility.hotControl = 0;
            }

            return;
        }

        Event current = Event.current;
        sceneControlId = GUIUtility.GetControlID(FocusType.Passive);

        if (eraserEnabled)
        {
            HandleEraserSceneGUI(sceneView, current);
            return;
        }

        if (!spawnerEnabled)
        {
            return;
        }

        bool isFinishingDrag = current.type == EventType.MouseUp && isDragging;
        bool isContinuingDrag = current.type == EventType.MouseDrag && isDragging;

        if (current.type == EventType.MouseMove && CanDrawHoverPrefabPreview())
        {
            sceneView.Repaint();
        }

        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape && isDragging)
        {
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            brushTiledGridInitialized = false;
            hasActiveRandomSeed = false;
            rectangleSideSnapLocked = false;
            GUIUtility.hotControl = 0;
            current.Use();
            sceneView.Repaint();
            return;
        }

        if (current.alt || (current.button != 0 && !isFinishingDrag && !isContinuingDrag))
        {
            DrawPreview();
            return;
        }

        if (current.type == EventType.MouseDown && TryGetWorldPoint(current.mousePosition, out dragStart))
        {
            BeginRandomSeedSession();
            dragStart = GetConnectionSnappedPoint(dragStart);
            GUIUtility.hotControl = sceneControlId;
            dragEnd = dragStart;
            isDragging = true;
            freehandPoints.Clear();
            freehandPathClosed = false;
            brushTiledGridInitialized = false;

            if (spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Brush)
            {
                AddFreehandPoint(dragStart, true);
                if (spawnMode == SpawnMode.Brush && IsTiledPlacementMode())
                {
                    InitializeBrushTiledGrid(dragStart);
                }
            }

            current.Use();
            DrawPreview();
            return;
        }

        if (current.type == EventType.MouseDrag && isDragging && TryGetWorldPoint(current.mousePosition, out dragEnd))
        {
            dragEnd = GetConnectionSnappedPoint(dragEnd, dragStart);

            if (spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Brush)
            {
                AddFreehandPoint(dragEnd, false);
                if (spawnMode == SpawnMode.Brush && IsTiledPlacementMode())
                {
                    ExpandBrushTiledGrid(dragEnd);
                }
            }

            sceneView.Repaint();
            current.Use();
            DrawPreview();
            return;
        }

        if (current.type == EventType.MouseUp && isDragging)
        {
            if (TryGetWorldPoint(current.mousePosition, out Vector3 releasePoint))
            {
                dragEnd = GetConnectionSnappedPoint(releasePoint, dragStart);

                if (spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Brush)
                {
                    AddFreehandPoint(dragEnd, IsConsecutiveFreehandMode());
                    if (spawnMode == SpawnMode.Brush && IsTiledPlacementMode())
                    {
                        ExpandBrushTiledGrid(dragEnd);
                    }
                }
            }

            SpawnObjects();
            EndRandomSeedSession();

            isDragging = false;
            brushTiledGridInitialized = false;
            rectangleSideSnapLocked = false;
            GUIUtility.hotControl = 0;
            current.Use();
            return;
        }

        switch (current.GetTypeForControl(sceneControlId))
        {
            case EventType.Layout:
                HandleUtility.AddDefaultControl(sceneControlId);
                break;

        }

        DrawBrushHoverPreview();
        DrawHoverPrefabPreview();
        DrawPreview();
    }

    private void DrawSceneViewSidebar(SceneView sceneView)
    {
        Handles.BeginGUI();
        const float buttonSize = 24f;
        const float sidebarWidth = 34f;
        const float buttonGap = 0f;
        const float dividerGap = 0f;
        const float headerHeight = 10f;
        const float topPad = 1f;
        const float sidePad = 5f;

        float sidebarHeight = topPad + headerHeight + 1f + buttonSize + dividerGap + (buttonSize + buttonGap) * 5f + 1f + buttonSize + 1f;
        Rect sidebarRect = new Rect(sceneSidebarPosition.x, sceneSidebarPosition.y, sidebarWidth, sidebarHeight);
        Rect handleRect = new Rect(sidebarRect.x, sidebarRect.y, sidebarRect.width, headerHeight);

        if (HandleSceneSidebarDrag(handleRect, sceneView))
        {
            Handles.EndGUI();
            return;
        }

        GUI.Box(sidebarRect, GUIContent.none, EditorStyles.helpBox);
        EditorGUI.DrawRect(new Rect(sidebarRect.x + 1f, sidebarRect.y + 1f, sidebarRect.width - 2f, sidebarRect.height - 2f), Color.black);
        EditorGUI.DrawRect(new Rect(sidebarRect.x, sidebarRect.y, sidebarRect.width, 1f), new Color(1f, 1f, 1f, 0.12f));
        EditorGUI.DrawRect(new Rect(sidebarRect.x, sidebarRect.yMax - 1f, sidebarRect.width, 1f), new Color(0f, 0f, 0f, 0.5f));

        Handles.color = new Color(1f, 1f, 1f, 0.24f);
        float handleY = sidebarRect.y + 2f;
        Handles.DrawLine(new Vector3(sidebarRect.x + 8f, handleY, 0f), new Vector3(sidebarRect.x + sidebarRect.width - 8f, handleY, 0f));
        Handles.DrawLine(new Vector3(sidebarRect.x + 8f, handleY + 3f, 0f), new Vector3(sidebarRect.x + sidebarRect.width - 8f, handleY + 3f, 0f));

        float y = sidebarRect.y + topPad + headerHeight + 1f;
        Rect statusRect = new Rect(sidebarRect.x + sidePad, y, buttonSize, buttonSize);
        bool nextEnabled = DrawSceneToolbarStatusToggle(statusRect, spawnerEnabled, "Enable or disable the Area Spawner");

        y += buttonSize;
        DrawSceneToolbarDivider(sidebarRect.x + sidePad, ref y, sidebarRect.width - (sidePad * 2f));

        DrawSceneToolbarButton(ref y, sidebarRect.x + sidePad, buttonSize, rectangleIcon, "Rectangle", "Draw a rectangular area.", SpawnMode.Rectangle, sceneView, buttonGap);
        DrawSceneToolbarButton(ref y, sidebarRect.x + sidePad, buttonSize, circleIcon, "Circle", "Draw a circular area.", SpawnMode.Circle, sceneView, buttonGap);
        DrawSceneToolbarButton(ref y, sidebarRect.x + sidePad, buttonSize, freehandIcon, "Freehand", "Draw a freehand path.", SpawnMode.Freehand, sceneView, buttonGap);
        DrawSceneToolbarButton(ref y, sidebarRect.x + sidePad, buttonSize, brushIcon, "Brush", "Draw a brush stroke with thickness.", SpawnMode.Brush, sceneView, buttonGap);
        DrawSceneToolbarButton(ref y, sidebarRect.x + sidePad, buttonSize, lineIcon, "Line", "Draw a straight line.", SpawnMode.Line, sceneView, buttonGap);
        y += 1f;
        DrawSceneToolbarDivider(sidebarRect.x + sidePad, ref y, sidebarRect.width - (sidePad * 2f));
        Rect eraserRect = new Rect(sidebarRect.x + sidePad, y, buttonSize, buttonSize);
        bool nextEraserEnabled = GUI.Toggle(eraserRect, eraserEnabled, GetToolButtonContent(eraserIcon, "Erase", "Erase spawned prefab instances."), EditorStyles.toolbarButton);
        y += buttonSize + buttonGap;

        Handles.EndGUI();

        if (nextEnabled != spawnerEnabled)
        {
            spawnerEnabled = nextEnabled;
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            hasActiveRandomSeed = false;
            rectangleSideSnapLocked = false;
            GUIUtility.hotControl = 0;
            sceneView.Repaint();
            Repaint();
        }

        if (nextEraserEnabled != eraserEnabled)
        {
            eraserEnabled = nextEraserEnabled;
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            hasActiveRandomSeed = false;
            rectangleSideSnapLocked = false;
            GUIUtility.hotControl = 0;
            sceneView.Repaint();
            Repaint();
        }
    }

    private void DrawSceneToolbarDivider(float x, ref float y, float width)
    {
        Rect lineRect = new Rect(x, y, width, 1f);
        EditorGUI.DrawRect(lineRect, new Color(1f, 1f, 1f, 0.12f));
        y += 1f;
    }

    private bool DrawSceneToolbarStatusToggle(Rect rect, bool value, string tooltip)
    {
        bool nextValue = GUI.Toggle(rect, value, GUIContent.none, EditorStyles.toolbarButton);
        Color dotColor = nextValue ? new Color(0.25f, 0.85f, 0.35f, 1f) : new Color(0.9f, 0.25f, 0.2f, 1f);
        Vector3 center = new Vector3(rect.center.x, rect.center.y, 0f);
        Handles.color = dotColor;
        Handles.DrawSolidDisc(center, Vector3.back, Mathf.Min(rect.width, rect.height) * 0.22f);
        Handles.color = new Color(0f, 0f, 0f, 0.35f);
        Handles.DrawWireDisc(center, Vector3.back, Mathf.Min(rect.width, rect.height) * 0.22f);

        if (Event.current.type == EventType.Repaint)
        {
            GUI.Label(rect, new GUIContent(string.Empty, tooltip));
        }

        return nextValue;
    }

    private bool HandleSceneSidebarDrag(Rect handleRect, SceneView sceneView)
    {
        Event current = Event.current;

        if (current.type == EventType.MouseDown && current.button == 0 && handleRect.Contains(current.mousePosition))
        {
            sceneSidebarDragging = true;
            sceneSidebarDragOffset = current.mousePosition - sceneSidebarPosition;
            current.Use();
            sceneView.Repaint();
            return true;
        }

        if (sceneSidebarDragging && current.type == EventType.MouseDrag && current.button == 0)
        {
            sceneSidebarPosition = current.mousePosition - sceneSidebarDragOffset;
            sceneSidebarPosition.x = Mathf.Clamp(sceneSidebarPosition.x, 4f, Mathf.Max(4f, sceneView.position.width - 40f));
            sceneSidebarPosition.y = Mathf.Clamp(sceneSidebarPosition.y, 40f, Mathf.Max(40f, sceneView.position.height - 40f));
            current.Use();
            sceneView.Repaint();
            return true;
        }

        if (sceneSidebarDragging && current.type == EventType.MouseUp)
        {
            sceneSidebarDragging = false;
            current.Use();
            sceneView.Repaint();
            return true;
        }

        return false;
    }

    private void DrawSceneToolbarButton(ref float y, float x, float size, Texture2D icon, string fallbackLabel, string tooltip, SpawnMode mode, SceneView sceneView, float gap, bool forceEraserButton = false)
    {
        Rect buttonRect = new Rect(x, y, size, size);
        bool selected = forceEraserButton ? eraserEnabled : (!eraserEnabled && spawnMode == mode);
        GUIContent content = GetToolButtonContent(icon, fallbackLabel, tooltip);

        bool nextSelected = GUI.Toggle(buttonRect, selected, content, EditorStyles.toolbarButton);
        if (nextSelected != selected)
        {
            if (forceEraserButton)
            {
                eraserEnabled = nextSelected;
            }
            else
            {
                eraserEnabled = false;
                spawnMode = mode;
            }
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            GUIUtility.hotControl = 0;
            sceneView.Repaint();
            Repaint();
        }

        y += size + gap;
    }

    private void HandleEraserSceneGUI(SceneView sceneView, Event current)
    {
        if (isDragging)
        {
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            GUIUtility.hotControl = 0;
        }

        if (current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        if (current.alt || current.button != 0)
        {
            DrawEraserPreview();
            return;
        }

        switch (current.GetTypeForControl(sceneControlId))
        {
            case EventType.Layout:
                HandleUtility.AddDefaultControl(sceneControlId);
                break;
        }

        if ((current.type == EventType.MouseDown || current.type == EventType.MouseDrag) && TryGetWorldPoint(current.mousePosition, out Vector3 erasePoint))
        {
            GUIUtility.hotControl = sceneControlId;
            ErasePrefabInstancesAt(erasePoint);
            current.Use();
            sceneView.Repaint();
            return;
        }

        if (current.type == EventType.MouseUp && GUIUtility.hotControl == sceneControlId)
        {
            GUIUtility.hotControl = 0;
            current.Use();
            return;
        }

        DrawEraserPreview();
    }

    private void DrawEraserPreview()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (!TryGetWorldPoint(Event.current.mousePosition, out Vector3 erasePoint))
        {
            return;
        }

        Handles.color = new Color(1f, 0.25f, 0.15f, 0.18f);
        Handles.DrawSolidDisc(erasePoint, Vector3.up, eraserRadius);
        Handles.color = new Color(1f, 0.25f, 0.15f, 0.9f);
        Handles.DrawWireDisc(erasePoint, Vector3.up, eraserRadius);
    }

    private bool TryGetWorldPoint(Vector2 mousePosition, out Vector3 worldPoint)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, placementY, 0f));

        if (plane.Raycast(ray, out float distance))
        {
            worldPoint = ray.GetPoint(distance);
            worldPoint.y = placementY;
            return true;
        }

        worldPoint = default;
        return false;
    }

    private void DrawPreview()
    {
        if (!isDragging)
        {
            return;
        }

        if (IsGroundPlacementMode())
        {
            DrawGroundPreview();
            return;
        }

        Handles.color = new Color(0.1f, 0.7f, 1f, 0.2f);

        if (spawnMode == SpawnMode.Rectangle)
        {
            if (IsTiledPlacementMode())
            {
                DrawTiledAreaPreview();
                return;
            }

            Vector3 center = GetRectangleCenter();
            Vector3 size = GetRectangleSize();
            Vector3[] corners =
            {
                center + new Vector3(-size.x, 0f, -size.z) * 0.5f,
                center + new Vector3(-size.x, 0f, size.z) * 0.5f,
                center + new Vector3(size.x, 0f, size.z) * 0.5f,
                center + new Vector3(size.x, 0f, -size.z) * 0.5f
            };

            Handles.DrawSolidRectangleWithOutline(corners, Handles.color, Color.cyan);

            if (IsConsecutivePerimeterMode())
            {
                DrawConsecutivePerimeterPrefabPreview();
            }
            else if (CanDrawStandardSpawnPrefabPreview())
            {
                DrawStandardSpawnPrefabPreview();
            }

            return;
        }

        if (spawnMode == SpawnMode.Circle)
        {
            if (IsTiledPlacementMode())
            {
                DrawTiledAreaPreview();
                return;
            }

            float radius = GetCircleRadius();
            Vector3 center = GetCircleCenter();
            Handles.DrawSolidDisc(center, Vector3.up, radius);
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(center, Vector3.up, radius);

            if (IsConsecutivePerimeterMode())
            {
                DrawConsecutivePerimeterPrefabPreview();
            }
            else if (CanDrawStandardSpawnPrefabPreview())
            {
                DrawStandardSpawnPrefabPreview();
            }

            return;
        }

        if (spawnMode == SpawnMode.Line)
        {
            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(4f, dragStart, dragEnd);
            Handles.SphereHandleCap(0, dragStart, Quaternion.identity, 0.15f, EventType.Repaint);
            Handles.SphereHandleCap(0, dragEnd, Quaternion.identity, 0.15f, EventType.Repaint);

            if (IsConsecutiveFreehandMode())
            {
                DrawConsecutiveLineObjects(false);
            }
            else if (CanDrawStandardSpawnPrefabPreview())
            {
                DrawStandardSpawnPrefabPreview();
            }

            return;
        }

        if (spawnMode == SpawnMode.Brush)
        {
            if (IsTiledPlacementMode())
            {
                DrawBrushTiledStrokePreview();
            }
            else
            {
                DrawBrushStrokePreview();
                DrawBrushStrokeSpawnPrefabPreview();
            }

            return;
        }

        if (freehandPoints.Count < 2)
        {
            return;
        }

        Handles.color = Color.cyan;
        Handles.DrawAAPolyLine(4f, freehandPoints.ToArray());

        for (int i = 0; i < freehandPoints.Count; i++)
        {
            Handles.SphereHandleCap(0, freehandPoints[i], Quaternion.identity, 0.15f, EventType.Repaint);
        }

        if (IsConsecutiveFreehandMode())
        {
            DrawConsecutiveFreehandPrefabPreview();
        }
        else if (CanDrawStandardSpawnPrefabPreview())
        {
            DrawStandardSpawnPrefabPreview();
        }
    }

    private bool CanDrawStandardSpawnPrefabPreview()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return false;
        }

        if (IsConsecutivePerimeterMode() || IsConsecutiveFreehandMode())
        {
            return false;
        }

        if ((spawnMode == SpawnMode.Rectangle || spawnMode == SpawnMode.Circle) && spawnOnPerimeterOnly)
        {
            return false;
        }

        if (IsTiledPlacementMode())
        {
            return false;
        }

        if (spawnMode == SpawnMode.Brush)
        {
            return false;
        }

        return true;
    }

    private void DrawStandardSpawnPrefabPreview()
    {
        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());

        int totalObjects = GetSpawnObjectCount();
        List<Vector3> placedPositions = new List<Vector3>();

        for (int i = 0; i < totalObjects; i++)
        {
            if (!TryGetSpawnPosition(i, placedPositions, out Vector3 position, out Vector3 tangent))
            {
                continue;
            }

            GameObject selectedPrefab = GetRandomPrefab();
            float scale = GetRandomScale();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);
            rotation = ApplyRandomSpawnRotation(rotation);

            if (alignToSurface && TryProjectToSurface(position, out RaycastHit hit))
            {
                position = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
                rotation = ApplyRandomSpawnRotation(rotation);
            }

            DrawPrefabPreview(selectedPrefab, position, rotation, Vector3.one * scale);
            placedPositions.Add(position);
        }

        Random.state = randomState;
    }

    private void DrawTiledAreaPreview()
    {
        if (Event.current.type != EventType.Repaint || !IsTiledPlacementMode())
        {
            return;
        }

        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());
        DrawTiledPreviewFrame();
        DrawTiledArea(false);
        Random.state = randomState;
    }

    private void DrawTiledPreviewFrame()
    {
        Handles.color = new Color(0.1f, 0.7f, 1f, 0.18f);

        if (spawnMode == SpawnMode.Rectangle)
        {
            Vector3 center = GetRectangleCenter();
            Vector3 size = GetRectangleSize();
            Vector3[] corners =
            {
                center + new Vector3(-size.x, 0f, -size.z) * 0.5f,
                center + new Vector3(-size.x, 0f, size.z) * 0.5f,
                center + new Vector3(size.x, 0f, size.z) * 0.5f,
                center + new Vector3(size.x, 0f, -size.z) * 0.5f
            };
            Handles.DrawSolidRectangleWithOutline(corners, Handles.color, Color.cyan);
            return;
        }

        float radius = GetCircleRadius();
        Vector3 circleCenter = GetCircleCenter();
        Handles.DrawSolidDisc(circleCenter, Vector3.up, radius);
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(circleCenter, Vector3.up, radius);

        Handles.color = new Color(0.1f, 0.7f, 1f, 0.35f);
        Handles.DrawWireCube(circleCenter, new Vector3(radius * 2f, 0.001f, radius * 2f));
    }

    private Quaternion ApplyRandomSpawnRotation(Quaternion baseRotation)
    {
        if (!randomizeSpawnRotation || !CanUseRandomSpawnRotation())
        {
            return baseRotation;
        }

        float x = randomizeRotationX ? Random.Range(0f, 360f) : 0f;
        float y = randomizeRotationY ? Random.Range(0f, 360f) : 0f;
        float z = randomizeRotationZ ? Random.Range(0f, 360f) : 0f;
        return baseRotation * Quaternion.Euler(x, y, z);
    }

    private bool CanDrawHoverPrefabPreview()
    {
        return !isDragging && !IsGroundPlacementMode() && (CanUseConnectionSnap() || (IsTiledPlacementMode() && spawnMode != SpawnMode.Brush));
    }

    private bool CanUseConnectionSnap()
    {
        return spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Line;
    }

    private bool CanUseRandomSpawnRotation()
    {
        return !eraserEnabled && !IsConsecutivePerimeterMode() && !IsConsecutiveFreehandMode();
    }

    private void DrawHoverPrefabPreview()
    {
        if (Event.current.type != EventType.Repaint || !CanDrawHoverPrefabPreview())
        {
            return;
        }

        if (!TryGetWorldPoint(Event.current.mousePosition, out Vector3 position))
        {
            return;
        }

        GameObject previewPrefab = GetFirstValidPrefab();
        PrefabFootprint footprint = GetPrefabFootprint(previewPrefab);

        if (IsTiledPlacementMode())
        {
            float tiledPreviewScale = Mathf.Max(0.01f, tiledScale);
            Vector3 tiledScaleMultiplier = Vector3.one * tiledPreviewScale;
            Vector2 cellSize = GetTiledCellSize();
            Quaternion tiledRotation = Quaternion.identity;
            Vector3 tiledPivotPosition = GetPivotPositionForBoundsCenter(position, tiledRotation, footprint, tiledScaleMultiplier);
            DrawPrefabPreview(previewPrefab, tiledPivotPosition, tiledRotation, tiledScaleMultiplier);
            Handles.color = new Color(0.1f, 0.7f, 1f, 0.35f);
            Matrix4x4 previousMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            Handles.DrawWireCube(Vector3.zero, new Vector3(cellSize.x, 0.001f, cellSize.y));
            Handles.matrix = previousMatrix;
            return;
        }

        float previewScale = (minScale + maxScale) * 0.5f;
        Vector3 scaleMultiplier = Vector3.one * previewScale;
        bool hasSnapPoint = TryFindNearestConnectionPoint(position, out Vector3 snapPoint);
        Vector3 connectionPoint = hasSnapPoint ? snapPoint : position;
        Quaternion rotation = GetHoverPreviewRotation(Vector3.up);

        if (alignToSurface && TryProjectToSurface(connectionPoint, out RaycastHit hit))
        {
            connectionPoint = hit.point;
            rotation = GetHoverPreviewRotation(hit.normal);
        }

        Vector3 boundsCenter = hasSnapPoint
            ? GetBoundsCenterFromStartConnectionPoint(connectionPoint, rotation, footprint, scaleMultiplier)
            : connectionPoint;
        Vector3 pivotPosition = GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, scaleMultiplier);
        DrawPrefabPreview(previewPrefab, pivotPosition, rotation, scaleMultiplier);

        if (hasSnapPoint)
        {
            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0, snapPoint, Quaternion.identity, GetConnectionSnapRadius() * 0.25f, EventType.Repaint);
        }
    }

    private void DrawBrushHoverPreview()
    {
        if (spawnMode != SpawnMode.Brush || isDragging || Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (!TryGetWorldPoint(Event.current.mousePosition, out Vector3 position))
        {
            return;
        }

        if (IsTiledPlacementMode())
        {
            DrawBrushTiledHoverPreview(position);
            return;
        }

        Handles.color = new Color(0.1f, 0.7f, 1f, 0.18f);
        Handles.DrawSolidDisc(position, Vector3.up, brushRadius);
        Handles.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        Handles.DrawWireDisc(position, Vector3.up, brushRadius);

        DrawBrushSpawnPrefabPreview(position);
    }

    private void DrawBrushStrokePreview()
    {
        if (freehandPoints.Count == 0)
        {
            return;
        }

        List<Vector3> strokePoints = GetBrushStrokePreviewPoints();

        if (strokePoints.Count == 0)
        {
            return;
        }

        Handles.color = new Color(0.1f, 0.7f, 1f, 0.08f);

        for (int i = 0; i < strokePoints.Count; i++)
        {
            Handles.DrawSolidDisc(strokePoints[i], Vector3.up, brushRadius);
        }
    }

    private List<Vector3> GetBrushStrokePreviewPoints()
    {
        List<Vector3> points = new List<Vector3>();

        if (freehandPoints.Count == 0)
        {
            return points;
        }

        points.Add(freehandPoints[0]);

        if (freehandPoints.Count == 1)
        {
            return points;
        }

        float step = Mathf.Max(0.1f, brushRadius * 0.5f);

        for (int i = 0; i < freehandPoints.Count - 1; i++)
        {
            Vector3 start = freehandPoints[i];
            Vector3 end = freehandPoints[i + 1];
            float distance = Vector3.Distance(start, end);

            if (distance <= Mathf.Epsilon)
            {
                continue;
            }

            int samples = Mathf.Max(1, Mathf.CeilToInt(distance / step));

            for (int sample = 1; sample <= samples; sample++)
            {
                float t = (float)sample / samples;
                points.Add(Vector3.Lerp(start, end, t));
            }
        }

        return points;
    }

    private void DrawBrushSpawnPrefabPreview(Vector3 center)
    {
        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());

        int totalObjects = Mathf.Max(1, GetBrushSpawnObjectCount());
        List<Vector3> placedPositions = new List<Vector3>();

        for (int i = 0; i < totalObjects; i++)
        {
            if (!TryGetBrushHoverSpawnPosition(center, i, placedPositions, out Vector3 position, out Vector3 tangent))
            {
                continue;
            }

            GameObject selectedPrefab = GetRandomPrefab();
            float scale = GetRandomScale();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);
            rotation = ApplyRandomSpawnRotation(rotation);

            if (alignToSurface && TryProjectToSurface(position, out RaycastHit hit))
            {
                position = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
                rotation = ApplyRandomSpawnRotation(rotation);
            }

            DrawPrefabPreview(selectedPrefab, position, rotation, Vector3.one * scale);
            placedPositions.Add(position);
        }

        Random.state = randomState;
    }

    private void DrawBrushStrokeSpawnPrefabPreview()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        List<Vector3> strokePoints = GetBrushStrokePreviewPoints();

        if (strokePoints.Count == 0)
        {
            return;
        }

        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());

        List<Vector3> placedPositions = new List<Vector3>();
        int objectsPerPoint = Mathf.Max(1, objectCount);

        for (int p = 0; p < strokePoints.Count; p++)
        {
            for (int i = 0; i < objectsPerPoint; i++)
            {
                if (!TryGetBrushPointSpawnPosition(strokePoints[p], placedPositions, out Vector3 position, out Vector3 tangent))
                {
                    continue;
                }

                GameObject selectedPrefab = GetRandomPrefab();
                float scale = GetRandomScale();
                PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
                Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);
                rotation = ApplyRandomSpawnRotation(rotation);

                if (alignToSurface && TryProjectToSurface(position, out RaycastHit hit))
                {
                    position = hit.point;
                    rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
                    rotation = ApplyRandomSpawnRotation(rotation);
                }

                DrawPrefabPreview(selectedPrefab, position, rotation, Vector3.one * scale);
                placedPositions.Add(position);
            }
        }

        Random.state = randomState;
    }

    private void DrawBrushTiledHoverPreview(Vector3 center)
    {
        float radius = GetBrushTiledEffectiveRadius();
        Handles.color = new Color(0.1f, 0.7f, 1f, 0.18f);
        Handles.DrawSolidDisc(center, Vector3.up, radius);
        Handles.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        Handles.DrawWireDisc(center, Vector3.up, radius);
    }

    private void DrawBrushTiledStrokePreview()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (!brushTiledGridInitialized)
        {
            return;
        }

        Vector2 cellSize = GetBrushTiledCellSize();
        List<Vector3> strokePoints = GetBrushStrokePreviewPoints();
        float radius = GetBrushTiledEffectiveRadius();
        for (int i = 0; i < strokePoints.Count; i++)
        {
            Handles.color = new Color(0.1f, 0.7f, 1f, 0.08f);
            Handles.DrawSolidDisc(strokePoints[i], Vector3.up, radius);
        }

        HashSet<Vector2Int> occupiedCells = BuildBrushTiledOccupiedCells(cellSize);
        List<Vector2Int> strokeCells = CollectBrushTiledStrokeCells(cellSize);
        DrawBrushTiledPlacementPreview(cellSize, strokeCells, occupiedCells);
    }

    private void SpawnBrushTiledObjects()
    {
        Vector2 cellSize = GetBrushTiledCellSize();
        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            return;
        }

        Random.InitState(GetCurrentSeed());

        List<Vector2Int> strokeCells = CollectBrushTiledStrokeCells(cellSize);
        if (strokeCells.Count == 0)
        {
            return;
        }

        HashSet<Vector2Int> occupiedCells = BuildBrushTiledOccupiedCells(cellSize);
        DrawBrushTiledPlacementObjects(cellSize, strokeCells, occupiedCells, true);
    }

    private void DrawBrushTiledGrid(Vector3 min, Vector3 max, Vector2 cellSize, HashSet<Vector2Int> occupiedCells)
    {
        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            return;
        }

        Handles.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        for (float x = min.x; x < max.x - Mathf.Epsilon; x += cellSize.x)
        {
            for (float z = min.z; z < max.z - Mathf.Epsilon; z += cellSize.y)
            {
                Vector3 tileCenter = new Vector3(x + cellSize.x * 0.5f, placementY, z + cellSize.y * 0.5f);
                Vector2Int cellKey = GetBrushTiledCellKey(tileCenter, cellSize);
                if (occupiedCells != null && !occupiedCells.Add(cellKey))
                {
                    continue;
                }

                Matrix4x4 previousMatrix = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(tileCenter, Quaternion.identity, new Vector3(cellSize.x, 0.001f, cellSize.y));
                Handles.DrawWireCube(Vector3.zero, Vector3.one);
                Handles.matrix = previousMatrix;
            }
        }
    }

    private HashSet<Vector2Int> BuildBrushTiledOccupiedCells(Vector2 cellSize)
    {
        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            return occupiedCells;
        }

        GameObject[] existingRoots = GetErasableObjectRoots();
        for (int i = 0; i < existingRoots.Length; i++)
        {
            GameObject root = existingRoots[i];

            if (root == null)
            {
                continue;
            }

            occupiedCells.Add(GetBrushTiledCellKey(root.transform.position, cellSize));
        }

        return occupiedCells;
    }

    private List<Vector2Int> CollectBrushTiledStrokeCells(Vector2 cellSize)
    {
        List<Vector2Int> orderedCells = new List<Vector2Int>();

        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            return orderedCells;
        }

        if (!brushTiledGridInitialized)
        {
            Vector2Int fallbackCell = GetBrushTiledCellKey(dragStart, cellSize);
            if (IsBrushTiledCellInsideStroke(fallbackCell, cellSize))
            {
                orderedCells.Add(fallbackCell);
            }

            return orderedCells;
        }

        HashSet<Vector2Int> seenCells = new HashSet<Vector2Int>();
        int minX = Mathf.FloorToInt(brushTiledGridMin.x / cellSize.x);
        int maxX = Mathf.CeilToInt(brushTiledGridMax.x / cellSize.x) - 1;
        int minZ = Mathf.FloorToInt(brushTiledGridMin.z / cellSize.y);
        int maxZ = Mathf.CeilToInt(brushTiledGridMax.z / cellSize.y) - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector2Int cellKey = new Vector2Int(x, z);
                if (seenCells.Contains(cellKey))
                {
                    continue;
                }

                if (!IsBrushTiledCellInsideStroke(cellKey, cellSize))
                {
                    continue;
                }

                seenCells.Add(cellKey);
                orderedCells.Add(cellKey);
            }
        }

        return orderedCells;
    }

    private void DrawBrushTiledPlacementPreview(Vector2 cellSize, List<Vector2Int> strokeCells, HashSet<Vector2Int> occupiedCells)
    {
        DrawBrushTiledPlacementObjects(cellSize, strokeCells, occupiedCells, false);
    }

    private void DrawBrushTiledPlacementObjects(Vector2 cellSize, List<Vector2Int> strokeCells, HashSet<Vector2Int> occupiedCells, bool instantiate)
    {
        if (strokeCells == null || strokeCells.Count == 0)
        {
            return;
        }

        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());

        for (int i = 0; i < strokeCells.Count; i++)
        {
            Vector2Int cellKey = strokeCells[i];
            if (occupiedCells.Contains(cellKey))
            {
                continue;
            }

            GameObject selectedPrefab = GetRandomPrefab();
            float scale = GetRandomScale();
            Vector3 scaleMultiplier = Vector3.one * scale;
            Quaternion rotation = GetTiledRotation();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            Vector3 boundsCenter = GetBrushTiledCellCenter(cellKey, cellSize);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, scaleMultiplier);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rotation;
                position = GetPivotPositionForBoundsCenter(hit.point, rotation, footprint, scaleMultiplier);
            }

            if (instantiate)
            {
                CreateSpawnedObject(selectedPrefab, position, rotation, scaleMultiplier);
            }
            else
            {
                DrawPrefabPreview(selectedPrefab, position, rotation, scaleMultiplier);
            }

            occupiedCells.Add(cellKey);
        }

        Random.state = randomState;
    }

    private bool IsBrushTiledCellInsideStroke(Vector2Int cellKey, Vector2 cellSize)
    {
        float radius = GetBrushTiledEffectiveRadius();
        if (brushTiledTolerance <= Mathf.Epsilon)
        {
            return IsBrushTiledCellFullyInsideStroke(cellKey, cellSize, radius);
        }

        float requiredFraction = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(brushTiledTolerance / 10f));
        const int samplesPerAxis = 5;
        int insideCount = 0;
        int totalSamples = samplesPerAxis * samplesPerAxis;

        for (int x = 0; x < samplesPerAxis; x++)
        {
            for (int z = 0; z < samplesPerAxis; z++)
            {
                float sampleX = (cellKey.x + (x + 0.5f) / samplesPerAxis) * cellSize.x;
                float sampleZ = (cellKey.y + (z + 0.5f) / samplesPerAxis) * cellSize.y;
                Vector3 samplePoint = new Vector3(sampleX, placementY, sampleZ);

                if (GetDistanceToBrushStroke(samplePoint) <= radius)
                {
                    insideCount++;
                }
            }
        }

        return (float)insideCount / totalSamples >= requiredFraction;
    }

    private bool IsBrushTiledCellFullyInsideStroke(Vector2Int cellKey, Vector2 cellSize, float radius)
    {
        Vector3 min = new Vector3(cellKey.x * cellSize.x, placementY, cellKey.y * cellSize.y);
        Vector3 max = new Vector3(min.x + cellSize.x, placementY, min.z + cellSize.y);

        Vector3[] corners =
        {
            min,
            new Vector3(max.x, placementY, min.z),
            max,
            new Vector3(min.x, placementY, max.z)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            if (GetDistanceToBrushStroke(corners[i]) > radius)
            {
                return false;
            }
        }

        return true;
    }

    private float GetDistanceToBrushStroke(Vector3 point)
    {
        if (freehandPoints.Count == 0)
        {
            Vector3 delta = point - dragStart;
            delta.y = 0f;
            return delta.magnitude;
        }

        if (freehandPoints.Count == 1)
        {
            Vector3 delta = point - freehandPoints[0];
            delta.y = 0f;
            return delta.magnitude;
        }

        float best = float.MaxValue;
        for (int i = 0; i < freehandPoints.Count - 1; i++)
        {
            Vector3 closestPoint = GetClosestPointOnSegment(point, freehandPoints[i], freehandPoints[i + 1]);
            Vector3 delta = point - closestPoint;
            delta.y = 0f;
            best = Mathf.Min(best, delta.sqrMagnitude);
        }

        return Mathf.Sqrt(best);
    }

    private Vector3 GetBrushTiledCellCenter(Vector2Int cellKey, Vector2 cellSize)
    {
        return new Vector3(
            (cellKey.x + 0.5f) * cellSize.x,
            placementY,
            (cellKey.y + 0.5f) * cellSize.y);
    }

    private Vector2Int GetBrushTiledCellKey(Vector3 position, Vector2 cellSize)
    {
        int xKey = Mathf.FloorToInt(position.x / Mathf.Max(0.01f, cellSize.x));
        int zKey = Mathf.FloorToInt(position.z / Mathf.Max(0.01f, cellSize.y));
        return new Vector2Int(xKey, zKey);
    }

    private void InitializeBrushTiledGrid(Vector3 point)
    {
        Vector2 cellSize = GetBrushTiledCellSize();
        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            brushTiledGridInitialized = false;
            return;
        }

        brushTiledGridInitialized = true;
        SetBrushTiledGridBounds(point, cellSize, true);
    }

    private void ExpandBrushTiledGrid(Vector3 point)
    {
        Vector2 cellSize = GetBrushTiledCellSize();
        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            return;
        }

        if (!brushTiledGridInitialized)
        {
            SetBrushTiledGridBounds(point, cellSize, true);
            brushTiledGridInitialized = true;
            return;
        }

        SetBrushTiledGridBounds(point, cellSize, false);
    }

    private Vector2 GetBrushTiledCellSize()
    {
        GameObject prefab = GetFirstValidPrefab();
        if (prefab != null && TryGetPrefabLocalBounds(prefab, out Bounds bounds))
        {
            float scale = Mathf.Max(0.01f, tiledScale);
            float width = Mathf.Max(0.01f, bounds.size.x * scale);
            float depth = Mathf.Max(0.01f, bounds.size.z * scale);
            if (randomizeSpawnRotation)
            {
                float maxSize = Mathf.Max(width, depth);
                return new Vector2(maxSize, maxSize);
            }

            return new Vector2(width, depth);
        }

        float fallback = Mathf.Max(0.1f, brushRadius * 0.5f);
        return new Vector2(fallback, fallback);
    }

    private void SetBrushTiledGridBounds(Vector3 point, Vector2 cellSize, bool forceReset)
    {
        const int paddingCells = 2;
        float radius = GetBrushTiledEffectiveRadius();

        Vector3 strokeMin = new Vector3(point.x - radius, placementY, point.z - radius);
        Vector3 strokeMax = new Vector3(point.x + radius, placementY, point.z + radius);

        Vector3 paddedMin = new Vector3(
            Mathf.Floor(strokeMin.x / cellSize.x) * cellSize.x - paddingCells * cellSize.x,
            placementY,
            Mathf.Floor(strokeMin.z / cellSize.y) * cellSize.y - paddingCells * cellSize.y);

        Vector3 paddedMax = new Vector3(
            Mathf.Ceil(strokeMax.x / cellSize.x) * cellSize.x + paddingCells * cellSize.x,
            placementY,
            Mathf.Ceil(strokeMax.z / cellSize.y) * cellSize.y + paddingCells * cellSize.y);

        if (forceReset || !brushTiledGridInitialized)
        {
            brushTiledGridMin = paddedMin;
            brushTiledGridMax = paddedMax;
            return;
        }

        brushTiledGridMin = new Vector3(
            Mathf.Min(brushTiledGridMin.x, paddedMin.x),
            placementY,
            Mathf.Min(brushTiledGridMin.z, paddedMin.z));

        brushTiledGridMax = new Vector3(
            Mathf.Max(brushTiledGridMax.x, paddedMax.x),
            placementY,
            Mathf.Max(brushTiledGridMax.z, paddedMax.z));
    }

    private float GetBrushTiledEffectiveRadius()
    {
        return Mathf.Max(0.1f, tiledScale * 2f);
    }

    private bool TryGetBrushHoverSpawnPosition(Vector3 center, int index, List<Vector3> placedPositions, out Vector3 position, out Vector3 tangent)
    {
        position = default;
        tangent = Vector3.forward;

        int attempts = 100;
        float spacing = GetBrushPlacementSpacing();
        float spacingSqr = spacing * spacing;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * brushRadius;
            position = new Vector3(center.x + offset.x, placementY, center.z + offset.y);

            Vector3 deltaFromCenter = position - center;
            deltaFromCenter.y = 0f;
            if (deltaFromCenter.sqrMagnitude > brushRadius * brushRadius)
            {
                continue;
            }

            bool spacedOk = true;
            if (spacing > 0f)
            {
                for (int p = 0; p < placedPositions.Count; p++)
                {
                    Vector3 delta = position - placedPositions[p];
                    delta.y = 0f;
                    if (delta.sqrMagnitude < spacingSqr)
                    {
                        spacedOk = false;
                        break;
                    }
                }
            }

            if (!spacedOk)
            {
                continue;
            }

            tangent = Vector3.forward;
            return true;
        }

        position = center;
        return true;
    }

    private bool TryGetBrushPointSpawnPosition(Vector3 center, List<Vector3> placedPositions, out Vector3 position, out Vector3 tangent)
    {
        position = default;
        tangent = Vector3.forward;

        int attempts = 100;
        float spacing = GetBrushPlacementSpacing();
        float spacingSqr = spacing * spacing;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * brushRadius;
            position = new Vector3(center.x + offset.x, placementY, center.z + offset.y);

            Vector3 deltaFromCenter = position - center;
            deltaFromCenter.y = 0f;
            if (deltaFromCenter.sqrMagnitude > brushRadius * brushRadius)
            {
                continue;
            }

            bool spacedOk = true;
            if (spacing > 0f)
            {
                for (int p = 0; p < placedPositions.Count; p++)
                {
                    Vector3 delta = position - placedPositions[p];
                    delta.y = 0f;
                    if (delta.sqrMagnitude < spacingSqr)
                    {
                        spacedOk = false;
                        break;
                    }
                }
            }

            if (!spacedOk)
            {
                continue;
            }

            tangent = Vector3.forward;
            return true;
        }

        position = center;
        return true;
    }

    private Vector3 GetBrushStrokePointForIndex(List<Vector3> strokePoints, int index, int totalObjects)
    {
        if (strokePoints == null || strokePoints.Count == 0)
        {
            return dragStart;
        }

        if (strokePoints.Count == 1 || totalObjects <= 1)
        {
            return strokePoints[0];
        }

        float t = (float)index / Mathf.Max(1, totalObjects - 1);
        float scaledIndex = t * (strokePoints.Count - 1);
        int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(scaledIndex), 0, strokePoints.Count - 2);
        float segmentT = scaledIndex - segmentIndex;
        return Vector3.Lerp(strokePoints[segmentIndex], strokePoints[segmentIndex + 1], segmentT);
    }

    private Quaternion GetHoverPreviewRotation(Vector3 up)
    {
        SceneView sceneView = SceneView.currentDrawingSceneView;
        Camera sceneCamera = sceneView != null ? sceneView.camera : null;

        if (sceneCamera == null)
        {
            return Quaternion.identity;
        }

        Vector3 flatDirection = Vector3.ProjectOnPlane(-sceneCamera.transform.forward, up);

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.FromToRotation(Vector3.up, up);
        }

        return Quaternion.LookRotation(flatDirection.normalized, up);
    }

    private Vector3 GetBoundsCenterFromStartConnectionPoint(Vector3 connectionPoint, Quaternion rotation, PrefabFootprint footprint, Vector3 scaleMultiplier)
    {
        Vector3 localHalfLength = footprint.axis.normalized * footprint.length * 0.5f;
        return connectionPoint + rotation * Vector3.Scale(localHalfLength, scaleMultiplier);
    }

    private void DrawConsecutivePerimeterPrefabPreview()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (!IsConsecutivePerimeterMode())
        {
            return;
        }

        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());

        if (spawnMode == SpawnMode.Rectangle)
        {
            DrawConsecutiveRectanglePerimeterPrefabPreview();
        }
        else if (spawnMode == SpawnMode.Circle)
        {
            DrawConsecutiveCirclePerimeterPrefabPreview();
        }

        Random.state = randomState;
    }

    private void DrawConsecutiveRectanglePerimeterPrefabPreview()
    {
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        Vector3 bottomLeft = new Vector3(min.x, placementY, min.z);
        Vector3 bottomRight = new Vector3(max.x, placementY, min.z);
        Vector3 topRight = new Vector3(max.x, placementY, max.z);
        Vector3 topLeft = new Vector3(min.x, placementY, max.z);

        DrawConsecutiveRectangleSidePrefabPreview(bottomLeft, bottomRight);
        DrawConsecutiveRectangleSidePrefabPreview(bottomRight, topRight);
        DrawConsecutiveRectangleSidePrefabPreview(topRight, topLeft);
        DrawConsecutiveRectangleSidePrefabPreview(topLeft, bottomLeft);
    }

    private void DrawConsecutiveRectangleSidePrefabPreview(Vector3 start, Vector3 end)
    {
        if (!TryGetUncoveredRectangleSide(start, end, out start, out end, out float coveredDistance, out Vector3 tangent))
        {
            return;
        }

        Vector3 side = end - start;
        float sideLength = side.magnitude;

        if (sideLength <= Mathf.Epsilon)
        {
            return;
        }

        tangent = side / sideLength;
        DrawRectangleSideSnapGuide(start, tangent, coveredDistance, sideLength);

        List<PerimeterPiece> pieces = BuildPiecesForSide(sideLength);
        float distance = 0f;

        for (int i = 0; i < pieces.Count; i++)
        {
            PerimeterPiece piece = pieces[i];
            Vector3 boundsCenter = start + tangent * (distance + piece.fittedLength * 0.5f);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, piece.footprint.axis);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                boundsCenter = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, piece.footprint.axis);
            }

            Vector3 scaleMultiplier = GetFittedScaleMultiplier(piece);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, piece.footprint, scaleMultiplier);
            DrawPrefabPreview(piece.prefab, position, rotation, scaleMultiplier);
            distance += piece.fittedLength;
        }
    }

    private void DrawConsecutiveCirclePerimeterPrefabPreview()
    {
        float perimeter = GetPerimeterLength();

        if (perimeter <= Mathf.Epsilon)
        {
            return;
        }

        float distance = 0f;
        int guard = 0;
        const int maxObjects = 10000;

        while (distance < perimeter && guard < maxObjects)
        {
            GameObject selectedPrefab = GetRandomPrefab();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            float scale = GetRandomScale();
            float scaledLength = Mathf.Max(0.01f, footprint.length * scale);
            float remainingLength = perimeter - distance;

            if (remainingLength <= Mathf.Epsilon)
            {
                break;
            }

            float fittedLength = Mathf.Min(scaledLength, remainingLength);
            Vector3 boundsCenter = GetConsecutivePerimeterPoint(distance + fittedLength * 0.5f, out Vector3 tangent);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                boundsCenter = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
            }

            Vector3 scaleMultiplier = GetLengthAdjustedScaleMultiplier(footprint, scale, scaledLength, fittedLength);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, scaleMultiplier);
            DrawPrefabPreview(selectedPrefab, position, rotation, scaleMultiplier);
            distance += fittedLength;
            guard++;
        }
    }

    private void DrawConsecutiveFreehandPrefabPreview()
    {
        if (Event.current.type != EventType.Repaint || !IsConsecutiveFreehandMode())
        {
            return;
        }

        Random.State randomState = Random.state;
        Random.InitState(GetCurrentSeed());
        DrawConsecutiveFreehandObjects(false);
        Random.state = randomState;
    }

    private int GetCurrentSeed()
    {
        return hasActiveRandomSeed ? activeRandomSeed : randomSeed;
    }

    private int GetDisplayedSeed()
    {
        return GetCurrentSeed();
    }

    private void BeginRandomSeedSession()
    {
        activeRandomSeed = randomSeedAuto ? GenerateRandomSeed() : randomSeed;
        hasActiveRandomSeed = true;
    }

    private void EndRandomSeedSession()
    {
        if (randomSeedAuto)
        {
            randomSeed = GenerateRandomSeed();
        }

        hasActiveRandomSeed = false;
    }

    private int GenerateRandomSeed()
    {
        unchecked
        {
            return System.Environment.TickCount ^ (int)System.DateTime.UtcNow.Ticks;
        }
    }

    private void DrawPrefabPreview(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scaleMultiplier)
    {
        if (prefab == null)
        {
            DrawFallbackPreview(position, rotation, scaleMultiplier);
            return;
        }

        Material previewMaterial = GetPrefabPreviewMaterial();
        previewMaterial.SetPass(0);

        Matrix4x4 rootMatrix = Matrix4x4.TRS(position, rotation, scaleMultiplier);
        Transform prefabRoot = prefab.transform;

        MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

            if (meshFilter.sharedMesh == null || meshRenderer == null || !meshRenderer.enabled)
            {
                continue;
            }

            Matrix4x4 localMatrix = prefabRoot.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            Graphics.DrawMeshNow(meshFilter.sharedMesh, rootMatrix * localMatrix);
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];

            if (skinnedMeshRenderer.sharedMesh == null || !skinnedMeshRenderer.enabled)
            {
                continue;
            }

            Matrix4x4 localMatrix = prefabRoot.worldToLocalMatrix * skinnedMeshRenderer.transform.localToWorldMatrix;
            Graphics.DrawMeshNow(skinnedMeshRenderer.sharedMesh, rootMatrix * localMatrix);
        }
    }

    private void DrawFallbackPreview(Vector3 position, Quaternion rotation, Vector3 scaleMultiplier)
    {
        Handles.color = new Color(0.1f, 0.8f, 1f, 0.35f);
        Matrix4x4 previousMatrix = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(position, rotation, scaleMultiplier);
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.matrix = previousMatrix;
    }

    private Material GetPrefabPreviewMaterial()
    {
        if (prefabPreviewMaterial != null)
        {
            return prefabPreviewMaterial;
        }

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        prefabPreviewMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        prefabPreviewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        prefabPreviewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        prefabPreviewMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        prefabPreviewMaterial.SetInt("_ZWrite", 0);
        prefabPreviewMaterial.SetColor("_Color", new Color(0.1f, 0.85f, 1f, 0.28f));

        return prefabPreviewMaterial;
    }

    private void SpawnObjects()
    {
        if ((spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Brush) && freehandPoints.Count == 0)
        {
            return;
        }

        if (spawnMode == SpawnMode.Line && (dragEnd - dragStart).sqrMagnitude < 0.0001f)
        {
            return;
        }

        Random.InitState(GetCurrentSeed());
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Spawn Level Area Objects");

        if (IsGroundPlacementMode())
        {
            SpawnGroundObject();
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        if (IsConsecutivePerimeterMode())
        {
            SpawnConsecutivePerimeterObjects();
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        if (spawnMode == SpawnMode.Brush && IsTiledPlacementMode())
        {
            SpawnBrushTiledObjects();
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        if (IsTiledPlacementMode())
        {
            SpawnTiledObjects();
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        if (spawnMode == SpawnMode.Brush)
        {
            SpawnBrushObjects();
            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        if (IsConsecutiveFreehandMode())
        {
            if (spawnMode == SpawnMode.Line)
            {
                SpawnConsecutiveLineObjects();
            }
            else
            {
                SpawnConsecutiveFreehandObjects();
            }

            Undo.CollapseUndoOperations(undoGroup);
            return;
        }

        int totalObjects = GetSpawnObjectCount();
        List<Vector3> placedPositions = new List<Vector3>();

        for (int i = 0; i < totalObjects; i++)
        {
            if (!TryGetSpawnPosition(i, placedPositions, out Vector3 position, out Vector3 tangent))
            {
                continue;
            }

            GameObject selectedPrefab = GetRandomPrefab();
            float scale = GetRandomScale();
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, GetPrefabFootprint(selectedPrefab).axis);
            rotation = ApplyRandomSpawnRotation(rotation);

            if (alignToSurface && TryProjectToSurface(position, out RaycastHit hit))
            {
                position = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, GetPrefabFootprint(selectedPrefab).axis);
                rotation = ApplyRandomSpawnRotation(rotation);
            }

            CreateSpawnedObject(selectedPrefab, position, rotation, scale);
            placedPositions.Add(position);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private void SpawnBrushObjects()
    {
        List<Vector3> strokePoints = GetBrushStrokePreviewPoints();

        if (strokePoints.Count == 0)
        {
            return;
        }

        Random.InitState(GetCurrentSeed());
        List<Vector3> placedPositions = new List<Vector3>();
        int objectsPerPoint = Mathf.Max(1, objectCount);

        for (int p = 0; p < strokePoints.Count; p++)
        {
            for (int i = 0; i < objectsPerPoint; i++)
            {
                if (!TryGetBrushPointSpawnPosition(strokePoints[p], placedPositions, out Vector3 position, out Vector3 tangent))
                {
                    continue;
                }

                GameObject selectedPrefab = GetRandomPrefab();
                float scale = GetRandomScale();
                Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, GetPrefabFootprint(selectedPrefab).axis);
                rotation = ApplyRandomSpawnRotation(rotation);

                if (alignToSurface && TryProjectToSurface(position, out RaycastHit hit))
                {
                    position = hit.point;
                    rotation = GetSpawnRotation(tangent, hit.normal, GetPrefabFootprint(selectedPrefab).axis);
                    rotation = ApplyRandomSpawnRotation(rotation);
                }

                CreateSpawnedObject(selectedPrefab, position, rotation, scale);
                placedPositions.Add(position);
            }
        }
    }

    private int DrawTiledArea(bool instantiate)
    {
        Vector2 cellSize = GetTiledCellSize();
        if (cellSize.x <= Mathf.Epsilon || cellSize.y <= Mathf.Epsilon)
        {
            return 0;
        }

        int guard = 0;
        const int maxTiles = 10000;
        int placedCount = 0;

        if (spawnMode == SpawnMode.Circle)
        {
            float radius = GetCircleRadius();
            Vector3 circleCenter = GetCircleCenter();
            float minX = circleCenter.x - radius;
            float maxX = circleCenter.x + radius;
            float minZ = circleCenter.z - radius;
            float maxZ = circleCenter.z + radius;
            int row = 0;

            for (float z = minZ; z <= maxZ - cellSize.y && guard < maxTiles; z += cellSize.y, row++)
            {
                float rowOffset = (row & 1) == 1 ? cellSize.x * 0.5f : 0f;
                for (float xPos = minX + rowOffset; xPos <= maxX - cellSize.x && guard < maxTiles; xPos += cellSize.x)
                {
                    GameObject selectedPrefab = GetRandomPrefab();
                    Quaternion rotation = GetTiledRotation();
                    float scale = GetRandomScale();
                    Vector3 scaleMultiplier = Vector3.one * scale;
                    Vector2 tileSize = cellSize;

                    Vector3 center = new Vector3(xPos + tileSize.x * 0.5f, placementY, z + tileSize.y * 0.5f);
                    if (!IsTileInsideCircle(center, tileSize))
                    {
                        continue;
                    }

                    PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
                    Vector3 position = GetPivotPositionForBoundsCenter(center, rotation, footprint, scaleMultiplier);

                    if (alignToSurface && TryProjectToSurface(center, out RaycastHit hit))
                    {
                        rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rotation;
                        position = GetPivotPositionForBoundsCenter(hit.point, rotation, footprint, scaleMultiplier);
                    }

                    if (instantiate)
                    {
                        CreateSpawnedObject(selectedPrefab, position, rotation, scaleMultiplier);
                    }
                    else
                    {
                        DrawPrefabPreview(selectedPrefab, position, rotation, scaleMultiplier);
                    }

                    placedCount++;
                    guard++;
                }
            }

            return placedCount;
        }

        Vector3 delta = dragEnd - dragStart;
        float availableX = Mathf.Abs(delta.x);
        float availableZ = Mathf.Abs(delta.z);
        if (availableX < cellSize.x || availableZ < cellSize.y)
        {
            return 0;
        }

        int columns = Mathf.FloorToInt(availableX / cellSize.x);
        int rows = Mathf.FloorToInt(availableZ / cellSize.y);
        if (columns <= 0 || rows <= 0)
        {
            return 0;
        }

        float xSign = Mathf.Sign(delta.x);
        float zSign = Mathf.Sign(delta.z);

        for (int xIndex = 0; xIndex < columns && guard < maxTiles; xIndex++)
        {
            for (int zIndex = 0; zIndex < rows && guard < maxTiles; zIndex++)
            {
                GameObject selectedPrefab = GetRandomPrefab();
                Quaternion rotation = GetTiledRotation();
                float scale = GetRandomScale();
                Vector3 scaleMultiplier = Vector3.one * scale;
                PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);

                Vector3 center = new Vector3(
                    dragStart.x + xSign * (xIndex + 0.5f) * cellSize.x,
                    placementY,
                    dragStart.z + zSign * (zIndex + 0.5f) * cellSize.y);
                Vector3 position = GetPivotPositionForBoundsCenter(center, rotation, footprint, scaleMultiplier);

                if (alignToSurface && TryProjectToSurface(center, out RaycastHit hit))
                {
                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rotation;
                    position = GetPivotPositionForBoundsCenter(hit.point, rotation, footprint, scaleMultiplier);
                }

                if (instantiate)
                {
                    CreateSpawnedObject(selectedPrefab, position, rotation, scaleMultiplier);
                }
                else
                {
                    DrawPrefabPreview(selectedPrefab, position, rotation, scaleMultiplier);
                }

                guard++;
                placedCount++;
            }
        }

        return placedCount;
    }

    private void SpawnTiledObjects()
    {
        Random.InitState(GetCurrentSeed());
        DrawTiledArea(true);
    }

    private void SpawnGroundObject()
    {
        Material finalMaterial = groundMaterial != null ? groundMaterial : GetFallbackGroundMaterial();
        if (finalMaterial == null)
        {
            return;
        }

        if (!TryCollectCurrentGroundCells(out List<GroundCell> cells, out bool tileable, out float tileSize))
        {
            return;
        }

        if (cells.Count == 0)
        {
            return;
        }

        GroundRegionData targetRegion = GetOrCreateGroundRegion(finalMaterial);
        if (targetRegion == null)
        {
            return;
        }

        Undo.RecordObject(targetRegion, "Spawn Ground");
        targetRegion.sourceMaterial = finalMaterial;
        targetRegion.tileSize = tileSize;

        List<GroundRegionData> regionsToRebuild = new List<GroundRegionData>();
        if (!regionsToRebuild.Contains(targetRegion))
        {
            regionsToRebuild.Add(targetRegion);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            GroundCell cell = cells[i];
            if (groundOverlapSubstitution)
            {
                RemoveCellFromOtherGroundRegions(cell, targetRegion, regionsToRebuild);
            }
            else if (IsCellOccupiedByOtherGroundRegion(cell, targetRegion))
            {
                continue;
            }

            if (!ContainsCell(targetRegion.cells, cell))
            {
                targetRegion.cells.Add(cell);
            }
        }

        for (int i = 0; i < regionsToRebuild.Count; i++)
        {
            RebuildGroundRegion(regionsToRebuild[i]);
        }
    }

    private bool TryMergeAnyGroundObject(Mesh newMesh, Vector3 newPivotPosition)
    {
        Material targetMaterial = groundMaterial != null ? groundMaterial : GetFallbackGroundMaterial();
        MeshFilter[] existingGrounds = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        if (newMesh == null)
        {
            return false;
        }

        if (!TryGetWorldBounds(newMesh, newPivotPosition, out Bounds newBounds))
        {
            return false;
        }

        for (int i = 0; i < existingGrounds.Length; i++)
        {
            MeshFilter existingFilter = existingGrounds[i];
            if (existingFilter == null || existingFilter.sharedMesh == null || existingFilter.transform == null)
            {
                continue;
            }

            if (existingFilter.gameObject.name != "Ground")
            {
                continue;
            }

            MeshRenderer existingRenderer = existingFilter.GetComponent<MeshRenderer>();
            MeshCollider existingCollider = existingFilter.GetComponent<MeshCollider>();
            if (existingRenderer == null || existingCollider == null)
            {
                continue;
            }

            if (existingRenderer.sharedMaterial != targetMaterial)
            {
                continue;
            }

            if (!TryGetWorldBounds(existingFilter, out Bounds existingBounds))
            {
                continue;
            }

            existingBounds.Expand(0.001f);
            newBounds.Expand(0.001f);
            if (!existingBounds.Intersects(newBounds))
            {
                continue;
            }

            Mesh combined = CombineGroundMeshes(existingFilter.sharedMesh, existingFilter.transform, newMesh, newPivotPosition);
            if (combined == null)
            {
                continue;
            }

            if (TryGetCurrentGroundTileSettings(out bool tileable, out float tileSize) && tileable)
            {
                ApplyWorldSpaceTileMapping(combined, existingFilter.transform, tileSize);
            }

            Undo.RecordObject(existingFilter, "Merge Ground");
            Mesh previousMesh = existingFilter.sharedMesh;
            existingFilter.sharedMesh = combined;
            existingFilter.transform.SetPositionAndRotation(existingFilter.transform.position, Quaternion.identity);
            existingCollider.sharedMesh = null;
            existingCollider.sharedMesh = combined;
            if (previousMesh != null && previousMesh != combined)
            {
                DestroyImmediate(previousMesh);
            }
            return true;
        }

        return false;
    }

    private GroundRegionData GetOrCreateGroundRegion(Material sourceMaterial)
    {
        GroundRegionData[] regions = UnityEngine.Object.FindObjectsByType<GroundRegionData>(FindObjectsSortMode.None);
        for (int i = 0; i < regions.Length; i++)
        {
            GroundRegionData region = regions[i];
            if (region != null && region.sourceMaterial == sourceMaterial)
            {
                return region;
            }
        }

        GameObject instance = new GameObject("Ground");
        Undo.RegisterCreatedObjectUndo(instance, "Spawn Ground");

        if (parent != null)
        {
            Undo.SetTransformParent(instance.transform, parent, "Parent Ground");
        }

        MeshFilter meshFilter = Undo.AddComponent<MeshFilter>(instance);
        MeshRenderer meshRenderer = Undo.AddComponent<MeshRenderer>(instance);
        MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(instance);
        GroundRegionData regionData = Undo.AddComponent<GroundRegionData>(instance);

        Material finalMaterial = sourceMaterial != null ? sourceMaterial : GetFallbackGroundMaterial();
        meshRenderer.sharedMaterial = finalMaterial;
        meshRenderer.sharedMaterials = new[] { finalMaterial };
        meshFilter.sharedMesh = new Mesh { name = "Ground Empty Mesh" };
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        regionData.sourceMaterial = finalMaterial;
        regionData.tileSize = 1f;
        return regionData;
    }

    private bool TryCollectCurrentGroundCells(out List<GroundCell> cells, out bool tileable, out float tileSize)
    {
        cells = new List<GroundCell>();
        tileable = false;
        tileSize = 1f;

        if (!groundPlacement)
        {
            return false;
        }

        if (spawnMode == SpawnMode.Rectangle)
        {
            tileable = rectangleGroundTileable;
            tileSize = rectangleGroundTileSize;
            return TryCollectRectangleGroundCells(out cells);
        }

        if (spawnMode == SpawnMode.Circle)
        {
            tileable = circleGroundTileable;
            tileSize = circleGroundTileSize;
            return TryCollectCircleGroundCells(out cells);
        }

        if (spawnMode == SpawnMode.Freehand)
        {
            tileable = freehandGroundTileable;
            tileSize = freehandGroundTileSize;
            return TryCollectFreehandGroundCells(out cells);
        }

        return false;
    }

    private bool TryCollectFreehandGroundCells(out List<GroundCell> cells)
    {
        cells = new List<GroundCell>();

        List<Vector3> points = GetGroundFreehandPoints();
        if (points == null || points.Count < 2)
        {
            return false;
        }

        float halfWidth = Mathf.Max(0.1f, freehandGroundWidth) * 0.5f;
        float cellReach = halfWidth + (groundCellSize * 0.70710678f);
        float cellReachSqr = cellReach * cellReach;
        HashSet<long> visited = new HashSet<long>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float minX = Mathf.Min(a.x, b.x) - cellReach;
            float maxX = Mathf.Max(a.x, b.x) + cellReach;
            float minZ = Mathf.Min(a.z, b.z) - cellReach;
            float maxZ = Mathf.Max(a.z, b.z) + cellReach;

            int cellMinX = Mathf.FloorToInt(minX / groundCellSize);
            int cellMaxX = Mathf.FloorToInt(maxX / groundCellSize);
            int cellMinZ = Mathf.FloorToInt(minZ / groundCellSize);
            int cellMaxZ = Mathf.FloorToInt(maxZ / groundCellSize);

            for (int z = cellMinZ; z <= cellMaxZ; z++)
            {
                for (int x = cellMinX; x <= cellMaxX; x++)
                {
                    Vector3 center = new Vector3((x + 0.5f) * groundCellSize, placementY, (z + 0.5f) * groundCellSize);
                    Vector3 closest = GetClosestPointOnSegment(center, a, b);
                    closest.y = placementY;
                    float distanceSqr = (center - closest).sqrMagnitude;
                    if (distanceSqr > cellReachSqr)
                    {
                        continue;
                    }

                    long key = (((long)x) << 32) ^ (uint)z;
                    if (visited.Add(key))
                    {
                        cells.Add(new GroundCell(x, z));
                    }
                }
            }
        }

        return cells.Count > 0;
    }

    private bool TryCollectRectangleGroundCells(out List<GroundCell> cells)
    {
        cells = new List<GroundCell>();
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        if (max.x <= min.x || max.z <= min.z)
        {
            return false;
        }

        int cellMinX = Mathf.FloorToInt(min.x / groundCellSize);
        int cellMaxX = Mathf.FloorToInt((max.x - 0.0001f) / groundCellSize);
        int cellMinZ = Mathf.FloorToInt(min.z / groundCellSize);
        int cellMaxZ = Mathf.FloorToInt((max.z - 0.0001f) / groundCellSize);

        for (int z = cellMinZ; z <= cellMaxZ; z++)
        {
            for (int x = cellMinX; x <= cellMaxX; x++)
            {
                Vector2 center = new Vector2((x + 0.5f) * groundCellSize, (z + 0.5f) * groundCellSize);
                if (center.x >= min.x && center.x <= max.x && center.y >= min.z && center.y <= max.z)
                {
                    cells.Add(new GroundCell(x, z));
                }
            }
        }

        return cells.Count > 0;
    }

    private bool TryCollectCircleGroundCells(out List<GroundCell> cells)
    {
        cells = new List<GroundCell>();
        float radius = GetCircleRadius();
        if (radius <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 center = GetCircleCenter();
        float minX = center.x - radius;
        float maxX = center.x + radius;
        float minZ = center.z - radius;
        float maxZ = center.z + radius;
        int cellMinX = Mathf.FloorToInt(minX / groundCellSize);
        int cellMaxX = Mathf.FloorToInt(maxX / groundCellSize);
        int cellMinZ = Mathf.FloorToInt(minZ / groundCellSize);
        int cellMaxZ = Mathf.FloorToInt(maxZ / groundCellSize);
        float radiusSqr = radius * radius;

        for (int z = cellMinZ; z <= cellMaxZ; z++)
        {
            for (int x = cellMinX; x <= cellMaxX; x++)
            {
                Vector2 cellCenter = new Vector2((x + 0.5f) * groundCellSize, (z + 0.5f) * groundCellSize);
                Vector2 circleCenter = new Vector2(center.x, center.z);
                if ((cellCenter - circleCenter).sqrMagnitude <= radiusSqr)
                {
                    cells.Add(new GroundCell(x, z));
                }
            }
        }

        return cells.Count > 0;
    }

    private bool TryBuildGroundMeshFromCells(List<GroundCell> cells, bool tileable, float tileSize, out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Cells Mesh";
        pivotPosition = Vector3.zero;

        if (cells == null || cells.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            return false;
        }

        float minX = (cells[0].x * groundCellSize);
        float maxX = minX + groundCellSize;
        float minZ = (cells[0].z * groundCellSize);
        float maxZ = minZ + groundCellSize;

        for (int i = 1; i < cells.Count; i++)
        {
            float cellMinX = cells[i].x * groundCellSize;
            float cellMaxX = cellMinX + groundCellSize;
            float cellMinZ = cells[i].z * groundCellSize;
            float cellMaxZ = cellMinZ + groundCellSize;
            minX = Mathf.Min(minX, cellMinX);
            maxX = Mathf.Max(maxX, cellMaxX);
            minZ = Mathf.Min(minZ, cellMinZ);
            maxZ = Mathf.Max(maxZ, cellMaxZ);
        }

        pivotPosition = new Vector3((minX + maxX) * 0.5f, placementY, (minZ + maxZ) * 0.5f);

        List<Vector3> vertices = new List<Vector3>(cells.Count * 4);
        List<Vector2> uvs = new List<Vector2>(cells.Count * 4);
        List<int> triangles = new List<int>(cells.Count * 6);
        float normalizedWidth = Mathf.Max(0.01f, maxX - minX);
        float normalizedHeight = Mathf.Max(0.01f, maxZ - minZ);
        float actualTileSize = Mathf.Max(0.01f, tileSize);

        for (int i = 0; i < cells.Count; i++)
        {
            float cellMinX = cells[i].x * groundCellSize;
            float cellMaxX = cellMinX + groundCellSize;
            float cellMinZ = cells[i].z * groundCellSize;
            float cellMaxZ = cellMinZ + groundCellSize;

            int index = vertices.Count;
            vertices.Add(new Vector3(cellMinX - pivotPosition.x, 0f, cellMinZ - pivotPosition.z));
            vertices.Add(new Vector3(cellMinX - pivotPosition.x, 0f, cellMaxZ - pivotPosition.z));
            vertices.Add(new Vector3(cellMaxX - pivotPosition.x, 0f, cellMaxZ - pivotPosition.z));
            vertices.Add(new Vector3(cellMaxX - pivotPosition.x, 0f, cellMinZ - pivotPosition.z));

            if (tileable)
            {
                uvs.Add(new Vector2(cellMinX / actualTileSize, cellMinZ / actualTileSize));
                uvs.Add(new Vector2(cellMinX / actualTileSize, cellMaxZ / actualTileSize));
                uvs.Add(new Vector2(cellMaxX / actualTileSize, cellMaxZ / actualTileSize));
                uvs.Add(new Vector2(cellMaxX / actualTileSize, cellMinZ / actualTileSize));
            }
            else
            {
                uvs.Add(new Vector2((cellMinX - minX) / normalizedWidth, (cellMinZ - minZ) / normalizedHeight));
                uvs.Add(new Vector2((cellMinX - minX) / normalizedWidth, (cellMaxZ - minZ) / normalizedHeight));
                uvs.Add(new Vector2((cellMaxX - minX) / normalizedWidth, (cellMaxZ - minZ) / normalizedHeight));
                uvs.Add(new Vector2((cellMaxX - minX) / normalizedWidth, (cellMinZ - minZ) / normalizedHeight));
            }

            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool ContainsCell(List<GroundCell> cells, GroundCell cell)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x == cell.x && cells[i].z == cell.z)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCellOccupiedByOtherGroundRegion(GroundCell cell, GroundRegionData targetRegion)
    {
        GroundRegionData[] regions = UnityEngine.Object.FindObjectsByType<GroundRegionData>(FindObjectsSortMode.None);
        for (int i = 0; i < regions.Length; i++)
        {
            GroundRegionData region = regions[i];
            if (region == null || region == targetRegion || region.cells == null)
            {
                continue;
            }

            if (ContainsCell(region.cells, cell))
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveCellFromOtherGroundRegions(GroundCell cell, GroundRegionData targetRegion, List<GroundRegionData> affectedRegions)
    {
        GroundRegionData[] regions = UnityEngine.Object.FindObjectsByType<GroundRegionData>(FindObjectsSortMode.None);
        for (int i = 0; i < regions.Length; i++)
        {
            GroundRegionData region = regions[i];
            if (region == null || region == targetRegion || region.cells == null)
            {
                continue;
            }

            int index = -1;
            for (int j = 0; j < region.cells.Count; j++)
            {
                if (region.cells[j].x == cell.x && region.cells[j].z == cell.z)
                {
                    index = j;
                    break;
                }
            }

            if (index < 0)
            {
                continue;
            }

            Undo.RecordObject(region, "Ground Overlap Substitution");
            region.cells.RemoveAt(index);
            if (!affectedRegions.Contains(region))
            {
                affectedRegions.Add(region);
            }

            if (region.cells.Count == 0)
            {
                DestroyImmediate(region.gameObject);
            }
        }
    }

    private void RebuildGroundRegion(GroundRegionData region)
    {
        if (region == null || region.gameObject == null)
        {
            return;
        }

        MeshFilter meshFilter = region.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = region.GetComponent<MeshRenderer>();
        MeshCollider meshCollider = region.GetComponent<MeshCollider>();
        if (meshFilter == null || meshRenderer == null || meshCollider == null)
        {
            return;
        }

        if (region.cells == null || region.cells.Count == 0)
        {
            meshFilter.sharedMesh = null;
            meshCollider.sharedMesh = null;
            DestroyImmediate(region.gameObject);
            return;
        }

        if (!TryBuildGroundMeshFromCells(region.cells, true, region.tileSize, out Mesh mesh, out Vector3 pivotPosition))
        {
            meshFilter.sharedMesh = null;
            meshCollider.sharedMesh = null;
            DestroyImmediate(region.gameObject);
            return;
        }

        Mesh previousMesh = meshFilter.sharedMesh;
        Undo.RecordObject(meshFilter, "Rebuild Ground");
        Undo.RecordObject(meshRenderer, "Rebuild Ground");
        Undo.RecordObject(meshCollider, "Rebuild Ground");
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
        region.transform.SetPositionAndRotation(pivotPosition, Quaternion.identity);
        meshRenderer.sharedMaterial = region.sourceMaterial != null ? region.sourceMaterial : GetFallbackGroundMaterial();
        meshRenderer.sharedMaterials = new[] { meshRenderer.sharedMaterial };

        if (previousMesh != null && previousMesh != mesh)
        {
            DestroyImmediate(previousMesh);
        }
    }

    private Mesh CombineGroundMeshes(Mesh existingMesh, Transform existingTransform, Mesh newMesh, Vector3 newPivotPosition)
    {
        if (existingMesh == null || newMesh == null || existingTransform == null)
        {
            return null;
        }

        Mesh combined = new Mesh();
        combined.name = "Ground Combined Mesh";
        combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CombineInstance[] combine = new CombineInstance[2];
        combine[0] = new CombineInstance
        {
            mesh = existingMesh,
            transform = Matrix4x4.identity
        };
        combine[1] = new CombineInstance
        {
            mesh = newMesh,
            transform = existingTransform.worldToLocalMatrix * Matrix4x4.TRS(newPivotPosition, Quaternion.identity, Vector3.one)
        };

        combined.CombineMeshes(combine, true, true);
        combined.RecalculateNormals();
        combined.RecalculateBounds();
        return combined;
    }

    private bool TryGetCurrentGroundTileSettings(out bool tileable, out float tileSize)
    {
        tileable = false;
        tileSize = 1f;

        if (!groundPlacement)
        {
            return false;
        }

        if (spawnMode == SpawnMode.Rectangle)
        {
            tileable = rectangleGroundTileable;
            tileSize = rectangleGroundTileSize;
            return true;
        }

        if (spawnMode == SpawnMode.Circle)
        {
            tileable = circleGroundTileable;
            tileSize = circleGroundTileSize;
            return true;
        }

        if (spawnMode == SpawnMode.Freehand)
        {
            tileable = freehandGroundTileable;
            tileSize = freehandGroundTileSize;
            return true;
        }

        return false;
    }

    private void ApplyWorldSpaceTileMapping(Mesh mesh, Transform referenceTransform, float tileSize)
    {
        if (mesh == null || referenceTransform == null || mesh.vertexCount == 0)
        {
            return;
        }

        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];
        float size = Mathf.Max(0.01f, tileSize);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 world = referenceTransform.TransformPoint(vertices[i]);
            uvs[i] = new Vector2(world.x / size, world.z / size);
        }

        mesh.uv = uvs;
    }

    private bool TryMergeRectangleGroundObject(Mesh newMesh, Rect newRectangle)
    {
        Material targetMaterial = groundMaterial != null ? groundMaterial : GetFallbackGroundMaterial();
        MeshFilter[] existingGrounds = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        for (int i = 0; i < existingGrounds.Length; i++)
        {
            MeshFilter existingFilter = existingGrounds[i];
            if (existingFilter == null || existingFilter.sharedMesh == null || existingFilter.transform == null)
            {
                continue;
            }

            if (existingFilter.gameObject.name != "Ground" || existingFilter.sharedMesh.name != "Ground Rectangle Mesh")
            {
                continue;
            }

            MeshRenderer existingRenderer = existingFilter.GetComponent<MeshRenderer>();
            MeshCollider existingCollider = existingFilter.GetComponent<MeshCollider>();
            if (existingRenderer == null || existingCollider == null)
            {
                continue;
            }

            if (existingRenderer.sharedMaterial != targetMaterial)
            {
                continue;
            }

            RectangleGroundData data = existingFilter.GetComponent<RectangleGroundData>();
            if (data == null)
            {
                data = existingFilter.gameObject.AddComponent<RectangleGroundData>();
                if (TryGetWorldBounds(existingFilter, out Bounds existingBounds))
                {
                    data.rectangles.Add(new Rect(existingBounds.min.x, existingBounds.min.z, existingBounds.size.x, existingBounds.size.z));
                }
            }

            if (data == null)
            {
                continue;
            }

            if (!TryGetRectangleBounds(data.rectangles, out Bounds existingRectBounds) || !TryGetRectangleBounds(newRectangle, out Bounds newBounds))
            {
                continue;
            }

            existingRectBounds.Expand(0.001f);
            newBounds.Expand(0.001f);

            if (!existingRectBounds.Intersects(newBounds))
            {
                continue;
            }

            data.rectangles.Add(newRectangle);

            if (!TryBuildRectangleGroundMeshFromRects(data.rectangles, out Mesh combined, out Vector3 mergedPivot))
            {
                data.rectangles.RemoveAt(data.rectangles.Count - 1);
                continue;
            }

            Undo.RecordObject(existingFilter, "Merge Ground");
            Mesh previousMesh = existingFilter.sharedMesh;
            existingFilter.sharedMesh = combined;
            existingFilter.transform.SetPositionAndRotation(mergedPivot, Quaternion.identity);
            existingCollider.sharedMesh = null;
            existingCollider.sharedMesh = combined;
            if (previousMesh != null && previousMesh != combined)
            {
                DestroyImmediate(previousMesh);
            }
            DestroyImmediate(newMesh);
            return true;
        }

        return false;
    }

    private bool TryMergeCircleGroundObject(Mesh newMesh, CircleGroundShape newCircle)
    {
        Material targetMaterial = groundMaterial != null ? groundMaterial : GetFallbackGroundMaterial();
        MeshFilter[] existingGrounds = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        for (int i = 0; i < existingGrounds.Length; i++)
        {
            MeshFilter existingFilter = existingGrounds[i];
            if (existingFilter == null || existingFilter.sharedMesh == null || existingFilter.transform == null)
            {
                continue;
            }

            if (existingFilter.gameObject.name != "Ground" || existingFilter.sharedMesh.name != "Ground Circle Mesh")
            {
                continue;
            }

            MeshRenderer existingRenderer = existingFilter.GetComponent<MeshRenderer>();
            MeshCollider existingCollider = existingFilter.GetComponent<MeshCollider>();
            if (existingRenderer == null || existingCollider == null)
            {
                continue;
            }

            if (existingRenderer.sharedMaterial != targetMaterial)
            {
                continue;
            }

            CircleGroundData data = existingFilter.GetComponent<CircleGroundData>();
            if (data == null)
            {
                data = existingFilter.gameObject.AddComponent<CircleGroundData>();
                if (TryGetWorldBounds(existingFilter, out Bounds seededCircleBounds))
                {
                    data.circles.Add(new CircleGroundShape
                    {
                        center = existingFilter.transform.position,
                        radius = Mathf.Max(seededCircleBounds.extents.x, seededCircleBounds.extents.z)
                    });
                }
            }

            if (data == null || data.circles.Count == 0)
            {
                continue;
            }

            if (!TryGetCircleGroundBounds(data.circles, out Bounds mergedCircleBounds) || !TryGetCircleGroundBounds(newCircle, out Bounds newBounds))
            {
                continue;
            }

            mergedCircleBounds.Expand(0.001f);
            newBounds.Expand(0.001f);

            if (!mergedCircleBounds.Intersects(newBounds))
            {
                continue;
            }

            data.circles.Add(newCircle);

            if (!TryBuildCircleGroundMeshFromCircles(data.circles, out Mesh combined, out Vector3 mergedPivot))
            {
                data.circles.RemoveAt(data.circles.Count - 1);
                continue;
            }

            Undo.RecordObject(existingFilter, "Merge Ground");
            Mesh previousMesh = existingFilter.sharedMesh;
            existingFilter.sharedMesh = combined;
            existingFilter.transform.SetPositionAndRotation(mergedPivot, Quaternion.identity);
            existingCollider.sharedMesh = null;
            existingCollider.sharedMesh = combined;
            if (previousMesh != null && previousMesh != combined)
            {
                DestroyImmediate(previousMesh);
            }
            DestroyImmediate(newMesh);
            return true;
        }

        return false;
    }

    private bool TryMergeFreehandGroundObject(Mesh newMesh, FreehandGroundPathData newPath)
    {
        Material targetMaterial = groundMaterial != null ? groundMaterial : GetFallbackGroundMaterial();
        MeshFilter[] existingGrounds = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        for (int i = 0; i < existingGrounds.Length; i++)
        {
            MeshFilter existingFilter = existingGrounds[i];
            if (existingFilter == null || existingFilter.sharedMesh == null || existingFilter.transform == null)
            {
                continue;
            }

            if (existingFilter.gameObject.name != "Ground" || !existingFilter.sharedMesh.name.StartsWith("Ground Freehand"))
            {
                continue;
            }

            MeshRenderer existingRenderer = existingFilter.GetComponent<MeshRenderer>();
            MeshCollider existingCollider = existingFilter.GetComponent<MeshCollider>();
            if (existingRenderer == null || existingCollider == null)
            {
                continue;
            }

            if (existingRenderer.sharedMaterial != targetMaterial)
            {
                continue;
            }

            FreehandGroundData data = existingFilter.GetComponent<FreehandGroundData>();
            if (data == null || data.paths.Count == 0)
            {
                continue;
            }

            if (!TryGetFreehandGroundBounds(data.paths, out Bounds mergedBounds) || !TryGetFreehandGroundBounds(newPath, out Bounds newBounds))
            {
                continue;
            }

            mergedBounds.Expand(0.001f);
            newBounds.Expand(0.001f);
            if (!mergedBounds.Intersects(newBounds) && !AreFreehandPathsConnected(data.paths, newPath))
            {
                continue;
            }

            List<FreehandGroundPathData> rebuiltPaths = new List<FreehandGroundPathData>();
            for (int p = 0; p < data.paths.Count; p++)
            {
                if (TryTrimFreehandPathAgainstCutter(data.paths[p], newPath, out List<FreehandGroundPathData> fragments))
                {
                    rebuiltPaths.AddRange(fragments);
                }
                else
                {
                    rebuiltPaths.Add(data.paths[p]);
                }
            }

            rebuiltPaths.Add(newPath);
            if (!TryBuildFreehandGroundMeshFromPaths(rebuiltPaths, out Mesh combined, out Vector3 mergedPivot))
            {
                continue;
            }

            data.paths.Clear();
            data.paths.AddRange(rebuiltPaths);

            Undo.RecordObject(existingFilter, "Merge Ground");
            Mesh previousMesh = existingFilter.sharedMesh;
            existingFilter.sharedMesh = combined;
            existingFilter.transform.SetPositionAndRotation(mergedPivot, Quaternion.identity);
            existingCollider.sharedMesh = null;
            existingCollider.sharedMesh = combined;
            if (previousMesh != null && previousMesh != combined)
            {
                DestroyImmediate(previousMesh);
            }
            DestroyImmediate(newMesh);
            return true;
        }

        return false;
    }

    private bool TryTrimFreehandPathAgainstCutter(FreehandGroundPathData path, FreehandGroundPathData cutter, out List<FreehandGroundPathData> fragments)
    {
        fragments = new List<FreehandGroundPathData>();
        if (path == null || path.points == null || path.points.Count < 2 || cutter == null || cutter.points == null || cutter.points.Count < 2)
        {
            return false;
        }

        float cutRadius = Mathf.Max(0.05f, freehandGroundWidth * 0.5f);
        float cutRadiusSqr = cutRadius * cutRadius;
        List<Vector3> current = new List<Vector3>();
        bool trimmedAny = false;

        for (int i = 0; i < path.points.Count; i++)
        {
            Vector3 point = path.points[i];
            bool insideCut = IsPointNearFreehandCutter(point, cutter, cutRadiusSqr);

            if (!insideCut)
            {
                current.Add(point);
                continue;
            }

            trimmedAny = true;
            if (current.Count >= 2)
            {
                fragments.Add(CreateFreehandPathFragment(current, path.closed));
            }
            current = new List<Vector3>();
        }

        if (current.Count >= 2)
        {
            fragments.Add(CreateFreehandPathFragment(current, path.closed));
        }

        return trimmedAny;
    }

    private bool IsPointNearFreehandCutter(Vector3 point, FreehandGroundPathData cutter, float cutRadiusSqr)
    {
        for (int i = 0; i < cutter.points.Count - 1; i++)
        {
            Vector3 closest = GetClosestPointOnSegment(point, cutter.points[i], cutter.points[i + 1]);
            if ((point - closest).sqrMagnitude <= cutRadiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private FreehandGroundPathData CreateFreehandPathFragment(List<Vector3> points, bool closed)
    {
        FreehandGroundPathData fragment = new FreehandGroundPathData
        {
            closed = false
        };

        for (int i = 0; i < points.Count; i++)
        {
            fragment.points.Add(points[i]);
        }

        return fragment;
    }

    private bool AreFreehandPathsConnected(List<FreehandGroundPathData> existingPaths, FreehandGroundPathData newPath)
    {
        if (existingPaths == null || newPath == null || newPath.points == null || newPath.points.Count < 2)
        {
            return false;
        }

        float connectDistance = Mathf.Max(0.05f, freehandGroundWidth * 0.35f);
        float connectDistanceSqr = connectDistance * connectDistance;

        for (int p = 0; p < existingPaths.Count; p++)
        {
            FreehandGroundPathData path = existingPaths[p];
            if (path == null || path.points == null || path.points.Count < 2)
            {
                continue;
            }

            for (int i = 0; i < newPath.points.Count; i++)
            {
                Vector3 point = newPath.points[i];
                for (int j = 0; j < path.points.Count - 1; j++)
                {
                    Vector3 closest = GetClosestPointOnSegment(point, path.points[j], path.points[j + 1]);
                    if ((point - closest).sqrMagnitude <= connectDistanceSqr)
                    {
                        return true;
                    }
                }
            }

            for (int i = 0; i < path.points.Count; i++)
            {
                Vector3 point = path.points[i];
                for (int j = 0; j < newPath.points.Count - 1; j++)
                {
                    Vector3 closest = GetClosestPointOnSegment(point, newPath.points[j], newPath.points[j + 1]);
                    if ((point - closest).sqrMagnitude <= connectDistanceSqr)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Rect GetCurrentRectangleGroundRect()
    {
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        return new Rect(min.x, min.z, max.x - min.x, max.z - min.z);
    }

    private CircleGroundShape GetCurrentCircleGroundShape()
    {
        return new CircleGroundShape
        {
            center = GetCircleCenter(),
            radius = GetCircleRadius()
        };
    }

    private FreehandGroundPathData GetCurrentFreehandGroundPath()
    {
        FreehandGroundPathData path = new FreehandGroundPathData
        {
            closed = freehandPathClosed
        };

        for (int i = 0; i < freehandPoints.Count; i++)
        {
            Vector3 point = freehandPoints[i];
            point.y = placementY;
            path.points.Add(point);
        }

        return path;
    }

    private bool TryGetWorldBounds(MeshFilter filter, out Bounds bounds)
    {
        if (filter == null || filter.sharedMesh == null)
        {
            bounds = default;
            return false;
        }

        return TryGetWorldBounds(filter.sharedMesh, filter.transform.localToWorldMatrix, out bounds);
    }

    private bool TryGetWorldBounds(Mesh mesh, Vector3 position, out Bounds bounds)
    {
        return TryGetWorldBounds(mesh, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one), out bounds);
    }

    private bool TryGetWorldBounds(Mesh mesh, Matrix4x4 localToWorld, out Bounds bounds)
    {
        bounds = default;
        if (mesh == null)
        {
            return false;
        }

        Bounds localBounds = mesh.bounds;
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        Vector3 worldPoint = localToWorld.MultiplyPoint3x4(corners[0]);
        bounds = new Bounds(worldPoint, Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
        {
            bounds.Encapsulate(localToWorld.MultiplyPoint3x4(corners[i]));
        }

        return true;
    }

    private bool TryBuildRectangleGroundMeshFromWorldBounds(Vector3 min, Vector3 max, out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Rectangle Mesh";

        Vector3 size = max - min;
        if (size.x <= Mathf.Epsilon || size.z <= Mathf.Epsilon)
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        pivotPosition = new Vector3((min.x + max.x) * 0.5f, placementY, (min.z + max.z) * 0.5f);
        Vector3 half = new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);

        mesh.vertices = new[]
        {
            new Vector3(-half.x, 0f, -half.z),
            new Vector3(-half.x, 0f, half.z),
            new Vector3(half.x, 0f, half.z),
            new Vector3(half.x, 0f, -half.z)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = rectangleGroundTileable
            ? new[]
            {
                Vector2.zero,
                new Vector2(0f, size.z / Mathf.Max(0.01f, rectangleGroundTileSize)),
                new Vector2(size.x / Mathf.Max(0.01f, rectangleGroundTileSize), size.z / Mathf.Max(0.01f, rectangleGroundTileSize)),
                new Vector2(size.x / Mathf.Max(0.01f, rectangleGroundTileSize), 0f)
            }
            : new[]
            {
                Vector2.zero,
                Vector2.up,
                Vector2.one,
                Vector2.right
            };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool TryGetRectangleBounds(Rect rect, out Bounds bounds)
    {
        bounds = new Bounds(new Vector3(rect.center.x, placementY, rect.center.y), new Vector3(Mathf.Max(0.001f, rect.width), 0.001f, Mathf.Max(0.001f, rect.height)));
        return rect.width > Mathf.Epsilon && rect.height > Mathf.Epsilon;
    }

    private bool TryGetRectangleBounds(List<Rect> rects, out Bounds bounds)
    {
        bounds = default;
        if (rects == null || rects.Count == 0)
        {
            return false;
        }

        float minX = rects[0].xMin;
        float maxX = rects[0].xMax;
        float minZ = rects[0].yMin;
        float maxZ = rects[0].yMax;

        for (int i = 1; i < rects.Count; i++)
        {
            Rect rect = rects[i];
            minX = Mathf.Min(minX, rect.xMin);
            maxX = Mathf.Max(maxX, rect.xMax);
            minZ = Mathf.Min(minZ, rect.yMin);
            maxZ = Mathf.Max(maxZ, rect.yMax);
        }

        bounds = new Bounds(new Vector3((minX + maxX) * 0.5f, placementY, (minZ + maxZ) * 0.5f), new Vector3(maxX - minX, 0.001f, maxZ - minZ));
        return true;
    }

    private bool TryBuildRectangleGroundMeshFromRects(List<Rect> rects, out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Rectangle Mesh";

        if (rects == null || rects.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        List<float> xCoords = new List<float>();
        List<float> zCoords = new List<float>();
        for (int i = 0; i < rects.Count; i++)
        {
            Rect rect = rects[i];
            if (rect.width <= Mathf.Epsilon || rect.height <= Mathf.Epsilon)
            {
                continue;
            }

            AddUniqueSorted(xCoords, rect.xMin);
            AddUniqueSorted(xCoords, rect.xMax);
            AddUniqueSorted(zCoords, rect.yMin);
            AddUniqueSorted(zCoords, rect.yMax);
        }

        if (xCoords.Count < 2 || zCoords.Count < 2)
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        float minX = xCoords[0];
        float maxX = xCoords[xCoords.Count - 1];
        float minZ = zCoords[0];
        float maxZ = zCoords[zCoords.Count - 1];
        pivotPosition = new Vector3((minX + maxX) * 0.5f, placementY, (minZ + maxZ) * 0.5f);

        int xCount = xCoords.Count;
        int zCount = zCoords.Count;
        Vector3[] vertices = new Vector3[xCount * zCount];
        Vector2[] uvs = new Vector2[xCount * zCount];

        for (int zi = 0; zi < zCount; zi++)
        {
            for (int xi = 0; xi < xCount; xi++)
            {
                int index = zi * xCount + xi;
                float worldX = xCoords[xi];
                float worldZ = zCoords[zi];
                vertices[index] = new Vector3(worldX - pivotPosition.x, 0f, worldZ - pivotPosition.z);
                uvs[index] = rectangleGroundTileable
                    ? new Vector2((worldX - minX) / Mathf.Max(0.01f, rectangleGroundTileSize), (worldZ - minZ) / Mathf.Max(0.01f, rectangleGroundTileSize))
                    : new Vector2((worldX - minX) / Mathf.Max(0.01f, maxX - minX), (worldZ - minZ) / Mathf.Max(0.01f, maxZ - minZ));
            }
        }

        List<int> triangles = new List<int>();
        for (int zi = 0; zi < zCount - 1; zi++)
        {
            for (int xi = 0; xi < xCount - 1; xi++)
            {
                Vector2 center = new Vector2((xCoords[xi] + xCoords[xi + 1]) * 0.5f, (zCoords[zi] + zCoords[zi + 1]) * 0.5f);
                if (!IsPointInsideAnyRectangle(center, rects))
                {
                    continue;
                }

                int bottomLeft = zi * xCount + xi;
                int topLeft = (zi + 1) * xCount + xi;
                int topRight = (zi + 1) * xCount + (xi + 1);
                int bottomRight = zi * xCount + (xi + 1);

                triangles.Add(bottomLeft);
                triangles.Add(topLeft);
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);
                triangles.Add(bottomRight);
            }
        }

        if (triangles.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            return false;
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private void AddUniqueSorted(List<float> values, float value)
    {
        const float epsilon = 0.0001f;
        for (int i = 0; i < values.Count; i++)
        {
            if (Mathf.Abs(values[i] - value) <= epsilon)
            {
                return;
            }
        }

        values.Add(value);
        values.Sort();
    }

    private bool IsPointInsideAnyRectangle(Vector2 point, List<Rect> rects)
    {
        for (int i = 0; i < rects.Count; i++)
        {
            Rect rect = rects[i];
            if (point.x >= rect.xMin - 0.0001f && point.x <= rect.xMax + 0.0001f &&
                point.y >= rect.yMin - 0.0001f && point.y <= rect.yMax + 0.0001f)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetCircleGroundBounds(CircleGroundShape circle, out Bounds bounds)
    {
        return TryGetCircleGroundBounds(new List<CircleGroundShape> { circle }, out bounds);
    }

    private bool TryGetCircleGroundBounds(List<CircleGroundShape> circles, out Bounds bounds)
    {
        bounds = default;
        if (circles == null || circles.Count == 0)
        {
            return false;
        }

        float minX = circles[0].center.x - circles[0].radius;
        float maxX = circles[0].center.x + circles[0].radius;
        float minZ = circles[0].center.z - circles[0].radius;
        float maxZ = circles[0].center.z + circles[0].radius;

        for (int i = 1; i < circles.Count; i++)
        {
            CircleGroundShape circle = circles[i];
            minX = Mathf.Min(minX, circle.center.x - circle.radius);
            maxX = Mathf.Max(maxX, circle.center.x + circle.radius);
            minZ = Mathf.Min(minZ, circle.center.z - circle.radius);
            maxZ = Mathf.Max(maxZ, circle.center.z + circle.radius);
        }

        bounds = new Bounds(new Vector3((minX + maxX) * 0.5f, placementY, (minZ + maxZ) * 0.5f), new Vector3(maxX - minX, 0.001f, maxZ - minZ));
        return true;
    }

    private bool TryBuildCircleGroundMeshFromCircles(List<CircleGroundShape> circles, out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Circle Mesh";

        if (circles == null || circles.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        if (!TryGetCircleGroundBounds(circles, out Bounds bounds))
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minZ = bounds.min.z;
        float maxZ = bounds.max.z;
        float smallestRadius = circles[0].radius;
        for (int i = 1; i < circles.Count; i++)
        {
            smallestRadius = Mathf.Min(smallestRadius, circles[i].radius);
        }

        float cellSize = Mathf.Clamp(smallestRadius / 40f, 0.02f, 0.15f);
        pivotPosition = new Vector3((minX + maxX) * 0.5f, placementY, (minZ + maxZ) * 0.5f);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (float z = minZ; z < maxZ; z += cellSize)
        {
            float nextZ = Mathf.Min(z + cellSize, maxZ);
            for (float x = minX; x < maxX; x += cellSize)
            {
                float nextX = Mathf.Min(x + cellSize, maxX);
                Vector2 center = new Vector2((x + nextX) * 0.5f, (z + nextZ) * 0.5f);
                if (!IsPointInsideAnyCircle(center, circles))
                {
                    continue;
                }

                int index = vertices.Count;
                vertices.Add(new Vector3(x - pivotPosition.x, 0f, z - pivotPosition.z));
                vertices.Add(new Vector3(x - pivotPosition.x, 0f, nextZ - pivotPosition.z));
                vertices.Add(new Vector3(nextX - pivotPosition.x, 0f, nextZ - pivotPosition.z));
                vertices.Add(new Vector3(nextX - pivotPosition.x, 0f, z - pivotPosition.z));

                if (circleGroundTileable)
                {
                    float tileSize = Mathf.Max(0.01f, circleGroundTileSize);
                    uvs.Add(new Vector2(x / tileSize, z / tileSize));
                    uvs.Add(new Vector2(x / tileSize, nextZ / tileSize));
                    uvs.Add(new Vector2(nextX / tileSize, nextZ / tileSize));
                    uvs.Add(new Vector2(nextX / tileSize, z / tileSize));
                }
                else
                {
                    uvs.Add(new Vector2((x - minX) / Mathf.Max(0.01f, maxX - minX), (z - minZ) / Mathf.Max(0.01f, maxZ - minZ)));
                    uvs.Add(new Vector2((x - minX) / Mathf.Max(0.01f, maxX - minX), (nextZ - minZ) / Mathf.Max(0.01f, maxZ - minZ)));
                    uvs.Add(new Vector2((nextX - minX) / Mathf.Max(0.01f, maxX - minX), (nextZ - minZ) / Mathf.Max(0.01f, maxZ - minZ)));
                    uvs.Add(new Vector2((nextX - minX) / Mathf.Max(0.01f, maxX - minX), (z - minZ) / Mathf.Max(0.01f, maxZ - minZ)));
                }

                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }
        }

        if (triangles.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            return false;
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool IsPointInsideAnyCircle(Vector2 point, List<CircleGroundShape> circles)
    {
        for (int i = 0; i < circles.Count; i++)
        {
            CircleGroundShape circle = circles[i];
            Vector2 center = new Vector2(circle.center.x, circle.center.z);
            if ((point - center).sqrMagnitude <= circle.radius * circle.radius)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetFreehandGroundBounds(FreehandGroundPathData path, out Bounds bounds)
    {
        bounds = default;
        if (path == null || path.points == null || path.points.Count == 0)
        {
            return false;
        }

        bounds = new Bounds(path.points[0], Vector3.zero);
        for (int i = 0; i < path.points.Count; i++)
        {
            bounds.Encapsulate(path.points[i]);
        }

        float extra = Mathf.Max(0.1f, freehandGroundWidth) * 0.5f;
        bounds.Expand(new Vector3(extra * 2f, 0.001f, extra * 2f));
        return true;
    }

    private bool TryGetFreehandGroundBounds(List<FreehandGroundPathData> paths, out Bounds bounds)
    {
        bounds = default;
        if (paths == null || paths.Count == 0)
        {
            return false;
        }

        bool hasBounds = false;
        for (int i = 0; i < paths.Count; i++)
        {
            if (!TryGetFreehandGroundBounds(paths[i], out Bounds pathBounds))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = pathBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(pathBounds.min);
                bounds.Encapsulate(pathBounds.max);
            }
        }

        return hasBounds;
    }

    private bool TryBuildFreehandGroundMeshFromPaths(List<FreehandGroundPathData> paths, out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Freehand Mesh";

        if (paths == null || paths.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        List<CombineInstance> combine = new List<CombineInstance>();
        List<Mesh> tempMeshes = new List<Mesh>();
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < paths.Count; i++)
        {
            if (!TryBuildSingleFreehandGroundMesh(paths[i], out Mesh pathMesh, out Vector3 pathPivot))
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = new Bounds(pathPivot, Vector3.zero);
                hasBounds = true;
            }

            combinedBounds.Encapsulate(pathPivot);
            combinedBounds.Encapsulate(pathPivot + pathMesh.bounds.min);
            combinedBounds.Encapsulate(pathPivot + pathMesh.bounds.max);

            combine.Add(new CombineInstance
            {
                mesh = pathMesh,
                transform = Matrix4x4.TRS(pathPivot, Quaternion.identity, Vector3.one)
            });
            tempMeshes.Add(pathMesh);
        }

        if (!hasBounds || combine.Count == 0)
        {
            DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        pivotPosition = combinedBounds.center;
        Matrix4x4 toPivot = Matrix4x4.TRS(-pivotPosition, Quaternion.identity, Vector3.one);
        for (int i = 0; i < combine.Count; i++)
        {
            combine[i] = new CombineInstance
            {
                mesh = combine[i].mesh,
                transform = toPivot * combine[i].transform
            };
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.CombineMeshes(combine.ToArray(), true, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        for (int i = 0; i < tempMeshes.Count; i++)
        {
            if (tempMeshes[i] != null)
            {
                DestroyImmediate(tempMeshes[i]);
            }
        }

        return true;
    }

    private bool TryBuildSingleFreehandGroundMesh(FreehandGroundPathData path, out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = null;
        pivotPosition = Vector3.zero;

        if (path == null || path.points == null || path.points.Count == 0)
        {
            return false;
        }

        if (path.closed && path.points.Count >= 3)
        {
            List<Vector3> points = new List<Vector3>(path.points);
            return TryBuildClosedFreehandMesh(points, out mesh, out pivotPosition);
        }

        return TryBuildFreehandStripMesh(path.points, out mesh, out pivotPosition);
    }

    private void DrawGroundPreview()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (!TryBuildGroundMesh(out Mesh mesh, out Vector3 pivotPosition))
        {
            return;
        }

        Material previewMaterial = GetFallbackGroundMaterial();
        if (previewMaterial == null)
        {
            DestroyImmediate(mesh);
            return;
        }

        previewMaterial.SetPass(0);
        try
        {
            Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(pivotPosition, Quaternion.identity, Vector3.one));
        }
        finally
        {
            DestroyImmediate(mesh);
        }
    }

    private Material groundFallbackMaterial;

    private Material GetFallbackGroundMaterial()
    {
        if (groundFallbackMaterial != null)
        {
            return groundFallbackMaterial;
        }

        Shader shader = Shader.Find("Standard");
        groundFallbackMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/Internal-Colored"));
        groundFallbackMaterial.hideFlags = HideFlags.HideAndDontSave;
        return groundFallbackMaterial;
    }

    private void DrawOptionHelp(string message)
    {
        EditorGUILayout.HelpBox(message, MessageType.None);
    }

    private void SpawnConsecutiveFreehandObjects()
    {
        DrawConsecutiveFreehandObjects(true);
    }

    private void SpawnConsecutiveLineObjects()
    {
        DrawConsecutiveLineObjects(true);
    }

    private void DrawConsecutiveLineObjects(bool instantiate)
    {
        Vector3 segment = dragEnd - dragStart;
        float pathLength = segment.magnitude;

        if (pathLength <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 tangent = segment / pathLength;
        float distance = 0f;
        int guard = 0;
        const int maxObjects = 10000;

        while (distance < pathLength && guard < maxObjects)
        {
            GameObject selectedPrefab = GetRandomPrefab();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            float scale = GetRandomScale();
            float scaledLength = Mathf.Max(0.01f, footprint.length * scale);
            float remainingLength = pathLength - distance;

            if (remainingLength <= Mathf.Epsilon)
            {
                break;
            }

            float fittedLength = Mathf.Min(scaledLength, remainingLength);
            Vector3 boundsCenter = dragStart + tangent * (distance + fittedLength * 0.5f);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                boundsCenter = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
            }

            Vector3 scaleMultiplier = GetLengthAdjustedScaleMultiplier(footprint, scale, scaledLength, fittedLength);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, scaleMultiplier);

            if (instantiate)
            {
                CreateSpawnedObject(selectedPrefab, position, rotation, scaleMultiplier);
            }
            else
            {
                DrawPrefabPreview(selectedPrefab, position, rotation, scaleMultiplier);
            }

            distance += fittedLength;
            guard++;
        }
    }

    private void DrawConsecutiveFreehandObjects(bool instantiate)
    {
        float pathLength = GetFreehandPathLength();

        if (pathLength <= Mathf.Epsilon)
        {
            return;
        }

        float distance = 0f;
        int guard = 0;
        const int maxObjects = 10000;

        while (distance < pathLength && guard < maxObjects)
        {
            GameObject selectedPrefab = GetRandomPrefab();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            float scale = GetRandomScale();
            float scaledLength = Mathf.Max(0.01f, footprint.length * scale);
            float remainingLength = pathLength - distance;

            if (remainingLength <= Mathf.Epsilon)
            {
                break;
            }

            float arcLength = Mathf.Min(scaledLength, remainingLength);
            Vector3 segmentStart = GetPointOnFreehandPath(distance, out _);
            Vector3 segmentEnd = GetPointOnFreehandPath(distance + arcLength, out _);
            Vector3 tangent = segmentEnd - segmentStart;
            float fittedLength = tangent.magnitude;

            if (fittedLength <= Mathf.Epsilon)
            {
                distance += arcLength;
                guard++;
                continue;
            }

            tangent /= fittedLength;
            Vector3 boundsCenter = (segmentStart + segmentEnd) * 0.5f;
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                boundsCenter = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
            }

            Vector3 scaleMultiplier = GetLengthAdjustedScaleMultiplier(footprint, scale, scaledLength, fittedLength);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, scaleMultiplier);

            if (instantiate)
            {
                CreateSpawnedObject(selectedPrefab, position, rotation, scaleMultiplier);
            }
            else
            {
                DrawPrefabPreview(selectedPrefab, position, rotation, scaleMultiplier);
            }

            distance += arcLength;
            guard++;
        }
    }

    private void SpawnConsecutivePerimeterObjects()
    {
        if (spawnMode == SpawnMode.Rectangle)
        {
            SpawnConsecutiveRectanglePerimeterObjects();
            return;
        }

        float perimeter = GetPerimeterLength();

        if (perimeter <= Mathf.Epsilon)
        {
            return;
        }

        float distance = 0f;
        int guard = 0;
        const int maxObjects = 10000;

        while (distance < perimeter && guard < maxObjects)
        {
            GameObject selectedPrefab = GetRandomPrefab();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            float scale = GetRandomScale();
            float scaledLength = Mathf.Max(0.01f, footprint.length * scale);
            float remainingLength = perimeter - distance;

            if (remainingLength <= Mathf.Epsilon)
            {
                break;
            }

            float fittedLength = Mathf.Min(scaledLength, remainingLength);
            Vector3 boundsCenter = GetConsecutivePerimeterPoint(distance + fittedLength * 0.5f, out Vector3 tangent);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, footprint.axis);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                boundsCenter = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, footprint.axis);
            }

            Vector3 scaleMultiplier = GetLengthAdjustedScaleMultiplier(footprint, scale, scaledLength, fittedLength);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, scaleMultiplier);
            CreateSpawnedObject(selectedPrefab, position, rotation, scaleMultiplier);
            distance += fittedLength;
            guard++;
        }
    }

    private void SpawnConsecutiveRectanglePerimeterObjects()
    {
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        Vector3 bottomLeft = new Vector3(min.x, placementY, min.z);
        Vector3 bottomRight = new Vector3(max.x, placementY, min.z);
        Vector3 topRight = new Vector3(max.x, placementY, max.z);
        Vector3 topLeft = new Vector3(min.x, placementY, max.z);

        SpawnConsecutiveRectangleSide(bottomLeft, bottomRight);
        SpawnConsecutiveRectangleSide(bottomRight, topRight);
        SpawnConsecutiveRectangleSide(topRight, topLeft);
        SpawnConsecutiveRectangleSide(topLeft, bottomLeft);
    }

    private void SpawnConsecutiveRectangleSide(Vector3 start, Vector3 end)
    {
        if (!TryGetUncoveredRectangleSide(start, end, out start, out end, out _, out _))
        {
            return;
        }

        Vector3 side = end - start;
        float sideLength = side.magnitude;

        if (sideLength <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 tangent = side / sideLength;
        List<PerimeterPiece> pieces = BuildPiecesForSide(sideLength);
        float distance = 0f;

        for (int i = 0; i < pieces.Count; i++)
        {
            PerimeterPiece piece = pieces[i];
            Vector3 boundsCenter = start + tangent * (distance + piece.fittedLength * 0.5f);
            Quaternion rotation = GetSpawnRotation(tangent, Vector3.up, piece.footprint.axis);

            if (alignToSurface && TryProjectToSurface(boundsCenter, out RaycastHit hit))
            {
                boundsCenter = hit.point;
                rotation = GetSpawnRotation(tangent, hit.normal, piece.footprint.axis);
            }

            Vector3 scaleMultiplier = GetFittedScaleMultiplier(piece);
            Vector3 position = GetPivotPositionForBoundsCenter(boundsCenter, rotation, piece.footprint, scaleMultiplier);
            CreateSpawnedObject(piece.prefab, position, rotation, scaleMultiplier);
            distance += piece.fittedLength;
        }
    }

    private bool TryGetUncoveredRectangleSide(Vector3 start, Vector3 end, out Vector3 adjustedStart, out Vector3 adjustedEnd, out float coveredDistance, out Vector3 tangent)
    {
        adjustedStart = start;
        adjustedEnd = end;
        coveredDistance = 0f;
        tangent = Vector3.forward;

        Vector3 side = end - start;
        float sideLength = side.magnitude;

        if (sideLength <= Mathf.Epsilon)
        {
            return false;
        }

        tangent = side / sideLength;

        float snapRadius = GetRectangleSideSnapRadius();
        if (rectangleSideSnapLocked && IsRectangleSideStillSnapped(start, end, rectangleSideSnapStart, rectangleSideSnapEnd, rectangleSideSnapTangent, snapRadius))
        {
            adjustedStart = rectangleSideSnapStart;
            adjustedEnd = rectangleSideSnapEnd;
            coveredDistance = rectangleSideSnapCoveredDistance;
            tangent = rectangleSideSnapTangent;
            return true;
        }

        rectangleSideSnapLocked = false;
        float forwardCoveredDistance = GetCoveredDistanceAlongRectangleSide(start, tangent, sideLength);
        float reverseCoveredDistance = GetCoveredDistanceAlongRectangleSide(end, -tangent, sideLength);

        coveredDistance = Mathf.Max(forwardCoveredDistance, reverseCoveredDistance);

        if (forwardCoveredDistance > 0f)
        {
            adjustedStart = start + tangent * forwardCoveredDistance;
        }

        if (reverseCoveredDistance > 0f)
        {
            adjustedEnd = end - tangent * reverseCoveredDistance;
        }

        if (coveredDistance >= sideLength - 0.0001f)
        {
            return false;
        }

        rectangleSideSnapLocked = forwardCoveredDistance > 0f || reverseCoveredDistance > 0f;
        rectangleSideSnapStart = adjustedStart;
        rectangleSideSnapEnd = adjustedEnd;
        rectangleSideSnapTangent = tangent;
        rectangleSideSnapCoveredDistance = coveredDistance;

        return true;
    }

    private float GetCoveredDistanceAlongRectangleSide(Vector3 start, Vector3 tangent, float sideLength)
    {
        GameObject[] prefabInstances = GetScenePrefabInstanceRoots();
        float snapRadius = GetRectangleSideSnapRadius();
        float snapRadiusSqr = snapRadius * snapRadius;
        List<Vector2> intervals = new List<Vector2>();

        for (int i = 0; i < prefabInstances.Length; i++)
        {
            GameObject instanceRoot = prefabInstances[i];

            if (!TryGetPrefabConnectionPoints(instanceRoot, out Vector3 startPoint, out Vector3 endPoint))
            {
                continue;
            }

            if (!TryGetRectangleSideInterval(start, tangent, sideLength, startPoint, endPoint, snapRadiusSqr, out Vector2 interval))
            {
                continue;
            }

            intervals.Add(interval);
        }

        float coveredDistance = 0f;
        bool progress = true;

        while (progress)
        {
            progress = false;

            for (int i = 0; i < intervals.Count; i++)
            {
                Vector2 interval = intervals[i];

                if (interval.x > coveredDistance + snapRadius)
                {
                    continue;
                }

                if (interval.y > coveredDistance + 0.0001f)
                {
                    coveredDistance = Mathf.Min(sideLength, interval.y);
                    progress = true;
                }
            }
        }

        return coveredDistance;
    }

    private void DrawRectangleSideSnapGuide(Vector3 sideStart, Vector3 tangent, float coveredDistance, float sideLength)
    {
        if (coveredDistance <= 0f || Event.current.type != EventType.Repaint)
        {
            return;
        }

        Vector3 guideStart = sideStart;
        Vector3 guideEnd = sideStart + tangent * Mathf.Min(coveredDistance, sideLength);

        Handles.color = new Color(0.2f, 1f, 0.7f, 0.2f);
        Handles.DrawAAPolyLine(10f, guideStart, guideEnd);

        Handles.color = new Color(0.2f, 1f, 0.7f, 0.9f);
        Handles.DrawAAPolyLine(3f, guideStart, guideEnd);
        Handles.SphereHandleCap(0, guideStart, Quaternion.identity, 0.12f, EventType.Repaint);
        Handles.SphereHandleCap(0, guideEnd, Quaternion.identity, 0.14f, EventType.Repaint);
    }

    private bool IsRectangleSideStillSnapped(Vector3 currentStart, Vector3 currentEnd, Vector3 snapStart, Vector3 snapEnd, Vector3 snapTangent, float snapRadius)
    {
        Vector3 currentSide = currentEnd - currentStart;
        float currentLength = currentSide.magnitude;

        if (currentLength <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 currentTangent = currentSide / currentLength;
        if (Vector3.Dot(currentTangent, snapTangent) < 0.98f && Vector3.Dot(currentTangent, -snapTangent) < 0.98f)
        {
            return false;
        }

        float startDistance = DistanceToLine(currentStart, snapStart, snapTangent);
        float endDistance = DistanceToLine(currentEnd, snapStart, snapTangent);
        return startDistance <= snapRadius && endDistance <= snapRadius;
    }

    private float DistanceToLine(Vector3 point, Vector3 lineStart, Vector3 lineTangent)
    {
        Vector3 relative = point - lineStart;
        Vector3 closest = lineStart + lineTangent * Vector3.Dot(relative, lineTangent);
        Vector3 delta = point - closest;
        delta.y = 0f;
        return delta.magnitude;
    }

    private bool TryGetRectangleSideInterval(Vector3 sideStart, Vector3 tangent, float sideLength, Vector3 pointA, Vector3 pointB, float snapRadiusSqr, out Vector2 interval)
    {
        interval = default;

        if (!IsPointCloseToSide(pointA, sideStart, tangent, sideLength, snapRadiusSqr, out float distanceA))
        {
            return false;
        }

        if (!IsPointCloseToSide(pointB, sideStart, tangent, sideLength, snapRadiusSqr, out float distanceB))
        {
            return false;
        }

        float min = Mathf.Clamp(Mathf.Min(distanceA, distanceB), 0f, sideLength);
        float max = Mathf.Clamp(Mathf.Max(distanceA, distanceB), 0f, sideLength);

        if (max <= Mathf.Epsilon || min >= sideLength)
        {
            return false;
        }

        interval = new Vector2(min, max);
        return true;
    }

    private bool IsPointCloseToSide(Vector3 point, Vector3 sideStart, Vector3 tangent, float sideLength, float snapRadiusSqr, out float distanceAlongSide)
    {
        Vector3 relative = point - sideStart;
        float distance = Vector3.Dot(relative, tangent);
        distanceAlongSide = distance;

        float snapRadius = Mathf.Sqrt(snapRadiusSqr);

        if (distance < -Mathf.Max(0.01f, snapRadius) || distance > sideLength + Mathf.Max(0.01f, snapRadius))
        {
            return false;
        }

        Vector3 closestPoint = sideStart + tangent * distance;
        Vector3 delta = point - closestPoint;
        delta.y = 0f;
        return delta.sqrMagnitude <= snapRadiusSqr;
    }

    private float GetRectangleSideSnapRadius()
    {
        return GetConnectionSnapRadius() * rectangleSideSnapMultiplier;
    }

    private List<PerimeterPiece> BuildPiecesForSide(float sideLength)
    {
        List<PerimeterPiece> pieces = new List<PerimeterPiece>();
        float totalBaseLength = 0f;
        int guard = 0;
        const int maxObjectsPerSide = 2500;

        while (totalBaseLength < sideLength && guard < maxObjectsPerSide)
        {
            GameObject selectedPrefab = GetRandomPrefab();
            PrefabFootprint footprint = GetPrefabFootprint(selectedPrefab);
            float baseScale = GetRandomScale();
            float baseLength = Mathf.Max(0.01f, footprint.length * baseScale);

            pieces.Add(new PerimeterPiece
            {
                prefab = selectedPrefab,
                footprint = footprint,
                baseScale = baseScale,
                baseLength = baseLength,
                fittedLength = baseLength
            });

            totalBaseLength += baseLength;
            guard++;
        }

        if (pieces.Count == 0 || totalBaseLength <= Mathf.Epsilon)
        {
            return pieces;
        }

        float fitFactor = sideLength / totalBaseLength;

        for (int i = 0; i < pieces.Count; i++)
        {
            PerimeterPiece piece = pieces[i];
            piece.fittedLength = piece.baseLength * fitFactor;
            pieces[i] = piece;
        }

        return pieces;
    }

    private Vector3 GetFittedScaleMultiplier(PerimeterPiece piece)
    {
        Vector3 scaleMultiplier = Vector3.one * piece.baseScale;
        float lengthScale = piece.fittedLength / Mathf.Max(0.01f, piece.baseLength);

        if (Mathf.Abs(piece.footprint.axis.x) >= Mathf.Abs(piece.footprint.axis.z))
        {
            scaleMultiplier.x *= lengthScale;
        }
        else
        {
            scaleMultiplier.z *= lengthScale;
        }

        return scaleMultiplier;
    }

    private Vector3 GetLengthAdjustedScaleMultiplier(PrefabFootprint footprint, float baseScale, float baseLength, float fittedLength)
    {
        Vector3 scaleMultiplier = Vector3.one * baseScale;
        float lengthScale = fittedLength / Mathf.Max(0.01f, baseLength);

        if (Mathf.Abs(footprint.axis.x) >= Mathf.Abs(footprint.axis.z))
        {
            scaleMultiplier.x *= lengthScale;
        }
        else
        {
            scaleMultiplier.z *= lengthScale;
        }

        return scaleMultiplier;
    }

    private void DrawPrefabList()
    {
        EditorGUILayout.LabelField("Prefabs", EditorStyles.miniBoldLabel);

        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Element {i}", prefabs[i], typeof(GameObject), false, GUILayout.Height(48f));
            DrawPrefabThumbnail(prefabs[i]);

            if (GUILayout.Button("-", GUILayout.Width(24f)))
            {
                prefabs.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Prefab"))
        {
            prefabs.Add(null);
        }

        using (new EditorGUI.DisabledScope(prefabs.Count == 0))
        {
            if (GUILayout.Button("Clear"))
            {
                prefabs.Clear();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (prefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("If the list is empty, temporary cubes will be created.", MessageType.None);
        }
    }

    private void DrawSpawnModeSelector()
    {
        const int toolCount = 6;
        const float buttonWidth = 40f;
        const float buttonHeight = 40f;
        const float buttonSpacing = 4f;

        LoadToolIcons();
        int columns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 24f + buttonSpacing) / (buttonWidth + buttonSpacing)));
        int index = 0;

        while (index < toolCount)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int column = 0; column < columns && index < toolCount; column++)
            {
                if (column > 0)
                {
                    GUILayout.Space(buttonSpacing);
                }

                DrawToolButtonByIndex(index, buttonWidth, buttonHeight);
                index++;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawSelectedToolTitle()
    {
        EditorGUILayout.Space(4);
        GUIStyle centeredHeader = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("TOOL SELECTED", centeredHeader);

        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.18f, 0.24f, 0.34f, 1f);
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = previousColor;

        GUIStyle centeredTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField(GetSelectedToolTitle(), centeredTitle);
        GUILayout.EndVertical();
    }

    private string GetSelectedToolTitle()
    {
        if (eraserEnabled)
        {
            return "Eraser";
        }

        switch (spawnMode)
        {
            case SpawnMode.Rectangle:
                return "Rectangle";
            case SpawnMode.Circle:
                return "Circle";
            case SpawnMode.Freehand:
                return "Freehand";
            case SpawnMode.Brush:
                return "Brush";
            case SpawnMode.Line:
                return "Line";
            default:
                return "Mode";
        }
    }

    private void DrawToolButtonByIndex(int index, float width, float height)
    {
        switch (index)
        {
            case 0:
                DrawSpawnModeButton(SpawnMode.Rectangle, rectangleIcon, "Rect", "Draw a rectangular area.", width, height);
                break;
            case 1:
                DrawSpawnModeButton(SpawnMode.Circle, circleIcon, "Circle", "Draw a circular area.", width, height);
                break;
            case 2:
                DrawSpawnModeButton(SpawnMode.Freehand, freehandIcon, "Free", "Draw a freehand path.", width, height);
                break;
            case 3:
                DrawSpawnModeButton(SpawnMode.Brush, brushIcon, "Brush", "Draw a brush stroke with thickness.", width, height);
                break;
            case 4:
                DrawSpawnModeButton(SpawnMode.Line, lineIcon, "Line", "Draw a straight line.", width, height);
                break;
            case 5:
                DrawEraserModeButton(width, height);
                break;
        }
    }

    private void DrawSpawnModeButton(SpawnMode mode, Texture2D icon, string fallbackLabel, string tooltip, float width, float height)
    {
        bool selected = !eraserEnabled && spawnMode == mode;
        GUIContent content = GetToolButtonContent(icon, fallbackLabel, tooltip);
        Rect buttonRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

        if (GUI.Toggle(buttonRect, selected, content, EditorStyles.toolbarButton) != selected)
        {
            eraserEnabled = false;
            spawnMode = mode;
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            SceneView.RepaintAll();
        }
    }

    private void DrawEraserModeButton(float width, float height)
    {
        GUIContent content = GetToolButtonContent(eraserIcon, "Erase", "Erase spawned prefab instances.");
        Rect buttonRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

        if (GUI.Toggle(buttonRect, eraserEnabled, content, EditorStyles.toolbarButton) != eraserEnabled)
        {
            eraserEnabled = true;
            isDragging = false;
            freehandPoints.Clear();
            freehandPathClosed = false;
            GUIUtility.hotControl = 0;
            SceneView.RepaintAll();
        }
    }

    private void LoadToolIcons()
    {
        rectangleIcon = LoadToolIcon(rectangleIcon, "rectangle");
        circleIcon = LoadToolIcon(circleIcon, "circle");
        freehandIcon = LoadToolIcon(freehandIcon, "freehand");
        brushIcon = LoadToolIcon(brushIcon, "brush");
        lineIcon = LoadToolIcon(lineIcon, "line");
        eraserIcon = LoadToolIcon(eraserIcon, "eraser");
    }

    private Texture2D LoadToolIcon(Texture2D currentIcon, string fileName)
    {
        if (currentIcon != null)
        {
            return currentIcon;
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Editor/Icons/{fileName}.png");
    }

    private GUIContent GetToolButtonContent(Texture2D icon, string fallbackLabel, string tooltip)
    {
        if (icon != null)
        {
            return new GUIContent(icon, tooltip);
        }

        return new GUIContent(fallbackLabel, tooltip);
    }

    private void DrawPrefabThumbnail(GameObject prefab)
    {
        Rect previewRect = GUILayoutUtility.GetRect(48f, 48f, GUILayout.Width(48f), GUILayout.Height(48f));
        GUI.Box(previewRect, GUIContent.none);

        if (prefab == null)
        {
            EditorGUI.LabelField(previewRect, "Empty", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Texture2D previewTexture = AssetPreview.GetAssetPreview(prefab);

        if (previewTexture == null)
        {
            previewTexture = AssetPreview.GetMiniThumbnail(prefab);
        }

        if (previewTexture == null)
        {
            EditorGUI.LabelField(previewRect, "No Preview", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
    }

    private void DrawScaleRange()
    {
        EditorGUILayout.LabelField("Random Scale", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Min", GUILayout.Width(28f));
        minScale = Mathf.Max(0.01f, EditorGUILayout.FloatField(minScale, GUILayout.MinWidth(48f)));
        EditorGUILayout.LabelField("Max", GUILayout.Width(32f));
        maxScale = Mathf.Max(0.01f, EditorGUILayout.FloatField(maxScale, GUILayout.MinWidth(48f)));
        EditorGUILayout.EndHorizontal();

        if (minScale > maxScale)
        {
            maxScale = minScale;
        }
    }

    private GameObject GetRandomPrefab()
    {
        int validPrefabCount = 0;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] != null)
            {
                validPrefabCount++;
            }
        }

        if (validPrefabCount == 0)
        {
            return null;
        }

        int selectedIndex = Random.Range(0, validPrefabCount);

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return prefabs[i];
            }

            selectedIndex--;
        }

        return null;
    }

    private GameObject GetFirstValidPrefab()
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] != null)
            {
                return prefabs[i];
            }
        }

        return null;
    }

    private Vector3 GetConnectionSnappedPoint(Vector3 point)
    {
        return GetConnectionSnappedPoint(point, point);
    }

    private Vector3 GetConnectionSnappedPoint(Vector3 point, Vector3 ignoredPoint)
    {
        if (!CanUseConnectionSnap())
        {
            return point;
        }

        if (TryFindNearestConnectionPoint(point, ignoredPoint, out Vector3 snapPoint))
        {
            return snapPoint;
        }

        return point;
    }

    private bool TryFindNearestConnectionPoint(Vector3 point, out Vector3 snapPoint)
    {
        return TryFindNearestConnectionPoint(point, point, out snapPoint);
    }

    private bool TryFindNearestConnectionPoint(Vector3 point, Vector3 ignoredPoint, out Vector3 snapPoint)
    {
        snapPoint = default;

        GameObject[] prefabInstances = GetScenePrefabInstanceRoots();
        float snapRadius = GetConnectionSnapRadius();
        float snapRadiusSqr = snapRadius * snapRadius;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < prefabInstances.Length; i++)
        {
            GameObject instanceRoot = prefabInstances[i];

            if (!TryGetPrefabConnectionPoints(instanceRoot, out Vector3 startPoint, out Vector3 endPoint))
            {
                continue;
            }

            TryUseCloserConnectionPoint(point, ignoredPoint, startPoint, snapRadiusSqr, ref bestDistanceSqr, ref snapPoint);
            TryUseCloserConnectionPoint(point, ignoredPoint, endPoint, snapRadiusSqr, ref bestDistanceSqr, ref snapPoint);
        }

        if (bestDistanceSqr < float.MaxValue)
        {
            snapPoint.y = placementY;
            return true;
        }

        return false;
    }

    private void ErasePrefabInstancesAt(Vector3 erasePoint)
    {
        GameObject[] erasableObjects = GetErasableObjectRoots();
        float eraseRadiusSqr = eraserRadius * eraserRadius;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Erase Level Area Objects");

        for (int i = 0; i < erasableObjects.Length; i++)
        {
            GameObject instanceRoot = erasableObjects[i];

            if (instanceRoot == null)
            {
                continue;
            }

            if (!IsPrefabInstanceInsideEraseRadius(instanceRoot, erasePoint, eraseRadiusSqr))
            {
                continue;
            }

            Undo.DestroyObjectImmediate(instanceRoot);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private GameObject[] GetErasableObjectRoots()
    {
        Transform[] sceneTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        HashSet<GameObject> roots = new HashSet<GameObject>();

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            GameObject gameObject = sceneTransforms[i].gameObject;

            if (!gameObject.scene.IsValid())
            {
                continue;
            }

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);

            if (prefabRoot != null)
            {
                if (HasPrefabSource(prefabRoot))
                {
                    roots.Add(prefabRoot);
                }

                continue;
            }

            if (IsFallbackLevelObject(gameObject))
            {
                roots.Add(gameObject);
            }
        }

        GameObject[] result = new GameObject[roots.Count];
        roots.CopyTo(result);
        return result;
    }

    private bool IsFallbackLevelObject(GameObject gameObject)
    {
        return gameObject.name == "Level Object";
    }

    private bool IsPrefabInstanceInsideEraseRadius(GameObject instanceRoot, Vector3 erasePoint, float eraseRadiusSqr)
    {
        if (TryGetWorldBounds(instanceRoot, out Bounds bounds))
        {
            Vector3 closestPoint = bounds.ClosestPoint(erasePoint);
            Vector3 flatDelta = closestPoint - erasePoint;
            flatDelta.y = 0f;
            return flatDelta.sqrMagnitude <= eraseRadiusSqr;
        }

        Vector3 pivotDelta = instanceRoot.transform.position - erasePoint;
        pivotDelta.y = 0f;
        return pivotDelta.sqrMagnitude <= eraseRadiusSqr;
    }

    private bool TryGetWorldBounds(GameObject root, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds();

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        return hasBounds;
    }

    private GameObject[] GetScenePrefabInstanceRoots()
    {
        Transform[] sceneTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        HashSet<GameObject> roots = new HashSet<GameObject>();

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            GameObject gameObject = sceneTransforms[i].gameObject;

            if (!gameObject.scene.IsValid())
            {
                continue;
            }

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);

            if (prefabRoot != null && HasPrefabSource(prefabRoot))
            {
                roots.Add(prefabRoot);
            }
        }

        GameObject[] result = new GameObject[roots.Count];
        roots.CopyTo(result);
        return result;
    }

    private bool HasPrefabSource(GameObject instanceRoot)
    {
        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
        return sourcePrefab != null;
    }

    private bool TryGetPrefabConnectionPoints(GameObject instanceRoot, out Vector3 startPoint, out Vector3 endPoint)
    {
        startPoint = default;
        endPoint = default;

        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);

        if (sourcePrefab == null)
        {
            return false;
        }

        PrefabFootprint footprint = GetPrefabFootprint(sourcePrefab);
        Vector3 localHalfLength = footprint.axis.normalized * footprint.length * 0.5f;
        startPoint = instanceRoot.transform.TransformPoint(footprint.center - localHalfLength);
        endPoint = instanceRoot.transform.TransformPoint(footprint.center + localHalfLength);
        return true;
    }

    private void TryUseCloserConnectionPoint(Vector3 point, Vector3 ignoredPoint, Vector3 candidate, float snapRadiusSqr, ref float bestDistanceSqr, ref Vector3 snapPoint)
    {
        Vector3 ignoredDelta = ignoredPoint - candidate;
        ignoredDelta.y = 0f;

        if (ignoredDelta.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 flatDelta = point - candidate;
        flatDelta.y = 0f;
        float distanceSqr = flatDelta.sqrMagnitude;

        if (distanceSqr <= snapRadiusSqr && distanceSqr < bestDistanceSqr)
        {
            bestDistanceSqr = distanceSqr;
            snapPoint = candidate;
        }
    }

    private float GetConnectionSnapRadius()
    {
        return Mathf.Max(0.35f, GetShortestPrefabLength() * Mathf.Max(0.01f, maxScale) * 0.35f);
    }

    private GameObject CreateSpawnedObject(GameObject selectedPrefab, Vector3 position, Quaternion rotation, float scale)
    {
        return CreateSpawnedObject(selectedPrefab, position, rotation, Vector3.one * scale);
    }

    private GameObject CreateSpawnedObject(GameObject selectedPrefab, Vector3 position, Quaternion rotation, Vector3 scaleMultiplier)
    {
        GameObject instance = selectedPrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        Undo.RegisterCreatedObjectUndo(instance, "Spawn Area Object");
        instance.name = selectedPrefab != null ? selectedPrefab.name : "Level Object";

        if (parent != null)
        {
            Undo.SetTransformParent(instance.transform, parent, "Parent Spawn Area Object");
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scaleMultiplier);
        return instance;
    }

    private float GetRandomScale()
    {
        if (IsTiledPlacementMode())
        {
            return tiledScale;
        }

        return Random.Range(minScale, maxScale);
    }

    private PrefabFootprint GetPrefabFootprint(GameObject prefab)
    {
        if (prefab == null)
        {
            return new PrefabFootprint { axis = Vector3.right, center = Vector3.zero, length = 1f };
        }

        if (!TryGetPrefabLocalBounds(prefab, out Bounds bounds))
        {
            return new PrefabFootprint { axis = Vector3.right, center = Vector3.zero, length = 1f };
        }

        Vector3 size = bounds.size;

        if (size.x >= size.z)
        {
            return new PrefabFootprint { axis = Vector3.right, center = bounds.center, length = Mathf.Max(0.01f, size.x) };
        }

        return new PrefabFootprint { axis = Vector3.forward, center = bounds.center, length = Mathf.Max(0.01f, size.z) };
    }

    private Vector3 GetPivotPositionForBoundsCenter(Vector3 boundsCenter, Quaternion rotation, PrefabFootprint footprint, float scale)
    {
        return GetPivotPositionForBoundsCenter(boundsCenter, rotation, footprint, Vector3.one * scale);
    }

    private Vector3 GetPivotPositionForBoundsCenter(Vector3 boundsCenter, Quaternion rotation, PrefabFootprint footprint, Vector3 scaleMultiplier)
    {
        return boundsCenter - rotation * Vector3.Scale(footprint.center, scaleMultiplier);
    }

    private bool TryGetPrefabLocalBounds(GameObject prefab, out Bounds localBounds)
    {
        bool hasBounds = false;
        localBounds = new Bounds();

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            EncapsulateWorldBoundsInPrefabSpace(prefab.transform, renderers[i].bounds, ref localBounds, ref hasBounds);
        }

        if (!hasBounds)
        {
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                EncapsulateWorldBoundsInPrefabSpace(prefab.transform, colliders[i].bounds, ref localBounds, ref hasBounds);
            }
        }

        return hasBounds;
    }

    private void EncapsulateWorldBoundsInPrefabSpace(Transform prefabRoot, Bounds worldBounds, ref Bounds localBounds, ref bool hasBounds)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 localPoint = prefabRoot.InverseTransformPoint(corners[i]);

            if (!hasBounds)
            {
                localBounds = new Bounds(localPoint, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                localBounds.Encapsulate(localPoint);
            }
        }
    }

    private bool TryGetSpawnPosition(int index, List<Vector3> placedPositions, out Vector3 position, out Vector3 tangent)
    {
        if (IsConsecutivePerimeterMode())
        {
            position = GetConsecutivePerimeterPoint(index, out tangent);
            return true;
        }

        if (spawnMode == SpawnMode.Brush)
        {
            int brushAttempts = 100;

            for (int i = 0; i < brushAttempts; i++)
            {
                if (!TryGetBrushSpawnPosition(index, out position, out tangent))
                {
                    continue;
                }

                if (HasEnoughSpacing(position, placedPositions))
                {
                    return true;
                }
            }

            position = default;
            tangent = Vector3.forward;
            return false;
        }

        int attempts = spawnMode == SpawnMode.Freehand ? 1 : 100;

        for (int i = 0; i < attempts; i++)
        {
            position = GetSpawnPosition(index);
            tangent = Vector3.forward;

            if (HasEnoughSpacing(position, placedPositions))
            {
                return true;
            }
        }

        position = default;
        tangent = Vector3.forward;
        return false;
    }

    private bool IsConsecutivePerimeterMode()
    {
        return (spawnMode == SpawnMode.Rectangle || spawnMode == SpawnMode.Circle) && spawnOnPerimeterOnly && placeConsecutivelyOnPerimeter;
    }

    private bool IsConsecutiveFreehandMode()
    {
        return (spawnMode == SpawnMode.Freehand || spawnMode == SpawnMode.Line) && placeConsecutivelyOnFreehand;
    }

    private bool IsConsecutivePlacementMode()
    {
        return IsConsecutivePerimeterMode() || IsConsecutiveFreehandMode();
    }

    private bool IsGroundPlacementMode()
    {
        return groundPlacement && (spawnMode == SpawnMode.Rectangle || spawnMode == SpawnMode.Circle || spawnMode == SpawnMode.Freehand);
    }

    private int GetSpawnObjectCount()
    {
        if (IsTiledPlacementMode())
        {
            return 0;
        }

        if (spawnMode == SpawnMode.Freehand)
        {
            return freehandPoints.Count;
        }

        if (spawnMode == SpawnMode.Brush)
        {
            return GetBrushSpawnObjectCount();
        }

        if (spawnMode == SpawnMode.Line)
        {
            return objectCount;
        }

        if (!IsConsecutivePerimeterMode())
        {
            return objectCount;
        }

        return objectCount;
    }

    private int GetBrushSpawnObjectCount()
    {
        if (freehandPoints.Count < 2)
        {
            return Mathf.Max(1, objectCount);
        }

        float densityStep = Mathf.Max(brushRadius * 0.75f, Mathf.Max(assetSpacing, 0.25f));
        int pathBasedCount = Mathf.CeilToInt(GetFreehandPathLength() / densityStep) + 1;
        return Mathf.Max(objectCount, pathBasedCount);
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnMode == SpawnMode.Rectangle)
        {
            if (spawnOnPerimeterOnly)
            {
                return GetRandomPointOnRectanglePerimeter();
            }

            return GetRandomPointInRectangle();
        }

        if (spawnMode == SpawnMode.Circle)
        {
            if (spawnOnPerimeterOnly)
            {
                return GetRandomPointOnCirclePerimeter();
            }

            return GetRandomPointInCircle();
        }

        if (spawnMode == SpawnMode.Line)
        {
            return GetRandomPointOnLine();
        }

        if (spawnMode == SpawnMode.Brush)
        {
            return freehandPoints.Count > 0 ? freehandPoints[0] : dragStart;
        }

        return freehandPoints[index];
    }

    private bool TryGetBrushSpawnPosition(int index, out Vector3 position, out Vector3 tangent)
    {
        position = default;
        tangent = Vector3.forward;

        if (freehandPoints.Count == 0 && spawnMode != SpawnMode.Brush)
        {
            return false;
        }

        if (!GetBrushSampleCenter(index, out Vector3 sampleCenter, out tangent))
        {
            return false;
        }

        Vector2 offset2D = Random.insideUnitCircle * brushRadius;
        position = new Vector3(
            sampleCenter.x + offset2D.x,
            placementY,
            sampleCenter.z + offset2D.y);

        Vector3 delta = position - sampleCenter;
        delta.y = 0f;
        if (delta.sqrMagnitude > brushRadius * brushRadius)
        {
            position = sampleCenter;
        }

        return true;
    }

    private bool GetBrushSampleCenter(int index, out Vector3 sampleCenter, out Vector3 tangent)
    {
        sampleCenter = default;
        tangent = Vector3.forward;

        if (spawnMode == SpawnMode.Brush && freehandPoints.Count == 0)
        {
            sampleCenter = dragStart;
            return true;
        }

        if (freehandPoints.Count == 0)
        {
            return false;
        }

        float pathLength = GetFreehandPathLength();
        if (pathLength <= Mathf.Epsilon)
        {
            sampleCenter = freehandPoints[0];
            return true;
        }

        int totalObjects = Mathf.Max(1, GetSpawnObjectCount());
        float t = totalObjects == 1 ? 0.5f : (float)index / Mathf.Max(1, totalObjects - 1);
        float distance = pathLength * t;
        sampleCenter = GetPointOnFreehandPath(distance, out tangent);
        return true;
    }

    private Vector3 GetRandomPointInRectangle()
    {
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);

        return new Vector3(
            Random.Range(min.x, max.x),
            placementY,
            Random.Range(min.z, max.z));
    }

    private Vector3 GetRandomPointOnRectanglePerimeter()
    {
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        float width = Mathf.Abs(max.x - min.x);
        float depth = Mathf.Abs(max.z - min.z);

        if (width <= Mathf.Epsilon)
        {
            return new Vector3(min.x, placementY, Random.Range(min.z, max.z));
        }

        if (depth <= Mathf.Epsilon)
        {
            return new Vector3(Random.Range(min.x, max.x), placementY, min.z);
        }

        float perimeterPosition = Random.Range(0f, (width + depth) * 2f);

        if (perimeterPosition < width)
        {
            return new Vector3(min.x + perimeterPosition, placementY, min.z);
        }

        perimeterPosition -= width;

        if (perimeterPosition < depth)
        {
            return new Vector3(max.x, placementY, min.z + perimeterPosition);
        }

        perimeterPosition -= depth;

        if (perimeterPosition < width)
        {
            return new Vector3(max.x - perimeterPosition, placementY, max.z);
        }

        perimeterPosition -= width;
        return new Vector3(min.x, placementY, max.z - perimeterPosition);
    }

    private Vector3 GetRandomPointInCircle()
    {
        Vector2 offset = Random.insideUnitCircle * GetCircleRadius();
        Vector3 center = GetCircleCenter();
        return new Vector3(center.x + offset.x, placementY, center.z + offset.y);
    }

    private Vector3 GetRandomPointOnCirclePerimeter()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = GetCircleRadius();
        Vector3 center = GetCircleCenter();
        return new Vector3(
            center.x + Mathf.Cos(angle) * radius,
            placementY,
            center.z + Mathf.Sin(angle) * radius);
    }

    private Vector3 GetRandomPointOnLine()
    {
        return Vector3.Lerp(dragStart, dragEnd, Random.value);
    }

    private Vector3 GetConsecutivePerimeterPoint(float distance, out Vector3 tangent)
    {
        if (spawnMode == SpawnMode.Rectangle)
        {
            return GetPointOnRectanglePerimeter(distance, out tangent);
        }

        return GetPointOnCirclePerimeter(distance, out tangent);
    }

    private Vector3 GetPointOnRectanglePerimeter(float distance, out Vector3 tangent)
    {
        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        float width = Mathf.Abs(max.x - min.x);
        float depth = Mathf.Abs(max.z - min.z);
        float perimeter = (width + depth) * 2f;

        if (perimeter <= Mathf.Epsilon)
        {
            tangent = Vector3.forward;
            return dragStart;
        }

        distance = Mathf.Repeat(distance, perimeter);

        if (distance < width)
        {
            tangent = Vector3.right;
            return new Vector3(min.x + distance, placementY, min.z);
        }

        distance -= width;

        if (distance < depth)
        {
            tangent = Vector3.forward;
            return new Vector3(max.x, placementY, min.z + distance);
        }

        distance -= depth;

        if (distance < width)
        {
            tangent = Vector3.left;
            return new Vector3(max.x - distance, placementY, max.z);
        }

        distance -= width;
        tangent = Vector3.back;
        return new Vector3(min.x, placementY, max.z - distance);
    }

    private Vector3 GetPointOnCirclePerimeter(float distance, out Vector3 tangent)
    {
        float radius = GetCircleRadius();
        Vector3 center = GetCircleCenter();

        if (radius <= Mathf.Epsilon)
        {
            tangent = Vector3.forward;
            return dragStart;
        }

        Vector3 startOffset = dragStart - center;
        startOffset.y = 0f;
        float startAngle = Mathf.Atan2(startOffset.z, startOffset.x);
        float angle = startAngle + distance / radius;
        Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        tangent = new Vector3(-radial.z, 0f, radial.x);
        return new Vector3(
            center.x + radial.x * radius,
            placementY,
            center.z + radial.z * radius);
    }

    private Vector3 GetPointOnFreehandPath(float distance, out Vector3 tangent)
    {
        if (spawnMode == SpawnMode.Line)
        {
            Vector3 segment = dragEnd - dragStart;
            float segmentLength = segment.magnitude;

            if (segmentLength <= Mathf.Epsilon)
            {
                tangent = Vector3.forward;
                return dragStart;
            }

            tangent = segment / segmentLength;
            return dragStart + tangent * Mathf.Clamp(distance, 0f, segmentLength);
        }

        if (freehandPoints.Count == 0)
        {
            tangent = Vector3.forward;
            return dragStart;
        }

        if (freehandPoints.Count == 1)
        {
            tangent = Vector3.forward;
            return freehandPoints[0];
        }

        float remainingDistance = distance;

        for (int i = 0; i < freehandPoints.Count - 1; i++)
        {
            Vector3 start = freehandPoints[i];
            Vector3 end = freehandPoints[i + 1];
            Vector3 segment = end - start;
            float segmentLength = segment.magnitude;

            if (segmentLength <= Mathf.Epsilon)
            {
                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                tangent = segment / segmentLength;
                return start + tangent * remainingDistance;
            }

            remainingDistance -= segmentLength;
        }

        Vector3 lastSegment = freehandPoints[freehandPoints.Count - 1] - freehandPoints[freehandPoints.Count - 2];
        tangent = lastSegment.sqrMagnitude > 0.0001f ? lastSegment.normalized : Vector3.forward;
        return freehandPoints[freehandPoints.Count - 1];
    }

    private float GetPerimeterLength()
    {
        if (spawnMode == SpawnMode.Rectangle)
        {
            Vector3 min = Vector3.Min(dragStart, dragEnd);
            Vector3 max = Vector3.Max(dragStart, dragEnd);
            float width = Mathf.Abs(max.x - min.x);
            float depth = Mathf.Abs(max.z - min.z);
            return (width + depth) * 2f;
        }

        if (spawnMode == SpawnMode.Circle)
        {
            return GetCircleRadius() * Mathf.PI * 2f;
        }

        return 0f;
    }

    private float GetFreehandPathLength()
    {
        if (spawnMode == SpawnMode.Line)
        {
            return Vector3.Distance(dragStart, dragEnd);
        }

        float length = 0f;

        for (int i = 0; i < freehandPoints.Count - 1; i++)
        {
            length += Vector3.Distance(freehandPoints[i], freehandPoints[i + 1]);
        }

        return length;
    }

    private float GetPathLength(List<Vector3> points)
    {
        if (points == null || points.Count < 2)
        {
            return 0f;
        }

        float length = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            length += Vector3.Distance(points[i], points[i + 1]);
        }

        return length;
    }

    private void AddFreehandPoint(Vector3 point, bool force)
    {
        if (spawnMode != SpawnMode.Freehand && spawnMode != SpawnMode.Brush)
        {
            return;
        }

        if (IsConsecutiveFreehandMode())
        {
            AddConsecutiveFreehandPoint(point, force);
            return;
        }

        if (!force && freehandPoints.Count > 0)
        {
            float minDistance = Mathf.Max(0f, assetSpacing);
            Vector3 lastPoint = freehandPoints[freehandPoints.Count - 1];

            if ((point - lastPoint).sqrMagnitude < minDistance * minDistance)
            {
                return;
            }
        }

        point.y = placementY;
        freehandPoints.Add(point);
    }

    private void AddConsecutiveFreehandPoint(Vector3 point, bool force)
    {
        point.y = placementY;

        if (freehandPathClosed)
        {
            return;
        }

        if (freehandPoints.Count == 0)
        {
            freehandPoints.Add(point);
            return;
        }

        float editRadius = GetConsecutiveFreehandEditRadius();
        Vector3 lastPoint = freehandPoints[freehandPoints.Count - 1];

        if (!force && (point - lastPoint).sqrMagnitude < editRadius * editRadius * 0.25f)
        {
            return;
        }

        if (TryCloseFreehandPath(point, editRadius))
        {
            return;
        }

        if (TryFindExistingFreehandPathHit(point, editRadius, out int segmentIndex, out Vector3 closestPoint))
        {
            TrimFreehandPath(segmentIndex, closestPoint);
            return;
        }

        freehandPoints.Add(point);
    }

    private bool TryCloseFreehandPath(Vector3 point, float radius)
    {
        if (freehandPoints.Count < 4)
        {
            return false;
        }

        Vector3 firstPoint = freehandPoints[0];

        if ((point - firstPoint).sqrMagnitude > radius * radius)
        {
            return false;
        }

        if (GetFreehandPathLength() < radius * 4f)
        {
            return false;
        }

        if ((freehandPoints[freehandPoints.Count - 1] - firstPoint).sqrMagnitude > 0.0001f)
        {
            freehandPoints.Add(firstPoint);
        }

        freehandPathClosed = true;
        return true;
    }

    private bool TryFindExistingFreehandPathHit(Vector3 point, float radius, out int segmentIndex, out Vector3 closestPoint)
    {
        segmentIndex = -1;
        closestPoint = default;

        if (freehandPoints.Count < 4)
        {
            return false;
        }

        float radiusSqr = radius * radius;
        int lastSegmentToCheck = Mathf.Max(0, freehandPoints.Count - 4);
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < lastSegmentToCheck; i++)
        {
            Vector3 candidate = GetClosestPointOnSegment(point, freehandPoints[i], freehandPoints[i + 1]);
            float distanceSqr = (point - candidate).sqrMagnitude;

            if (distanceSqr < radiusSqr && distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                segmentIndex = i;
                closestPoint = candidate;
            }
        }

        return segmentIndex >= 0;
    }

    private void TrimFreehandPath(int segmentIndex, Vector3 endPoint)
    {
        int keepCount = Mathf.Clamp(segmentIndex + 1, 1, freehandPoints.Count);

        if (freehandPoints.Count > keepCount)
        {
            freehandPoints.RemoveRange(keepCount, freehandPoints.Count - keepCount);
        }

        endPoint.y = placementY;

        if ((freehandPoints[freehandPoints.Count - 1] - endPoint).sqrMagnitude > 0.0001f)
        {
            freehandPoints.Add(endPoint);
        }
    }

    private Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float segmentLengthSqr = segment.sqrMagnitude;

        if (segmentLengthSqr <= Mathf.Epsilon)
        {
            return start;
        }

        float t = Vector3.Dot(point - start, segment) / segmentLengthSqr;
        return start + segment * Mathf.Clamp01(t);
    }

    private float GetConsecutiveFreehandEditRadius()
    {
        return Mathf.Max(0.25f, GetShortestPrefabLength() * Mathf.Max(0.01f, minScale) * 0.45f);
    }

    private float GetShortestPrefabLength()
    {
        float shortestLength = float.MaxValue;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null)
            {
                continue;
            }

            shortestLength = Mathf.Min(shortestLength, GetPrefabFootprint(prefabs[i]).length);
        }

        return shortestLength < float.MaxValue ? shortestLength : 1f;
    }

    private bool HasEnoughSpacing(Vector3 position, List<Vector3> placedPositions)
    {
        float spacing = GetPlacementSpacing();

        if (spacing <= 0f)
        {
            return true;
        }

        float spacingSqr = spacing * spacing;

        for (int i = 0; i < placedPositions.Count; i++)
        {
            Vector3 delta = position - placedPositions[i];
            delta.y = 0f;

            if (delta.sqrMagnitude < spacingSqr)
            {
                return false;
            }
        }

        return true;
    }

    private float GetPlacementSpacing()
    {
        if (spawnMode == SpawnMode.Brush)
        {
            return GetBrushPlacementSpacing();
        }

        return assetSpacing;
    }

    private float GetBrushPlacementSpacing()
    {
        int count = Mathf.Max(1, objectCount);
        float spacing = (brushRadius * 2f) / Mathf.Sqrt(count) * 0.75f;
        return Mathf.Max(0.01f, spacing);
    }

    private bool IsTiledPlacementMode()
    {
        return (spawnMode == SpawnMode.Rectangle || spawnMode == SpawnMode.Circle || spawnMode == SpawnMode.Brush) && tiledPlacement;
    }

    private Vector2 GetTiledCellSize()
    {
        float scale = Mathf.Max(0.01f, tiledScale);
        float maxWidth = 0f;
        float maxDepth = 0f;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null || !TryGetPrefabLocalBounds(prefabs[i], out Bounds bounds))
            {
                continue;
            }

            maxWidth = Mathf.Max(maxWidth, bounds.size.x * scale);
            maxDepth = Mathf.Max(maxDepth, bounds.size.z * scale);
        }

        if (maxWidth <= Mathf.Epsilon && maxDepth <= Mathf.Epsilon)
        {
            float spacing = Mathf.Max(0.01f, scale);
            return new Vector2(spacing, spacing);
        }

        if (randomizeSpawnRotation)
        {
            float maxSize = Mathf.Max(maxWidth, maxDepth, Mathf.Epsilon);
            return new Vector2(maxSize, maxSize);
        }

        return new Vector2(Mathf.Max(0.01f, maxWidth), Mathf.Max(0.01f, maxDepth));
    }

    private Vector2 GetTiledCellSize(GameObject prefab, int quarterTurns, float scale)
    {
        if (prefab == null || !TryGetPrefabLocalBounds(prefab, out Bounds bounds))
        {
            float spacing = Mathf.Max(0.01f, scale);
            return new Vector2(spacing, spacing);
        }

        float sizeX = Mathf.Max(0.01f, bounds.size.x * scale);
        float sizeZ = Mathf.Max(0.01f, bounds.size.z * scale);

        if ((quarterTurns & 1) == 1)
        {
            return new Vector2(sizeZ, sizeX);
        }

        return new Vector2(sizeX, sizeZ);
    }

    private Quaternion GetTiledRotation()
    {
        return GetTiledRotation(out _);
    }

    private Quaternion GetTiledRotation(out int quarterTurns)
    {
        if (!randomizeSpawnRotation)
        {
            quarterTurns = 0;
            return Quaternion.identity;
        }

        quarterTurns = Random.Range(0, 4);
        return Quaternion.Euler(0f, quarterTurns * 90f, 0f);
    }

    private bool IsTileInsideCircle(Vector3 center, Vector2 tileSize)
    {
        Vector3 circleCenter = GetCircleCenter();
        float radius = GetCircleRadius();
        Vector3 halfExtents = new Vector3(tileSize.x * 0.5f, 0f, tileSize.y * 0.5f);

        Vector3[] corners =
        {
            center + new Vector3(-halfExtents.x, 0f, -halfExtents.z),
            center + new Vector3(-halfExtents.x, 0f, halfExtents.z),
            center + new Vector3(halfExtents.x, 0f, -halfExtents.z),
            center + new Vector3(halfExtents.x, 0f, halfExtents.z)
        };

        float radiusSqr = radius * radius;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 offset = corners[i] - circleCenter;
            offset.y = 0f;
            if (offset.sqrMagnitude > radiusSqr)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTileInsideBrush(Vector3 center, Vector2 tileSize, Vector3 brushCenter)
    {
        float radius = brushRadius;
        Vector3 halfExtents = new Vector3(tileSize.x * 0.5f, 0f, tileSize.y * 0.5f);

        Vector3[] corners =
        {
            center + new Vector3(-halfExtents.x, 0f, -halfExtents.z),
            center + new Vector3(-halfExtents.x, 0f, halfExtents.z),
            center + new Vector3(halfExtents.x, 0f, -halfExtents.z),
            center + new Vector3(halfExtents.x, 0f, halfExtents.z)
        };

        float radiusSqr = radius * radius;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 offset = corners[i] - brushCenter;
            offset.y = 0f;
            if (offset.sqrMagnitude > radiusSqr)
            {
                return false;
            }
        }

        return true;
    }

    private Quaternion GetSpawnRotation(Vector3 tangent, Vector3 up, Vector3 prefabLengthAxis)
    {
        if (!IsConsecutivePlacementMode())
        {
            return up == Vector3.up ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, up);
        }

        Vector3 perimeterDirection = Vector3.ProjectOnPlane(tangent, up);

        if (perimeterDirection.sqrMagnitude <= 0.0001f)
        {
            perimeterDirection = Vector3.ProjectOnPlane(Vector3.forward, up);
        }

        Quaternion forwardRotation = Quaternion.LookRotation(perimeterDirection.normalized, up);

        if (Mathf.Abs(prefabLengthAxis.x) >= Mathf.Abs(prefabLengthAxis.z))
        {
            return forwardRotation * Quaternion.Euler(0f, -90f, 0f);
        }

        return forwardRotation;
    }

    private bool TryProjectToSurface(Vector3 position, out RaycastHit hit)
    {
        Vector3 origin = position + Vector3.up * 500f;
        return Physics.Raycast(origin, Vector3.down, out hit, 1000f, surfaceMask);
    }

    private bool TryBuildGroundMesh(out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = null;
        pivotPosition = Vector3.zero;

        if (!TryCollectCurrentGroundCells(out List<GroundCell> cells, out bool tileable, out float tileSize))
        {
            return false;
        }

        return TryBuildGroundMeshFromCells(cells, tileable, tileSize, out mesh, out pivotPosition);
    }

    private bool TryBuildRectangleGroundMesh(out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Rectangle Mesh";

        Vector3 min = Vector3.Min(dragStart, dragEnd);
        Vector3 max = Vector3.Max(dragStart, dragEnd);
        pivotPosition = new Vector3((min.x + max.x) * 0.5f, placementY, (min.z + max.z) * 0.5f);
        Vector3 half = new Vector3((max.x - min.x) * 0.5f, 0f, (max.z - min.z) * 0.5f);

        if (half.x <= Mathf.Epsilon || half.z <= Mathf.Epsilon)
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = null;
            return false;
        }

        mesh.vertices = new[]
        {
            new Vector3(-half.x, 0f, -half.z),
            new Vector3(-half.x, 0f, half.z),
            new Vector3(half.x, 0f, half.z),
            new Vector3(half.x, 0f, -half.z)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = rectangleGroundTileable
            ? new[]
            {
                Vector2.zero,
                new Vector2(0f, (max.z - min.z) / Mathf.Max(0.01f, rectangleGroundTileSize)),
                new Vector2((max.x - min.x) / Mathf.Max(0.01f, rectangleGroundTileSize), (max.z - min.z) / Mathf.Max(0.01f, rectangleGroundTileSize)),
                new Vector2((max.x - min.x) / Mathf.Max(0.01f, rectangleGroundTileSize), 0f)
            }
            : new[]
            {
                Vector2.zero,
                Vector2.up,
                Vector2.one,
                Vector2.right
            };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool TryBuildCircleGroundMesh(out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Circle Mesh";

        float radius = GetCircleRadius();
        if (radius <= Mathf.Epsilon)
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = null;
            pivotPosition = Vector3.zero;
            return false;
        }

        pivotPosition = GetCircleCenter();
        int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 8f), 24, 96);
        List<Vector3> vertices = new List<Vector3>(segments + 1);
        List<Vector2> uvs = new List<Vector2>(segments + 1);
        List<int> triangles = new List<int>(segments * 3);

        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));
        float tileSize = Mathf.Max(0.01f, circleGroundTileSize);
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, 0f, z));
            if (circleGroundTileable)
            {
                uvs.Add(new Vector2((x / tileSize) + 0.5f, (z / tileSize) + 0.5f));
            }
            else
            {
                uvs.Add(new Vector2((x / (radius * 2f)) + 0.5f, (z / (radius * 2f)) + 0.5f));
            }

            if (i < segments)
            {
                triangles.Add(0);
                triangles.Add(i + 2);
                triangles.Add(i + 1);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool TryBuildFreehandGroundMesh(out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = null;

        List<Vector3> points = GetGroundFreehandPoints();
        if (points.Count < 2)
        {
            return TryBuildFreehandFallbackMesh(out mesh, out pivotPosition);
        }

        if (freehandPathClosed && points.Count >= 3)
        {
            return TryBuildClosedFreehandMesh(points, out mesh, out pivotPosition);
        }

        return TryBuildFreehandStripMesh(points, out mesh, out pivotPosition);
    }

    private List<Vector3> GetGroundFreehandPoints()
    {
        List<Vector3> points = new List<Vector3>(freehandPoints.Count);
        for (int i = 0; i < freehandPoints.Count; i++)
        {
            Vector3 point = freehandPoints[i];
            point.y = placementY;
            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.0001f)
            {
                points.Add(point);
            }
        }

        if (points.Count > 1 && (points[0] - points[points.Count - 1]).sqrMagnitude < 0.0001f)
        {
            points[points.Count - 1] = points[0];
            freehandPathClosed = true;
        }

        if (points.Count == 0)
        {
            points.Add(dragStart);
        }

        return points;
    }

    private bool TryBuildFreehandFallbackMesh(out Mesh mesh, out Vector3 pivotPosition)
    {
        mesh = new Mesh();
        mesh.name = "Ground Freehand Fallback Mesh";
        pivotPosition = dragStart;
        float half = Mathf.Max(0.1f, freehandGroundWidth) * 0.5f;

        mesh.vertices = new[]
        {
            new Vector3(-half, 0f, -half),
            new Vector3(-half, 0f, half),
            new Vector3(half, 0f, half),
            new Vector3(half, 0f, -half)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new[]
        {
            Vector2.zero,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(1f, 0f)
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool TryBuildFreehandStripMesh(List<Vector3> points, out Mesh mesh, out Vector3 pivotPosition)
    {
        if (points == null || points.Count < 2)
        {
            return TryBuildFreehandFallbackMesh(out mesh, out pivotPosition);
        }

        float width = Mathf.Max(0.1f, freehandGroundWidth);
        List<Vector3> left = new List<Vector3>();
        List<Vector3> right = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 tangent;
            if (i == 0)
            {
                tangent = (points[1] - points[0]).normalized;
            }
            else if (i == points.Count - 1)
            {
                tangent = (points[i] - points[i - 1]).normalized;
            }
            else
            {
                Vector3 a = (points[i] - points[i - 1]).normalized;
                Vector3 b = (points[i + 1] - points[i]).normalized;
                tangent = (a + b).normalized;
                if (tangent.sqrMagnitude <= Mathf.Epsilon)
                {
                    tangent = b.sqrMagnitude > Mathf.Epsilon ? b : a;
                }
            }

            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                tangent = Vector3.forward;
            }

            Vector3 normal = Vector3.Cross(Vector3.up, tangent).normalized;
            if (normal.sqrMagnitude <= Mathf.Epsilon)
            {
                normal = Vector3.right;
            }

            left.Add(points[i] - normal * (width * 0.5f));
            right.Add(points[i] + normal * (width * 0.5f));
        }

        List<Vector3> vertices = new List<Vector3>(points.Count * 2);
        List<int> triangles = new List<int>((points.Count - 1) * 6);

        Bounds bounds = new Bounds(points[0], Vector3.zero);
        for (int i = 0; i < points.Count; i++)
        {
            bounds.Encapsulate(left[i]);
            bounds.Encapsulate(right[i]);
        }

        pivotPosition = bounds.center;
        List<Vector2> uvs = new List<Vector2>(points.Count * 2);
        for (int i = 0; i < points.Count; i++)
        {
            vertices.Add(left[i] - pivotPosition);
            vertices.Add(right[i] - pivotPosition);
        }

        float stripWidth = Mathf.Max(0.01f, freehandGroundWidth);
        float minX = bounds.min.x;
        float minZ = bounds.min.z;
        float sizeX = Mathf.Max(0.01f, bounds.size.x);
        float sizeZ = Mathf.Max(0.01f, bounds.size.z);
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 leftWorld = left[i];
            Vector3 rightWorld = right[i];
            if (freehandGroundTileable)
            {
                float tileSize = Mathf.Max(0.01f, freehandGroundTileSize);
                uvs.Add(new Vector2(leftWorld.x / tileSize, leftWorld.z / tileSize));
                uvs.Add(new Vector2(rightWorld.x / tileSize, rightWorld.z / tileSize));
            }
            else
            {
                uvs.Add(new Vector2((leftWorld.x - minX) / sizeX, (leftWorld.z - minZ) / sizeZ));
                uvs.Add(new Vector2((rightWorld.x - minX) / sizeX, (rightWorld.z - minZ) / sizeZ));
            }
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            int baseIndex = i * 2;
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);

            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        mesh = new Mesh();
        mesh.name = "Ground Freehand Strip Mesh";
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private bool TryBuildClosedFreehandMesh(List<Vector3> points, out Mesh mesh, out Vector3 pivotPosition)
    {
        List<Vector2> polygon = new List<Vector2>();
        for (int i = 0; i < points.Count; i++)
        {
            polygon.Add(new Vector2(points[i].x, points[i].z));
        }

        if (polygon.Count >= 2 && (polygon[0] - polygon[polygon.Count - 1]).sqrMagnitude < 0.0001f)
        {
            polygon.RemoveAt(polygon.Count - 1);
        }

        if (polygon.Count < 3)
        {
            return TryBuildFreehandFallbackMesh(out mesh, out pivotPosition);
        }

        List<int> triangles = TriangulatePolygon(polygon);
        if (triangles.Count == 0)
        {
            return TryBuildFreehandFallbackMesh(out mesh, out pivotPosition);
        }

        Bounds bounds = new Bounds(new Vector3(polygon[0].x, placementY, polygon[0].y), Vector3.zero);
        for (int i = 0; i < polygon.Count; i++)
        {
            bounds.Encapsulate(new Vector3(polygon[i].x, placementY, polygon[i].y));
        }

        pivotPosition = bounds.center;
        List<Vector3> vertices = new List<Vector3>(polygon.Count);
        List<Vector2> uvs = new List<Vector2>(polygon.Count);
        float sizeX = Mathf.Max(0.01f, bounds.size.x);
        float sizeZ = Mathf.Max(0.01f, bounds.size.z);
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3 world = new Vector3(polygon[i].x, placementY, polygon[i].y);
            vertices.Add(world - pivotPosition);
            if (freehandGroundTileable)
            {
                float tileSize = Mathf.Max(0.01f, freehandGroundTileSize);
                uvs.Add(new Vector2(world.x / tileSize, world.z / tileSize));
            }
            else
            {
                uvs.Add(new Vector2((world.x - bounds.min.x) / sizeX, (world.z - bounds.min.z) / sizeZ));
            }
        }

        mesh = new Mesh();
        mesh.name = "Ground Freehand Polygon Mesh";
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return true;
    }

    private List<int> TriangulatePolygon(List<Vector2> polygon)
    {
        List<int> indices = new List<int>();
        if (polygon.Count < 3)
        {
            return indices;
        }

        List<int> remaining = new List<int>();
        for (int i = 0; i < polygon.Count; i++)
        {
            remaining.Add(i);
        }

        if (SignedPolygonArea(polygon) < 0f)
        {
            remaining.Reverse();
        }

        int guard = 0;
        while (remaining.Count > 3 && guard < 5000)
        {
            bool earFound = false;
            for (int i = 0; i < remaining.Count; i++)
            {
                int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                int curr = remaining[i];
                int next = remaining[(i + 1) % remaining.Count];

                if (!IsConvexVertex(polygon[prev], polygon[curr], polygon[next]))
                {
                    continue;
                }

                bool containsPoint = false;
                for (int j = 0; j < remaining.Count; j++)
                {
                    int test = remaining[j];
                    if (test == prev || test == curr || test == next)
                    {
                        continue;
                    }

                    if (PointInTriangle(polygon[test], polygon[prev], polygon[curr], polygon[next]))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (containsPoint)
                {
                    continue;
                }

                indices.Add(prev);
                indices.Add(curr);
                indices.Add(next);
                remaining.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                break;
            }

            guard++;
        }

        if (remaining.Count == 3)
        {
            indices.Add(remaining[0]);
            indices.Add(remaining[1]);
            indices.Add(remaining[2]);
        }

        return indices;
    }

    private float SignedPolygonArea(List<Vector2> polygon)
    {
        float area = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            area += (a.x * b.y) - (b.x * a.y);
        }

        return area * 0.5f;
    }

    private bool IsConvexVertex(Vector2 prev, Vector2 curr, Vector2 next)
    {
        return Cross2D(curr - prev, next - curr) >= 0f;
    }

    private float Cross2D(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float ab = Cross2D(b - a, point - a);
        float bc = Cross2D(c - b, point - b);
        float ca = Cross2D(a - c, point - c);
        bool hasNeg = (ab < 0f) || (bc < 0f) || (ca < 0f);
        bool hasPos = (ab > 0f) || (bc > 0f) || (ca > 0f);
        return !(hasNeg && hasPos);
    }

    private Vector3 GetRectangleCenter()
    {
        return (dragStart + dragEnd) * 0.5f;
    }

    private Vector3 GetCircleCenter()
    {
        return (dragStart + dragEnd) * 0.5f;
    }

    private Vector3 GetRectangleSize()
    {
        Vector3 delta = dragEnd - dragStart;
        return new Vector3(Mathf.Abs(delta.x), 0f, Mathf.Abs(delta.z));
    }

    private float GetCircleRadius()
    {
        Vector3 flatDelta = dragEnd - dragStart;
        flatDelta.y = 0f;
        return flatDelta.magnitude * 0.5f;
    }

    private static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        string[] layerNames = UnityEditorInternal.InternalEditorUtility.layers;
        int maskWithoutEmpty = 0;

        for (int i = 0; i < layerNames.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layerNames[i]);
            if ((layerMask.value & (1 << layer)) != 0)
            {
                maskWithoutEmpty |= 1 << i;
            }
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layerNames);
        int mask = 0;

        for (int i = 0; i < layerNames.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
            {
                mask |= 1 << LayerMask.NameToLayer(layerNames[i]);
            }
        }

        layerMask.value = mask;
        return layerMask;
    }
}
