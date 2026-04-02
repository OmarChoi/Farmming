using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _waterVfxPrefab;
    [SerializeField] private GameObject _iceVfxPrefab;
    [SerializeField] private float _secondaryEnergyCost = 25f;
    [SerializeField] private float _iceSpawnOffset = 17.5f;

    private HelperAnimationAbility _animAbility;
    private readonly List<IWaterEffect> _waterEffects = new();
    private readonly List<IWaterEffect> _iceEffects = new();
    private bool _isActing = false;
    private TerrainCell _currentCell;
    private bool _isSecondary = false;

    protected override void Awake()
    {
        base.Awake();
        AddWaterEffects();
        AddIceEffects();
    }

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void AddWaterEffects()
    {
        _waterEffects.Add(new FarmDryWaterEffect());
        // 나중에 용암 타일 물적신 효과 추가예정
    }

    private void AddIceEffects()
    {
        _iceEffects.Add(new LavaToStoneWaterEffect());
    }

    private void StartWaterAction(TerrainCell cell, bool isSecondary)
    {
        _isActing = true;
        _isSecondary = isSecondary;
        _currentCell = cell;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Water);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null) return;
        if (_owner.IsMine && _isActing) return;
        StartWaterAction(cell, isSecondary: false);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (cell == null) return;
        if (_owner.IsMine && _isActing) return;
        if(!_owner.Energy.TryConsume(_secondaryEnergyCost)) return;
        StartWaterAction(cell, isSecondary: true);
    }

    public void WaterOpen()
    {
        if (_currentCell == null) return;

        Vector3 spawnPos = _mouthPoint != null
            ? _mouthPoint.position
            : _owner.transform.position;

        Vector3 targetPos = GetTargetPosition(_currentCell);
        Vector3 direction = (targetPos - spawnPos).normalized;
        TerrainCell cell = _currentCell;    

        if(_isSecondary)
        {
            if (_iceVfxPrefab != null)
            {
                Vector3 iceSpawnPos = targetPos + Vector3.up * _iceSpawnOffset;

                GameObject vfxObj = Instantiate(_iceVfxPrefab, iceSpawnPos, Quaternion.identity);
                IceVFX iceVfx = vfxObj.GetComponentInChildren<IceVFX>();
                iceVfx?.Launch(targetPos, direction, () =>
                {
                    ApplyEffects(cell, _iceEffects);
                });
            }
        }
        else
        {
            if (_waterVfxPrefab != null)
            {
                GameObject vfxObj = Instantiate(
                    _waterVfxPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                WaterVFX waterVfx = vfxObj.GetComponent<WaterVFX>();

                waterVfx?.Launch(targetPos, direction, () =>
                {
                    ApplyEffects(cell, _waterEffects);
                });
            }
        }
    }

    private void ApplyEffects(TerrainCell cell, List<IWaterEffect> effects)
    {
        foreach (IWaterEffect effect in effects)
        {
            if (effect.CanHandle(cell))
            {
                effect.Apply(cell);
                return;
            }
        }
    }

    public void WaterClose()
    {
        _animAbility?.Play(EHelperAnim.Idle);
        ResetState();
    }

    private void ResetState()
    {
        _isActing = false;
        _isSecondary = false;
        _currentCell = null;
        _owner?.EndAction();
    }

    private Vector3 GetTargetPosition(TerrainCell cell)
    {
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            Vector3 pos = cell.FarmTile.CropSpawnPoint != null ? cell.FarmTile.CropSpawnPoint.position : cell.transform.position;
            return pos + Vector3.up * 0.1f;
        }

        Vector3 rayOrigin = cell.transform.position + Vector3.up * 3f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            return hit.point + Vector3.up * 0.1f;
        }

        return cell.transform.position + Vector3.up * 0.5f;
    }
}
