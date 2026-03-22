using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _waterVfxPrefab;
    [SerializeField] private float _waterEffectDelay = 0.3f;
    [SerializeField] private float _waterDelay = 0.5f;

    private HelperAnimationAbility _animAbility;

    private readonly List<IWaterEffect> _waterEffects = new();

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
        
        StartCoroutine(WaterCoroutine(cell));
    }
    
    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
    }

    private IEnumerator WaterCoroutine(TerrainCell cell)
    {
        Vector3 targetPos = GetTargetPosition(cell);

        _animAbility?.Play(EHelperAnim.Water);
        yield return new WaitForSeconds(_waterEffectDelay);

        if(_waterVfxPrefab != null && _mouthPoint != null)
        {
            GameObject vfxObj = Instantiate(_waterVfxPrefab, _mouthPoint.position, Quaternion.identity);


            WaterVFX waterVfx = vfxObj.GetComponent<WaterVFX>();
            waterVfx?.OnLand(targetPos);
        }

        yield return new WaitForSeconds(_waterDelay);

        ApplyWaterEffects(cell);

        _animAbility?.Play(EHelperAnim.Idle);
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
