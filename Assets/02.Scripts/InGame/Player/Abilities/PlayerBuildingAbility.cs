using UnityEngine;

public class PlayerBuildingAbility : PlayerAbility
{
    [Header("참조")]
    [SerializeField] private BuildingManager _buildingManager;
    [SerializeField] private BuildingDataSO _buildingData;

    [Header("키 설정")]
    [SerializeField] private KeyCode _placeKey = KeyCode.B;
    [SerializeField] private KeyCode _removeKey = KeyCode.N;
    [SerializeField] private KeyCode _rotateKey = KeyCode.R;

    private PlayerTerrainAbility _terrainAbility;
    private bool _swapped;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void Update()
    {
        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_rotateKey))
        {
            ToggleSwap();
        }

        if (Input.GetKeyDown(_placeKey))
        {
            TryPlace();
        }
        else if (Input.GetKeyDown(_removeKey))
        {
            TryRemove();
        }
    }

    private void ToggleSwap()
    {
        if (_buildingData == null) return;
        _swapped = !_swapped;
    }

    private void TryPlace()
    {
        if (_buildingData == null) return;

        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        // todo. buildingData 정보 선택 기능(UI) 추가 
        var request = new BuildingRequest
        {
            Data = _buildingData,
            AnchorPos = cell.GridPosition,
            Direction = BuildingPlacer.GetDirection(_owner.transform.forward),
            Swapped = _swapped
        };
        _buildingManager.TryBuild(request);
    }

    private void TryRemove()
    {
        var cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _buildingManager.TryRemove(cell.GridPosition);
    }
}
