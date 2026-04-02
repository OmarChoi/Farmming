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
    private bool _hasStarted;
    private bool _isBuildingSelectionBound;
    private bool _isBuildingRemoveRefundBound;
    private bool _isInventoryReadyEventBound;
    private bool _isInventorySlotChangedBound;

    // ── 건설 상태 ───────────────────────────────────────
    private BuildState _state = BuildState.None;
    private BuildingDataSO _buildingData;
    private int _rotationQuarterTurns;
    private bool _isBuilding;

    // ── Ghost(미리보기 모델) 관련 ────────────────────────
    private BuildingGhost _ghost;
    private string _ghostBuildingId;
    private Vector3Int _prevGridPos;
    private int _previewRequestVersion;

    // ── 건설 자원 캐시 ──────────────────────────────────
    private bool _hasResourcesCached;
    private bool _hasResourcesDirty = true;

    private bool IsLocalPlayer => _owner != null && _owner.IsMine;

    protected override void Awake()
    {
        base.Awake();
        CacheStaticReferences();
        CacheLocalInventoryAbility();
    }

    private void Start()
    {
        _hasStarted = true;
        BindLocalRuntimeEvents();
    }

    private void OnEnable()
    {
        if (!_hasStarted) return;
        BindLocalRuntimeEvents();
    }

    private void OnDisable()
    {
        UnbindLocalRuntimeEvents();

        // UI_BuildingList의 OnClosed 구독 해제
        if (UIController.Instance != null)
        {
            var ui = UIController.Instance.GetInstance<UI_BuildingList>();
            if (ui != null) ui.OnClosed -= OnBuildingListClosed;
        }

        if (IsLocalPlayer)
        {
            DestroyGhost();
        }
    }

    private void OnInventoryChanged(int slotIndex)
    {
        if (!IsLocalPlayer) return;

        _hasResourcesDirty = true;
        RefreshBuildInfoUIIfOpen();
    }

    private void CacheStaticReferences()
    {
        _buildingManager ??= BuildingManager.Instance;
        if (_owner == null) return;

        _terrainAbility ??= _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void CacheLocalInventoryAbility()
    {
        if (_owner == null) return;
        SetLocalInventoryAbility(_owner.GetAbility<PlayerInventoryAbility>());
    }

    private void SetLocalInventoryAbility(PlayerInventoryAbility inventoryAbility)
    {
        if (inventoryAbility == _inventoryAbility) return;

        if (_inventoryAbility != null && _isInventorySlotChangedBound)
        {
            _inventoryAbility.OnSlotChanged -= OnInventoryChanged;
            _isInventorySlotChangedBound = false;
        }

        _inventoryAbility = inventoryAbility;
    }

    private void BindLocalRuntimeEvents()
    {
        if (!IsLocalPlayer) return;

        CacheStaticReferences();
        CacheLocalInventoryAbility();

        if (_buildingManager != null && !_isBuildingSelectionBound)
        {
            _buildingManager.OnBuildingSelected += OnBuildingSelectedFromUi;
            _isBuildingSelectionBound = true;
        }

        if (_buildingManager != null && !_isBuildingRemoveRefundBound)
        {
            _buildingManager.OnLocalRemoveRefundGranted += OnLocalRemoveRefundGranted;
            _isBuildingRemoveRefundBound = true;
        }

        if (!_isInventoryReadyEventBound)
        {
            PlayerInventoryAbility.OnLocalPlayerReady += OnLocalInventoryReady;
            _isInventoryReadyEventBound = true;
        }

        if (_inventoryAbility != null && !_isInventorySlotChangedBound)
        {
            _inventoryAbility.OnSlotChanged += OnInventoryChanged;
            _isInventorySlotChangedBound = true;
        }
    }

    private void UnbindLocalRuntimeEvents()
    {
        if (_buildingManager != null && _isBuildingSelectionBound)
        {
            _buildingManager.OnBuildingSelected -= OnBuildingSelectedFromUi;
            _isBuildingSelectionBound = false;
        }

        if (_buildingManager != null && _isBuildingRemoveRefundBound)
        {
            _buildingManager.OnLocalRemoveRefundGranted -= OnLocalRemoveRefundGranted;
            _isBuildingRemoveRefundBound = false;
        }

        if (_isInventoryReadyEventBound)
        {
            PlayerInventoryAbility.OnLocalPlayerReady -= OnLocalInventoryReady;
            _isInventoryReadyEventBound = false;
        }

        if (_inventoryAbility != null && _isInventorySlotChangedBound)
        {
            _inventoryAbility.OnSlotChanged -= OnInventoryChanged;
            _isInventorySlotChangedBound = false;
        }
    }

    private void OnLocalInventoryReady(PlayerInventoryAbility inventoryAbility)
    {
        if (!IsLocalPlayer || inventoryAbility == null) return;
        if (inventoryAbility.GetComponentInParent<PlayerController>() != _owner) return;

        SetLocalInventoryAbility(inventoryAbility);
        BindLocalRuntimeEvents();
    }

    private void OnLocalRemoveRefundGranted(BuildingDataSO buildingData)
    {
        if (!IsLocalPlayer || buildingData == null) return;
        if (!TryEnsureLocalInventoryAbility()) return;

        foreach (BuildingCostEntry costItem in buildingData.Costs)
        {
            if (costItem.Item == null || costItem.Amount <= 0) continue;
            _inventoryAbility.AddItem(costItem.Item, costItem.Amount);
        }
    }

    private bool TryEnsureLocalInventoryAbility()
    {
        if (!IsLocalPlayer) return false;
        if (_inventoryAbility != null) return true;

        CacheLocalInventoryAbility();
        if (_inventoryAbility == null) return false;

        BindLocalRuntimeEvents();
        return true;
    }

    private void Update()
    {
        if (!IsLocalPlayer) return;
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
            TryRemoveFrontBuilding();
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
        if (!IsLocalPlayer) return;

        SetBuildingData(buildingData);
        EnterPreviewAsync().Forget();
    }

    private void HandlePreviewingState()
    {
        if (_terrainAbility == null || _ghost == null) return;

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
            TryPlaceSelectedBuilding();
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
        if (!IsLocalPlayer) return;
        CacheStaticReferences();
        int previewRequestVersion = ++_previewRequestVersion;

        BuildingDataSO requestedBuilding = _buildingData;
        if (requestedBuilding == null) return;

        // 기존 Ghost가 있으면 재사용
        if (_ghost?.Instance != null && _ghostBuildingId == requestedBuilding.BuildingId)
        {
            _state = BuildState.Previewing;
            _rotationQuarterTurns = BuildingPlacer.GetDirection(_owner.transform.forward);
            _ghost.SetVisible(true);
            RefreshGhostInitial();
            ShowOrRefreshBuildInfoUI();
            return;
        }

        string prefabKey = AssetKey.Building.GetKey(requestedBuilding.BuildingId);
        if (string.IsNullOrEmpty(prefabKey)) return;

        var prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
        if (previewRequestVersion != _previewRequestVersion
            || requestedBuilding != _buildingData
            || !isActiveAndEnabled
            || _state != BuildState.None) return;

        if (prefab == null) return;

        _state = BuildState.Previewing;
        _rotationQuarterTurns = BuildingPlacer.GetDirection(_owner.transform.forward);
        _ghost = new BuildingGhost();
        _ghost.Spawn(prefab, _ghostMaterial, _ghostValidColor, _ghostInvalidColor);
        _ghostBuildingId = requestedBuilding.BuildingId;
        RefreshGhostInitial();

        // 건물 정보 패널 표시
        ShowOrRefreshBuildInfoUI();
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
        DestroyGhost();
    }

    private void DestroyGhost(bool clearSelection = true)
    {
        _previewRequestVersion++;
        _ghost?.Destroy();
        _ghost = null;
        _ghostBuildingId = null;
        _prevGridPos = default;
        _state = BuildState.None;
        if (clearSelection)
        {
            _buildingManager?.ClearSelection();
        }
        UIController.Instance?.CloseAsync<UI_BuildInfo>().Forget();
    }

    private void TryPlaceSelectedBuilding()
    {
        if (!IsLocalPlayer) return;

        CacheStaticReferences();
        if (_terrainAbility == null || _buildingData == null) return;

        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        int direction = _rotationQuarterTurns;
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(cell.GridPosition, _buildingData, direction);

        if (!preview.CanPlace) return;

        // 자원 검증 + 소모를 원자적으로 처리
        if (!TryConsumeBuildResources(_buildingData)) return;
        _hasResourcesDirty = true;

        var request = new BuildingRequest
        {
            Data = _buildingData,
            AnchorPos = cell.GridPosition,
            Direction = direction
        };
        _buildingManager.RequestBuild(request);
        DestroyGhost();
    }

    private void RefreshGhost()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        int direction = _rotationQuarterTurns;
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(cell.GridPosition, _buildingData, direction);

        _ghost.UpdateTransform(preview.SpawnPosition, preview.Rotation);

        if (_hasResourcesDirty)
        {
            _hasResourcesCached = HasRequiredBuildResources(_buildingData);
            _hasResourcesDirty = false;
        }

        _ghost.SetValid(preview.CanPlace && _hasResourcesCached);
    }

    private void RotatePreviewClockwise()
    {
        if (_buildingData == null) return;
        _rotationQuarterTurns = (_rotationQuarterTurns + 1) % 4;
    }

    private void TryRemoveFrontBuilding()
    {
        if (!IsLocalPlayer) return;

        CacheStaticReferences();
        if (_terrainAbility == null || _buildingManager == null) return;

        var cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _buildingManager.RequestRemove(cell.GridPosition);
    }

    private void ShowOrRefreshBuildInfoUI()
    {
        if (!IsLocalPlayer) return;
        if (_buildingData == null || UIController.Instance == null) return;
        if (!TryEnsureLocalInventoryAbility()) return;
        int[] ownedCounts = BuildRequiredItemOwnedCounts(_buildingData);

        UI_BuildInfo openedUi = UIController.Instance.GetInstance<UI_BuildInfo>();
        if (openedUi != null && openedUi.IsOpen)
        {
            openedUi.SetData(_buildingData, ownedCounts);
            return;
        }

        UIController.Instance?.OpenAsync<UI_BuildInfo>(ui =>
        {
            ui.SetData(_buildingData, ownedCounts);
        }).Forget();
    }

    private void RefreshBuildInfoUIIfOpen()
    {
        if (!IsLocalPlayer) return;
        if (_buildingData == null || UIController.Instance == null) return;
        if (!TryEnsureLocalInventoryAbility()) return;
        var ui = UIController.Instance.GetInstance<UI_BuildInfo>();
        if (ui == null) return;
        if (!ui.IsOpen) return;
        ui.SetData(_buildingData, BuildRequiredItemOwnedCounts(_buildingData));
    }

    private int[] BuildRequiredItemOwnedCounts(BuildingDataSO data)
    {
        if (data == null) return System.Array.Empty<int>();
        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        var counts = new int[costs.Count];
        if (!TryEnsureLocalInventoryAbility()) return counts;

        for (int i = 0; i < costs.Count; i++)
        {
            if (costs[i].Item == null) continue;
            counts[i] = _inventoryAbility.GetItemCount(costs[i].Item);
        }
        return counts;
    }

    // 건설에 필요한 모든 자원이 인벤토리에 충분한지 확인
    private bool HasRequiredBuildResources(BuildingDataSO data)
    {
        if (data == null) return false;
        if (!TryEnsureLocalInventoryAbility()) return false;
        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        foreach (BuildingCostEntry cost in costs)
        {
            if (cost.Item == null) continue;
            if (_inventoryAbility.GetItemCount(cost.Item) < cost.Amount) return false;
        }
        return true;
    }

    // 모든 자원 보유를 먼저 검증한 뒤, 통과 시 한 번에 차감
    private bool TryConsumeBuildResources(BuildingDataSO data)
    {
        if (!IsLocalPlayer || data == null) return false;
        if (!TryEnsureLocalInventoryAbility()) return false;

        if (!HasRequiredBuildResources(data)) return false;

        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        foreach (BuildingCostEntry cost in costs)
        {
            if (cost.Item == null) continue;
            _inventoryAbility.RemoveItem(cost.Item, cost.Amount);
        }
        return true;
    }
}
