using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerBuildSession
{
    private readonly BuildingManager _buildingManager;
    private readonly PlayerController _owner;
    private readonly PlayerBuildResourceTracker _resourceHandler;
    private readonly GhostConfig _ghostConfig;

    private BuildingDataSO _selectedBuilding;
    private int _rotationQuarterTurns;
    private BuildingGhost _ghost;
    private string _ghostBuildingId;
    private Vector3Int _prevGridPos;
    private int _previewRequestVersion;

    public bool IsPreviewing => _ghost != null;
    public BuildingDataSO SelectedBuilding => _selectedBuilding;

    public PlayerBuildSession(
        BuildingManager buildingManager,
        PlayerController owner,
        PlayerBuildResourceTracker resourceHandler,
        GhostConfig ghostConfig)
    {
        _buildingManager = buildingManager;
        _owner = owner;
        _resourceHandler = resourceHandler;
        _ghostConfig = ghostConfig;

        _resourceHandler.OnResourceChanged += OnResourceChanged;
    }

    #region Public API

    public async void OpenSelectionUIAsync()
    {
        if (UIController.Instance == null) return;

        IReadOnlyList<BuildingDataSO> buildings = _buildingManager.AvailableBuildings;
        UI_BuildingList ui = await UIController.Instance.OpenAsync<UI_BuildingList>(ui =>
        {
            ui.Initialize(buildings, OnBuildingSelectedFromUI);
            ui.OnClosed -= OnBuildingListClosed;
            ui.OnClosed += OnBuildingListClosed;
        });

        if (ui == null) return;
        _owner.EnterUIMode();
    }

    public void Rotate()
    {
        if (_selectedBuilding == null) return;
        _rotationQuarterTurns = (_rotationQuarterTurns + 1) % 4;
        RefreshGhost();
    }

    public void Cancel()
    {
        DestroyGhost();
    }

    public void TryPlace(TerrainCell cell)
    {
        if (cell == null || _selectedBuilding == null) return;

        int direction = _rotationQuarterTurns;
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(
            cell.GridPosition, _selectedBuilding, direction);

        if (!preview.CanPlace) return;
        if (_resourceHandler == null || !_resourceHandler.HasRequired(_selectedBuilding)) return;

        var request = new BuildingRequest
        {
            Data = _selectedBuilding,
            AnchorPos = cell.GridPosition,
            Direction = direction
        };
        _buildingManager.RequestBuild(request);
        DestroyGhost();
    }

    public void TryRemove(TerrainCell cell)
    {
        if (cell == null || _buildingManager == null) return;
        _buildingManager.RequestRemove(cell.GridPosition);
    }

    public void UpdatePreview(TerrainCell cell)
    {
        if (_ghost == null) return;

        if (cell == null)
        {
            _ghost.SetVisible(false);
            return;
        }

        _ghost.SetVisible(true);
        bool resourceDirty = _resourceHandler != null && _resourceHandler.IsDirty;
        if (cell.GridPosition != _prevGridPos || resourceDirty)
        {
            _prevGridPos = cell.GridPosition;
            RefreshGhost();
        }
    }

    public void Dispose()
    {
        _resourceHandler.OnResourceChanged -= OnResourceChanged;
        DestroyGhost();
    }

    #endregion

    #region Internal

    private void OnBuildingSelectedFromUI(BuildingDataSO data)
    {
        _resourceHandler?.MarkDirty();

        if (_ghost?.Instance != null && _ghostBuildingId != data?.BuildingId)
        {
            DestroyGhost();
        }

        _selectedBuilding = data;
        EnterPreviewAsync().Forget();
    }

    private void OnBuildingListClosed()
    {
        _owner.ExitUIMode();
    }

    private void OnResourceChanged()
    {
        RefreshBuildInfoUIIfOpen();
    }

    private async UniTaskVoid EnterPreviewAsync()
    {
        int previewRequestVersion = ++_previewRequestVersion;

        BuildingDataSO requested = _selectedBuilding;
        if (requested == null) return;

        // 기존 Ghost가 있으면 재사용
        if (_ghost?.Instance != null && _ghostBuildingId == requested.BuildingId)
        {
            _rotationQuarterTurns = BuildingPlacer.GetDirection(_owner.transform.forward);
            _ghost.SetVisible(true);
            RefreshGhostInitial();
            ShowOrRefreshBuildInfoUI();
            return;
        }

        string prefabKey = AssetKey.Building.GetKey(requested.BuildingId);
        if (string.IsNullOrEmpty(prefabKey)) return;

        var prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
        if (previewRequestVersion != _previewRequestVersion
            || requested != _selectedBuilding) return;

        if (prefab == null) return;

        _rotationQuarterTurns = BuildingPlacer.GetDirection(_owner.transform.forward);
        _ghost = new BuildingGhost();
        _ghost.Spawn(prefab, _ghostConfig.Material, _ghostConfig.ValidColor, _ghostConfig.InvalidColor);
        _ghostBuildingId = requested.BuildingId;
        RefreshGhostInitial();
        ShowOrRefreshBuildInfoUI();
    }

    private void RefreshGhostInitial()
    {
        // 초기 위치 갱신은 세션 외부에서 UpdatePreview로 처리됨
        // 여기서는 ghost가 생성된 직후 UI만 갱신
    }

    private void RefreshGhost()
    {
        if (_ghost == null || _selectedBuilding == null) return;

        int direction = _rotationQuarterTurns;
        BuildingPreviewInfo preview = _buildingManager.GetPreviewInfo(
            _prevGridPos, _selectedBuilding, direction);

        _ghost.UpdateTransform(preview.SpawnPosition, preview.Rotation);

        bool hasResources = _resourceHandler != null && _resourceHandler.CheckCached(_selectedBuilding);
        _ghost.SetValid(preview.CanPlace && hasResources);
    }

    private void DestroyGhost()
    {
        _previewRequestVersion++;
        _ghost?.Destroy();
        _ghost = null;
        _ghostBuildingId = null;
        _prevGridPos = default;
        _selectedBuilding = null;
        UIController.Instance?.CloseAsync<UI_BuildInfo>().Forget();
    }

    private void ShowOrRefreshBuildInfoUI()
    {
        if (_selectedBuilding == null || UIController.Instance == null) return;
        if (_resourceHandler == null) return;

        int[] ownedCounts = _resourceHandler.GetOwnedCounts(_selectedBuilding);

        UI_BuildInfo openedUi = UIController.Instance.GetInstance<UI_BuildInfo>();
        if (openedUi != null && openedUi.IsOpen)
        {
            openedUi.SetData(_selectedBuilding, ownedCounts);
            return;
        }

        UIController.Instance?.OpenAsync<UI_BuildInfo>(ui =>
        {
            ui.SetData(_selectedBuilding, ownedCounts);
        }).Forget();
    }

    private void RefreshBuildInfoUIIfOpen()
    {
        if (_selectedBuilding == null || UIController.Instance == null) return;
        if (_resourceHandler == null) return;

        var ui = UIController.Instance.GetInstance<UI_BuildInfo>();
        if (ui == null) return;
        if (!ui.IsOpen) return;
        ui.SetData(_selectedBuilding, _resourceHandler.GetOwnedCounts(_selectedBuilding));
    }

    #endregion
}
