using System.Collections;
using UnityEngine;

// 파종 곡룡: FarmDry에서 씨앗 주기
public class SowActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private float _sowEffectDelay = 0.3f;
    [SerializeField] private float _sowDelay = 0.5f;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;

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

        _cultivateAbility.JumpAndCultivate(cell, null);
    }
    
    public void InteractSecondary(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
        {
            Debug.Log("농지 없음");
            return;
        }

        if (!farmTile.IsReadyToSow)
        {
            Debug.Log("씨앗 심을 수 없는 상태");
            return;
        }

        SeedConfig selectedSeed = _seedSelector?.SelectedSeed;
        if(selectedSeed == null)
        {
            Debug.Log("씨앗이 선택되지 않음");
            return;
        }

        StartCoroutine(SowCoroutine(farmTile, selectedSeed));
    }

    private IEnumerator SowCoroutine(FarmTile farmTile, SeedConfig seed)
    {
        _animAbility?.Play(EHelperAnim.Sow);

        yield return new WaitForSeconds(_sowEffectDelay);

        if(_seedVfxPrefab != null && _mouthPoint != null)
        {
            GameObject vfxObj = Instantiate(_seedVfxPrefab, _mouthPoint.position, Quaternion.identity);

            SowVFX sowVfx = vfxObj.GetComponent<SowVFX>();
            sowVfx?.Launch(farmTile.CropSpawnPoint.position);
        }

        yield return new WaitForSeconds(_sowDelay);

        farmTile.Interact(seed);

        _animAbility?.Play(EHelperAnim.Idle);     

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
}
