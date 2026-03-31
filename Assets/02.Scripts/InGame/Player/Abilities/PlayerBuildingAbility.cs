using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerBuildingAbility : PlayerAbility
{
    private enum BuildState
    {
        None,
        Previewing
    }

    // ── Inspector 설정 ──────────────────────────────────
    [Header("Ghost 설정")]
    [SerializeField] private Material _ghostMaterial;
    [SerializeField] private Color _ghostValidColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color _ghostInvalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("키 설정")]
    [SerializeField] private KeyCode _placeKey = KeyCode.B;
    [SerializeField] private KeyCode _removeKey = KeyCode.V;
    [SerializeField] private KeyCode _rotateKey = KeyCode.R;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    // ── 외부 참조 ───────────────────────────────────────
    private BuildingManager _buildingManager;
    private PlayerTerrainAbility _terrainAbility;
    private PlayerInventoryAbility _inventoryAbility;

    // ── 건설 상태 ───────────────────────────────────────
    private BuildState _state = BuildState.None;
    private BuildingDataSO _buildingData;
    private int _rotationQuarterTurns;
    private bool _isBuilding;

    // ── Ghost(미리보기 모델) 관련 ────────────────────────
    private BuildingGhost _ghost;
    private string _ghostBuildingId;
    private Vector3Int _prevGridPos;

    // ── 건설 자원 캐시 ──────────────────────────────────
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
        RefreshBuildInfoUI();
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

        if (Input.GetKeyDown(_placeKey))
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

        if (_ghost?.Instance != null && _ghostBuildingId != _buildingData?.BuildingId)
        {
            DestroyGhost(clearSelection: false);
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

        // Rotate Preview
        if (Input.GetKeyDown(_rotateKey))
        {
            RotatePreviewClockwise();
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
        if (cell.GridPosition != _prevGridPos || _hasResourcesDirty)
        {
            _prevGridPos = cell.GridPosition;
            RefreshGhost();
        }
    }

    private async UniTaskVoid EnterPreviewAsync()
    {
        BuildingDataSO requestedBuilding = _buildingData;
        if (requestedBuilding == null) return;

        // 기존 Ghost가 있으면 재사용
        if (_ghost?.Instance != null && _ghostBuildingId == requestedBuilding.BuildingId)
        {
            _state = BuildState.Previewing;
            _rotationQuarterTurns = BuildingPlacer.GetDirection(_owner.transform.forward);
            _ghost.SetVisible(true);
            RefreshGhostInitial();
            OpenBuildInfoUI();
            return;
        }

        string prefabKey = AssetKey.Building.GetKey(requestedBuilding.BuildingId);
        if (string.IsNullOrEmpty(prefabKey)) return;

        var prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
        if (requestedBuilding != _buildingData || !isActiveAndEnabled || _state != BuildState.None) return;

        if (prefab == null) return;

        _state = BuildState.Previewing;
        _rotationQuarterTurns = BuildingPlacer.GetDirection(_owner.transform.forward);
        _ghost = new BuildingGhost();
        _ghost.Spawn(prefab, _ghostMaterial, _ghostValidColor, _ghostInvalidColor);
        _ghostBuildingId = requestedBuilding.BuildingId;
        RefreshGhostInitial();

        // 건물 정보 패널 표시
        OpenBuildInfoUI();
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
        _buildingManager.ClearSelection();
        UIController.Instance?.CloseAsync<UI_BuildInfo>().Forget();
    }

    private void DestroyGhost(bool clearSelection = true)
    {
        _ghost?.Destroy();
        _ghost = null;
        _ghostBuildingId = null;
        _state = BuildState.None;
        if (clearSelection)
        {
            _buildingManager.ClearSelection();
        }
        UIController.Instance?.CloseAsync<UI_BuildInfo>().Forget();
    }

    private async UniTaskVoid TryConfirmAsync()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        int direction = _rotationQuarterTurns;
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(cell.GridPosition, _buildingData, direction, swapped: false);

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
            Swapped = false
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

        int direction = _rotationQuarterTurns;
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(cell.GridPosition, _buildingData, direction, swapped: false);

        _ghost.UpdateTransform(preview.SpawnPosition, preview.Rotation);

        if (_hasResourcesDirty)
        {
            _hasResourcesCached = HasResources(_buildingData);
            _hasResourcesDirty = false;
        }

        _ghost.SetValid(preview.CanPlace && _hasResourcesCached);
    }

    private void RotatePreviewClockwise()
    {
        if (_buildingData == null) return;
        _rotationQuarterTurns = (_rotationQuarterTurns + 1) % 4;
    }

    private void TryRemove()
    {
        var cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        if (!_buildingManager.TryRemove(cell.GridPosition, out BuildingDataSO buildingData)) return;
        if (buildingData == null) return;
        foreach (BuildingCostEntry costItem in buildingData.Costs)
        {
            _inventoryAbility.AddItem(costItem.Item, costItem.Amount);
        }
    }

    private void OpenBuildInfoUI()
    {
        UIController.Instance?.OpenAsync<UI_BuildInfo>(ui =>
        {
            ui.SetOwnedCounts(BuildOwnedCounts(_buildingData));
        }).Forget();
    }

    private void RefreshBuildInfoUI()
    {
        if (_buildingData == null || UIController.Instance == null) return;
        var ui = UIController.Instance.GetInstance<UI_BuildInfo>();
        if (ui == null) return;
        ui.SetOwnedCounts(BuildOwnedCounts(_buildingData));
    }

    private int[] BuildOwnedCounts(BuildingDataSO data)
    {
        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        var counts = new int[costs.Count];
        for (int i = 0; i < costs.Count; i++)
        {
            if (costs[i].Item == null) continue;
            counts[i] = _inventoryAbility.GetItemCount(costs[i].Item);
        }
        return counts;
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