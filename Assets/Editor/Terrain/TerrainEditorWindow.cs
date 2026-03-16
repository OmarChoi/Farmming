using UnityEditor;
using UnityEngine;

public class TerrainEditorWindow : EditorWindow
{
    private enum BrushMode { Dirt, Tree, Rock, Eraser }

    private TerrainGridManager _gridManager;
    private TerrainBrush _brush = new();
    private BrushMode _brushMode = BrushMode.Dirt;
    private bool _isPainting;
    
    private const float MaxRaycastDistance = 500f;
    
    [MenuItem("Tools/Terrain Editor")]
    public static void Open()
    {
        GetWindow<TerrainEditorWindow>("Terrain Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 그리드 매니저 참조
        _gridManager = (TerrainGridManager)EditorGUILayout.ObjectField(
            "Grid Manager", _gridManager, typeof(TerrainGridManager), true);

        if (_gridManager == null)
        {
            EditorGUILayout.HelpBox("씬에서 TerrainGridManager를 할당하세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        GUILayout.Label("브러시 모드", EditorStyles.boldLabel);

        // 브러시 모드 선택
        _brushMode = (BrushMode)GUILayout.Toolbar((int)_brushMode,
            new[] { "흙 블록", "나무", "돌", "지우기" });

        EditorGUILayout.Space();

        switch (_brushMode)
        {
            case BrushMode.Dirt:
                _brush.IsEraser = false;
                _brush.CellType = ECellType.Dirt;
                _brush.ObjectType = EGridObjectType.None;
                _brush.DirtLevel = EditorGUILayout.IntSlider("흙 레벨", _brush.DirtLevel, 1, 5);
                break;

            case BrushMode.Tree:
                _brush.IsEraser = false;
                _brush.CellType = ECellType.Dirt;
                _brush.ObjectType = EGridObjectType.Tree;
                _brush.ObjectLevel = EditorGUILayout.IntSlider("나무 레벨", _brush.ObjectLevel, 1, 5);
                _brush.DirtLevel = EditorGUILayout.IntSlider("흙 레벨", _brush.DirtLevel, 1, 5);
                break;

            case BrushMode.Rock:
                _brush.IsEraser = false;
                _brush.CellType = ECellType.Dirt;
                _brush.ObjectType = EGridObjectType.Rock;
                _brush.ObjectLevel = EditorGUILayout.IntSlider("돌 레벨", _brush.ObjectLevel, 1, 5);
                _brush.DirtLevel = EditorGUILayout.IntSlider("흙 레벨", _brush.DirtLevel, 1, 5);
                break;

            case BrushMode.Eraser:
                _brush.IsEraser = true;
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "씬 뷰에서 좌클릭으로 페인팅합니다.\n" +
            "기존 블록 위를 클릭하면 위에 쌓입니다.\n" +
            "빈 바닥을 클릭하면 y=0에 배치됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("전체 삭제"))
        {
            if (EditorUtility.DisplayDialog("확인", "모든 셀을 삭제하시겠습니까?", "삭제", "취소"))
            {
                Undo.RegisterCompleteObjectUndo(_gridManager, "Clear All Cells");
                _gridManager.ClearAll();
                EditorUtility.SetDirty(_gridManager);
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_gridManager == null) return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Raycast로 마우스 위치 계산
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3Int gridPos;
        bool hasTarget = TryGetGridPosition(ray, out gridPos);

        // 와이어 프리뷰 표시
        if (hasTarget)
        {
            float size = _gridManager.CellSize;
            Vector3 worldPos = _gridManager.GridToWorld(gridPos);
            Vector3 center = worldPos + new Vector3(0, size * 0.5f, 0);

            Handles.color = _brush.IsEraser ? Color.red : Color.green;
            Handles.DrawWireCube(center, Vector3.one * size);
        }

        // 마우스 입력 처리
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            _isPainting = true;
            if (hasTarget) Paint(gridPos);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && _isPainting)
        {
            if (hasTarget) Paint(gridPos);
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            _isPainting = false;
            e.Use();
        }

        sceneView.Repaint();
    }

    private bool TryGetGridPosition(Ray ray, out Vector3Int gridPos)
    {
        gridPos = Vector3Int.zero;

        // 1. 기존 블록에 hit하면 그 위에 쌓기
        if (Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance))
        {
            TerrainCell cell = hit.collider.GetComponentInParent<TerrainCell>();
            if (cell != null)
            {
                // hit.normal 방향으로 한 칸 이동 → 쌓기
                Vector3 neighborWorld = _gridManager.GridToWorld(cell.GridPosition)
                    + hit.normal * _gridManager.CellSize;
                gridPos = _gridManager.WorldToGrid(neighborWorld);

                // 지우기 모드면 hit된 블록 자체를 대상으로
                if (_brush.IsEraser)
                    gridPos = cell.GridPosition;

                return true;
            }
        }

        // 2. 빈 바닥 → y=0 평면에 배치
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 point = ray.GetPoint(dist);
            gridPos = _gridManager.WorldToGrid(point);
            gridPos.y = 0;
            return true;
        }

        return false;
    }

    private void Paint(Vector3Int gridPos)
    {
        if (_brush.IsEraser)
        {
            if (_gridManager.GetCell(gridPos) != null)
            {
                Undo.RegisterCompleteObjectUndo(_gridManager, "Erase Cell");
                _gridManager.RemoveCell(gridPos);
                EditorUtility.SetDirty(_gridManager);
            }
        }
        else
        {
            Undo.RegisterCompleteObjectUndo(_gridManager, "Paint Cell");
            TerrainCellData data = _brush.CreateCellData();
            _gridManager.SetCell(gridPos, data);
            EditorUtility.SetDirty(_gridManager);
        }
    }
}