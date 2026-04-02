using System.Collections;
using Photon.Pun;
using UnityEngine;

// 파종 곡룡: FarmDry에서 씨앗 주기
public class SowActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private float _sowDelay = 0.5f;

    [SerializeField] private int _cultivateExperience = 10;
    [SerializeField] private int _sowExperience = 10;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;

    private bool _isActing = false;
    private FarmTile _currentFarmTile;
    private SeedItemDataSO _currentSeed;

    private void Start()
    {
        _cultivateAbility = _owner.GetAbility<CultivateAbility>();
        _seedSelector = _owner.GetAbility<SeedSelectAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null)
        {
            return;
        }

        if (cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf)
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, () =>
            {
                cell.TryConvertToFarm();
                _owner.Experience.Add(_cultivateExperience);
            });
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
        {
            return;
        }

        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, null);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (_owner.IsMine && _isActing)
        {
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;
        if (!farmTile.IsReadyToSow) return;

        SeedItemDataSO selectedSeed = _seedSelector?.SelectedSeed;
        if(selectedSeed == null) return;

        StartSow(farmTile, selectedSeed);

        var pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlantSeed), RpcTarget.Others,
            pos.x, pos.y, pos.z, selectedSeed.Id);
    }

    private void StartSow(FarmTile farmTile, SeedItemDataSO seed)
    {
        _isActing = true;
        _currentFarmTile = farmTile;
        _currentSeed = seed;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Sow);
    }

    public void SowOpen()
    {
        if (_currentFarmTile == null || _mouthPoint == null)
        {
            return;
        }

        Vector3 spawnPos = _mouthPoint.position;
        Vector3 targetPos = _currentFarmTile.CropSpawnPoint.position;
        Vector3 direction = (targetPos - spawnPos).normalized;

        if (_seedVfxPrefab != null)
        {
            GameObject vfxObj = Instantiate(_seedVfxPrefab, spawnPos, Quaternion.identity);
            SowVFX sowVfx = vfxObj.GetComponent<SowVFX>();
            sowVfx?.Launch(targetPos, direction);
        }

        FarmTile farmTile = _currentFarmTile;
        SeedItemDataSO seed = _currentSeed;

        StartCoroutine(PlantAfterDelay(farmTile, seed));
    }

    public void SowClose()
    {
        _animAbility?.Play(EHelperAnim.Idle);
        _currentFarmTile = null;
        _currentSeed = null;
        _isActing = false;
        _owner.EndAction();
    }

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedItemDataSO seed)
    {
        yield return new WaitForSeconds(_sowDelay);
        if (farmTile == null) yield break;

        farmTile.PlantSeed(seed);
        _owner.Experience.Add(_sowExperience);
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        if(cell == null)
        {
            return null;
        }
        if(cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }

        return null;
    }

    private void OnDisable()
    {
        _isActing = false;
        _currentFarmTile = null;
        _currentSeed = null;
        _owner?.EndAction();
    }

    [PunRPC]
    internal void RPC_PlantSeed(int gridX, int gridY, int gridZ, int seedId)
    {
        var cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (cell == null) return;

        var seed = TerrainGridManager.Instance.SeedDatabase?.GetById(seedId);
        if (seed == null) return;

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;

        StartSow(farmTile, seed);
    }
}