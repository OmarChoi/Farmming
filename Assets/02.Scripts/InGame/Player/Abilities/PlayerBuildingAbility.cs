using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerBuildingAbility : PlayerAbility
{
    private enum BuildState { None, Previewing }

    [Header("참조")]
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

    private BuildingManager _buildingManager;
    private PlayerTerrainAbility _terrainAbility;
    private PlayerInventoryAbility _inventoryAbility;
    private bool _swapped;
    private BuildState _state = BuildState.None;
    private BuildingGhost _ghost;
    private Vector3Int _prevGridPos;
    private int _prevDirection = -1;
    private bool _isLoadingPrefab;
    private bool _isBuilding;
    private bool _hasResourcesCached;
    private bool _hasResourcesDirty = true;

    protected override void Awake()
    {
        base.Awake();
        _buildingManager = BuildingManager.Instance;
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
        _inventoryAbility = _owner.GetAbility<PlayerInventoryAbility>();
    }

    private void OnEnable()
    {
        _buildingManager.OnBuildingSelected += OnBuildingSelectedFromUi;
        if (_inventoryAbility != null)
        {
            _inventoryAbility.OnSlotChanged += OnInventoryChanged;
        }
    }

    private void OnDisable()
    {
        _buildingManager.OnBuildingSelected -= OnBuildingSelectedFromUi;
        if (_inventoryAbility != null)
        {
            _inventoryAbility.OnSlotChanged -= OnInventoryChanged;
        }

        // UI_BuildingList의 OnClosed 구독 해제
        if (UIController.Instance != null)
        {
            var ui = UIController.Instance.GetInstance<UI_BuildingList>();
            if (ui != null) ui.OnClosed -= OnBuildingListClosed;
        }
    }

    private void OnInventoryChanged(int slotIndex)
    {
        _hasResourcesDirty = true;
    }

    private void Update()
    {
        if (!_owner.IsMine) return;
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
            if (UIController.Instance != null)
            {
                OpenBuildingSelectionUi();
            }
            else
            {
                EnterPreviewAsync().Forget();
            }
        }
    }

    private void SetBuildingData(BuildingDataSO buildingData)
    {
        if (_buildingData == buildingData) return;

        _buildingData = buildingData;
        _hasResourcesDirty = true;

        if (_state == BuildState.Previewing)
        {
            DestroyGhost();
        }
    }

    private void OpenBuildingSelectionUi()
    {
        if (UIController.Instance == null) return;
        _owner.EnterUIMode();
        UIController.Instance.OpenAsync<UI_BuildingList>(ui =>
        {
            ui.OnClosed -= OnBuildingListClosed;
            ui.OnClosed += OnBuildingListClosed;
        }).Forget();
    }

    private void OnBuildingListClosed()
    {
        _owner.ExitUIMode();
    }

    private void OnBuildingSelectedFromUi(BuildingDataSO buildingData)
    {
        SetBuildingData(buildingData);

        if (_isLoadingPrefab) return;

        EnterPreviewAsync().Forget();
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
        int direction = BuildingPlacer.GetDirection(_owner.transform.forward);

        if (cell.GridPosition != _prevGridPos || direction != _prevDirection)
        {
            _prevGridPos = cell.GridPosition;
            _prevDirection = direction;
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
            UIController.Instance?.OpenAsync<UI_BuildInfo>().Forget();
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

        // 건물 정보 패널 표시
        UIController.Instance?.OpenAsync<UI_BuildInfo>().Forget();
    }

    private void RefreshGhostInitial()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell != null)
        {
            _prevGridPos = cell.GridPosition;
            _prevDirection = BuildingPlacer.GetDirection(_owner.transform.forward);
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
        _prevDirection = -1;
        _buildingManager.ClearSelection();
        UIController.Instance?.CloseAsync<UI_BuildInfo>().Forget();
    }

    private void DestroyGhost()
    {
        _ghost?.Destroy();
        _ghost = null;
        _state = BuildState.None;
        _prevDirection = -1;
        _buildingManager.ClearSelection();
        UIController.Instance?.CloseAsync<UI_BuildInfo>().Forget();
    }

    private async UniTaskVoid TryConfirmAsync()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        int direction = BuildingPlacer.GetDirection(_owner.transform.forward);
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(cell.GridPosition, _buildingData, direction, _swapped);

        if (!preview.CanPlace) return;

        // 자원 검증 + 소모를 원자적으로 처리
        if (!TryConsumeResources(_buildingData)) return;
        _hasResourcesDirty = true;

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
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(cell.GridPosition, _buildingData, direction, _swapped);

        _ghost.UpdateTransform(preview.SpawnPosition, preview.Rotation);

        if (_hasResourcesDirty)
        {
            _hasResourcesCached = HasResources(_buildingData);
            _hasResourcesDirty = false;
        }

        _ghost.SetValid(preview.CanPlace && _hasResourcesCached);
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

    // 건설에 필요한 모든 자원이 인벤토리에 충분한지 확인
    private bool HasResources(BuildingDataSO data)
    {
        if (_inventoryAbility == null) return false;
        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        foreach (BuildingCostEntry cost in costs)
        {
            if (cost.Item == null) continue;
            if (_inventoryAbility.GetItemCount(cost.Item) < cost.Amount) return false;
        }
        return true;
    }

    // 모든 자원 보유를 먼저 검증한 뒤, 통과 시 한 번에 차감
    private bool TryConsumeResources(BuildingDataSO data)
    {
        if (!HasResources(data)) return false;

        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        foreach (BuildingCostEntry cost in costs)
        {
            if (cost.Item == null) continue;
            _inventoryAbility.RemoveItem(cost.Item, cost.Amount);
        }
        return true;
    }
}
