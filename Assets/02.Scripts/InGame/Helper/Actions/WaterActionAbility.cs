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
    [SerializeField] private int _waterExperience = 10;

    private static readonly int WaterStateHash = Animator.StringToHash("Water");

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

    private void Update()
    {
        if (!_isActing) return;

        var stateInfo = _animAbility.Animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash == WaterStateHash && stateInfo.normalizedTime >= 1f)
        {
            ResetState();
        }
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void AddWaterEffects()
    {
        _waterEffects.Add(new FarmDryWaterEffect());
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
        // 로컬만 중복 입력 방지, 원격은 소유자가 검증한 RPC이므로 그대로 실행
        if (_owner.IsMine && _isActing) return;
        StartWaterAction(cell, isSecondary: false);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (cell == null) return;
        // 로컬만 중복 입력 방지, 원격은 소유자가 검증한 RPC이므로 그대로 실행
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
                    if(ApplyEffects(cell, _waterEffects))
                    {
                        _owner.Experience.Add(_waterExperience);
                    }
                });
            }
        }
    }

    private bool ApplyEffects(TerrainCell cell, List<IWaterEffect> effects)
    {
        foreach (IWaterEffect effect in effects)
        {
            if (effect.CanHandle(cell))
            {
                effect.Apply(cell);
                return true;
            }
        }
        return false;
    }
    
    private void ResetState()
    {
        _isActing = false;
        _isSecondary = false;
        _currentCell = null;
        _animAbility?.Play(EHelperAnim.Idle);
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
