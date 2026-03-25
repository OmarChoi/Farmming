using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class NavMeshLinkBuilder : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private TerrainGridManager _gridManager;
    [SerializeField] private NavMeshSurface _navMeshSurface;
    [SerializeField] private Transform _linkRoot;

    [Header("링크 생성 조건")]
    [SerializeField] private int _maxJumpUpStep = 1;          // 몇 칸까지 위로 오를지 결정합니다.
    [SerializeField] private bool _bidirectional = true;      // 위/아래 양방향으로 이동 가능합니다.
    [SerializeField] private float _sampleDistance = 1.0f;    // NavMesh.SamplePosition 반경입니다.
    [SerializeField] private float _linkWidth = 0.4f;

    [Header("링크 포인트 보정")]
    [SerializeField] private float _topOffset = 0.2f;          // 셀 윗면보다 얼마나 위에서 지정할 지 결정합니다.
    [SerializeField] private float _pointVerticalOffset = 0f;  // 수치 보정용입니다.

    private readonly List<NavMeshLink> _links = new();
    private readonly HashSet<string> _createdKeys = new();

    private static readonly Vector2Int[] Directions =
    {
        // 중복 방지용으로 오른쪽/앞만 검사합니다.
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
    };
    private void Start()
    {
        if (_linkRoot == null)
        {
            GameObject root = new GameObject("RuntimeNavMeshLinks");
            root.transform.SetParent(transform);
            _linkRoot = root.transform;
        }
    }

    private void Reset()
    {
        if (_gridManager == null)
        {
            _gridManager = TerrainGridManager.Instance;
        }
        if (_navMeshSurface == null)
        {
            _navMeshSurface = GetComponent<NavMeshSurface>();
        }
    }

    public void RebuildLinks()
    {
        if (_gridManager == null)
        {
            _gridManager = TerrainGridManager.Instance;
        }
        if (_gridManager == null || _navMeshSurface == null)
        {
            Debug.LogWarning("TerrainGridManager 또는 NavMeshSurface가 없습니다.");
            return;
        }

        ClearLinks();

        if (_linkRoot == null)
        {
            GameObject root = new GameObject("RuntimeNavMeshLinks");
            root.transform.SetParent(transform);
            _linkRoot = root.transform;
        }
        TerrainGridData gridData = _gridManager.GetGridData();
        if (gridData == null || gridData.Cells == null) return;

        // top 셀만 뽑습니다.
        List<Vector3Int> topCells = CollectTopCells(gridData);

        // x,z -> topY 맵으로 만듭니다.
        Dictionary<Vector2Int, int> topMap = BuildTopMap(topCells);

        foreach (var pair in topMap)
        {
            Vector2Int currentXZ = pair.Key;
            int currentTopY = pair.Value;

            foreach (var direction in Directions)
            {
                Vector2Int nextXZ = currentXZ + direction;

                if (!topMap.TryGetValue(nextXZ, out int nextTopY)) continue;

                int diff = nextTopY - currentTopY;
                if (diff == 0) continue;

                // 오르는 링크가 필요한 경우만 생성합니다.
                // 예: current=2, next=3 -> diff=1 이면 생성
                if (diff > 0 && diff <= _maxJumpUpStep)
                {
                    TryCreateLinkBetween(
                        new Vector3Int(currentXZ.x, currentTopY, currentXZ.y),
                        new Vector3Int(nextXZ.x, nextTopY, nextXZ.y));
                }
                else if (_bidirectional && diff < 0 && -diff <= _maxJumpUpStep)
                {
                    // 현재가 더 높고, next에서 current로 오를 수 있는 경우도 처리합니다.
                    TryCreateLinkBetween(
                        new Vector3Int(nextXZ.x, nextTopY, nextXZ.y),
                        new Vector3Int(currentXZ.x, currentTopY, currentXZ.y));
                }
            }
        }
    }

    public void ClearLinks()
    {
        if (_linkRoot != null)
        {
            foreach (Transform child in _linkRoot)
            {
                Destroy(child.gameObject);
            }
        }
        _links.Clear();
        _createdKeys.Clear();
    }

    private List<Vector3Int> CollectTopCells(TerrainGridData gridData)
    {
        List<Vector3Int> result = new();

        foreach (var kvp in gridData.Cells)
        {
            Vector3Int pos = kvp.Key;
            TerrainCellData data = kvp.Value;

            if (data != null && data.IsTop)
            {
                result.Add(pos);
            }
        }

        return result;
    }

    private Dictionary<Vector2Int, int> BuildTopMap(List<Vector3Int> topCells)
    {
        Dictionary<Vector2Int, int> map = new();

        foreach (var pos in topCells)
        {
            Vector2Int key = new Vector2Int(pos.x, pos.z);

            if (!map.TryGetValue(key, out int existingY) || pos.y > existingY)
            {
                map[key] = pos.y;
            }
        }

        return map;
    }

    private void TryCreateLinkBetween(Vector3Int lowerCell, Vector3Int upperCell)
    {
        string key = MakeKey(lowerCell, upperCell);
        if (_createdKeys.Contains(key)) return;

        if (!TryGetWalkableTopPoint(lowerCell, out Vector3 startPosition)) return;
        
        if (!TryGetWalkableTopPoint(upperCell, out Vector3 endPosition)) return;

        CreateLink(startPosition, endPosition);
        _createdKeys.Add(key);
    }

    private bool TryGetWalkableTopPoint(Vector3Int cell, out Vector3 sampledPos)
    {
        Vector3 center = _gridManager.GridToWorld(cell);
        float cellSize = _gridManager.CellSize;

        Vector3 candidate = center;
        candidate.y += cellSize * 0.5f + _topOffset + _pointVerticalOffset;

        return TrySampleNavMesh(candidate, out sampledPos);
    }

    private bool TrySampleNavMesh(Vector3 worldPos, out Vector3 sampledPos)
    {
        if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, _sampleDistance, NavMesh.AllAreas))
        {
            sampledPos = hit.position;
            return true;
        }

        sampledPos = default;
        return false;
    }

    private void CreateLink(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        GameObject go = new GameObject($"RuntimeLink_{_links.Count}");
        go.transform.SetParent(_linkRoot, false);

        Vector3 center = (startWorldPosition + endWorldPosition) * 0.5f;
        go.transform.position = center;

        NavMeshLink link = go.AddComponent<NavMeshLink>();
        link.agentTypeID = _navMeshSurface.agentTypeID;
        link.bidirectional = _bidirectional;
        link.width = _linkWidth;
        link.startPoint = startWorldPosition - center;
        link.endPoint = endWorldPosition - center;
        link.UpdateLink();

        _links.Add(link);
    }

    private string MakeKey(Vector3Int a, Vector3Int b)
    {
        return $"{a.x},{a.y},{a.z}->{b.x},{b.y},{b.z}";
    }
}
