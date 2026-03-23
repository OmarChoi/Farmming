using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _waterVfxPrefab;
    [SerializeField] private float _vfxDuration = 0.3f;

    private HelperAnimationAbility _animAbility;

    private readonly List<IWaterEffect> _waterEffects = new();
    private bool _isActing = false;

    protected override void Awake()
    {
        base.Awake();
        AddWaterEffects();
    }

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    private void AddWaterEffects()
    {
        _waterEffects.Add(new FarmDryWaterEffect());
        // 나중에 용암 타일 물적신 효과 추가예정
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null)
        {
            return;
        }
        if(_isActing)
        {
            return;
        }
        
        StartCoroutine(WaterCoroutine(cell));
    }
    
    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
    }

    private IEnumerator WaterCoroutine(TerrainCell cell)
    {
        _isActing = true;

        Vector3 spawnPos = _mouthPoint !=null ?_mouthPoint.position : _owner.transform.position;
        Vector3 targetPos = GetTargetPosition(cell);

        Vector3 direction = (targetPos - spawnPos).normalized;
        _animAbility?.Play(EHelperAnim.Water);

        if(_waterVfxPrefab != null)
        {
            GameObject vfxObj = Instantiate(_waterVfxPrefab, spawnPos, Quaternion.identity);
            Debug.Log("물이펙트 생성");
            WaterVFX waterVfx = vfxObj.GetComponent<WaterVFX>();
            waterVfx?.PlayeEffect(targetPos, direction);
            Debug.Log("플레이이펙트");
        }

        yield return new WaitForSeconds(_vfxDuration);

        ApplyWaterEffects(cell);
        Debug.Log("땅물젖음 적용");

        _animAbility?.Play(EHelperAnim.Idle);
        _isActing = false;

    }

    private void ApplyWaterEffects(TerrainCell cell)
    {
        foreach(IWaterEffect effect in _waterEffects)
        {
            if(effect.CanHandle(cell))
            {
                effect.Apply(cell);
                return;
            }
        }
    }

    private Vector3 GetTargetPosition(TerrainCell cell)
    {
        if(cell.FarmTile !=null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile.CropSpawnPoint != null? cell.FarmTile.CropSpawnPoint.position : cell.transform.position;
        }

        return cell.transform.position;
    }
}
