using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerBuildingAbility : PlayerAbility
{
    private enum BuildState { None, Previewing }

    [Header("참조")]
    [SerializeField] private BuildingManager _buildingManager;
    [SerializeField] private BuildingDataSO _buildingData;

    [Header("Ghost 설정")]
    [SerializeField] private Material _ghostMaterial;
    [SerializeField] private Color _ghostValidColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color _ghostInvalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("키 설정")]
    [SerializeField] private KeyCode _placeKey = KeyCode.B;
    [SerializeField] private KeyCode _removeKey = KeyCode.V;
    [SerializeField] private KeyCode _rotateKey = KeyCode.R;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    private PlayerTerrainAbility _terrainAbility;
    private bool _swapped;
    private BuildState _state = BuildState.None;
    private BuildingGhost _ghost;
    private Vector3Int _prevGridPos;
    private bool _isLoadingPrefab;
    private bool _isBuilding;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void Update()
    {
        if (!_owner.CanMove) return;

        switch (_state)
        {
            case BuildState.None:
                HandleNoneState();
                break;
            case BuildState.Previewing:
                HandlePreviewingState();
                break;
        }
    }

    private void HandleNoneState()
    {
        if (Input.GetKeyDown(_removeKey))
        {
            TryRemove();
            return;
        }
        
        if (Input.GetKeyDown(_placeKey) && !_isLoadingPrefab)
        {
            EnterPreviewAsync().Forget();
        }
    }

    private void HandlePreviewingState()
    {
        // Cancel
        if (Input.GetKeyDown(_cancelKey))
        {
            CancelPreview();
            return;
        }

        // Swap toggle
        if (Input.GetKeyDown(_rotateKey))
        {
            ToggleSwap();
            RefreshGhost();
        }

        // Confirm
        if (Input.GetKeyDown(_placeKey) && !_isBuilding)
        {
            TryConfirmAsync().Forget();
            return;
        }

        // 셀 위치 변경 감지
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null)
        {
            _ghost.SetVisible(false);
            return;
        }

        _ghost.SetVisible(true);

        if (cell.GridPosition != _prevGridPos)
        {
            _prevGridPos = cell.GridPosition;
            RefreshGhost();
        }
    }

    private async UniTaskVoid EnterPreviewAsync()
    {
        if (_buildingData == null) return;

        // 기존 Ghost가 있으면 재사용
        if (_ghost?.Instance != null)
        {
            _state = BuildState.Previewing;
            _swapped = false;
            _ghost.SetVisible(true);
            RefreshGhostInitial();
            return;
        }

        string prefabKey = AssetKey.Building.GetKey(_buildingData.BuildingId);
        if (string.IsNullOrEmpty(prefabKey)) return;

        _isLoadingPrefab = true;
        var prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
        _isLoadingPrefab = false;

        if (prefab == null || _state != BuildState.None) return;

        _state = BuildState.Previewing;
        _swapped = false;
        _ghost = new BuildingGhost();
        _ghost.Spawn(prefab, _ghostMaterial, _ghostValidColor, _ghostInvalidColor);
        RefreshGhostInitial();
    }

    private void RefreshGhostInitial()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell != null)
        {
            _prevGridPos = cell.GridPosition;
            RefreshGhost();
        }
        else
        {
            _ghost.SetVisible(false);
        }
    }

    private void CancelPreview()
    {
        _ghost?.SetVisible(false);
        _state = BuildState.None;
    }

    private void DestroyGhost()
    {
        _ghost?.Destroy();
        _ghost = null;
        _state = BuildState.None;
    }

    private async UniTaskVoid TryConfirmAsync()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        int direction = BuildingPlacer.GetDirection(_owner.transform.forward);
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(_buildingData, direction, _swapped);

        if (!_buildingManager.CanPlace(cell.GridPosition, footprint, out _)) return;

        _isBuilding = true;
        var request = new BuildingRequest
        {
            Data = _buildingData,
            AnchorPos = cell.GridPosition,
            Direction = direction,
            Swapped = _swapped
        };
        await _buildingManager.TryBuild(request);
        _isBuilding = false;
        if (_state != BuildState.Previewing) return;
        DestroyGhost();
    }

    private void RefreshGhost()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        int direction = BuildingPlacer.GetDirection(_owner.transform.forward);
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(_buildingData, direction, _swapped);

        // 위치 계산
        bool canPlace = _buildingManager.CanPlace(cell.GridPosition, footprint, out int baseY);
        var anchorPos = new Vector3Int(cell.GridPosition.x, baseY >= 0 ? baseY : cell.GridPosition.y, cell.GridPosition.z);
        Vector3 spawnPos = _buildingManager.CalculateSpawnPos(anchorPos, footprint);

        float yRot = footprint.Direction * 90f + (_swapped ? 90f : 0f);
        _ghost.UpdateTransform(spawnPos, Quaternion.Euler(0f, yRot, 0f));
        _ghost.SetValid(canPlace);
    }

    private void ToggleSwap()
    {
        if (_buildingData == null) return;
        _swapped = !_swapped;
    }

    private void TryRemove()
    {
        var cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _buildingManager.TryRemove(cell.GridPosition);
    }
}
