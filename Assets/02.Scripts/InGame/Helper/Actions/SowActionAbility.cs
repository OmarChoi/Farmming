using System.Collections;
using UnityEngine;

// 파종 곡룡: FarmDry에서 씨앗 주기
public class SowActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private float _sowDelay = 0.5f;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;

    private bool _isActing = false;
    private FarmTile _currentFarmTile;
    private SeedConfig _currentSeed;

    protected override void Awake()
    {
        base.Awake();
    }

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
        if (_isActing)
        {
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;
        if (!farmTile.IsReadyToSow) return;

        SeedConfig selectedSeed = _seedSelector?.SelectedSeed;
        if(selectedSeed == null) return;

        _isActing = true;
        _currentFarmTile = farmTile;
        _currentSeed = selectedSeed;

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
        SeedConfig seed = _currentSeed;

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

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedConfig seed)
    {
        yield return new WaitForSeconds(_sowDelay);
        farmTile?.PlantSeed(seed);
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
}
