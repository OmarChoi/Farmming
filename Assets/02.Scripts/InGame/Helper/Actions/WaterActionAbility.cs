using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _waterVfxPrefab;
    [SerializeField] private GameObject _iceVfxPrefab;

    [Header("에픽 등급 물효과")]
    [SerializeField] private GameObject _epicWaterHelperVfxPrefab;
    [SerializeField] private GameObject _epicWaterLandVfxPrefab;
    [SerializeField] private float _splashDuration = 1f;

    [Header("레전더리 등급 물효과")]
    [SerializeField] private GameObject _legendaryWaterHelperVfxPrefab;
    [SerializeField] private GameObject _legendaryWaterLandVfxPrefab;

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
    private Coroutine _rotationCoroutine;
    private Coroutine _jumpCoroutine;

    private EHelperGrade CurrentGrade => _owner.Grade.CurrentGrade;

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
        if(CurrentGrade == EHelperGrade.Normal || _isSecondary)
        {
            if (!_isActing) return;

            var stateInfo = _animAbility.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == WaterStateHash && stateInfo.normalizedTime >= 1f)
            {
                ResetState();
            }
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

        if (CurrentGrade == EHelperGrade.Legendary && !isSecondary)
        {
            _animAbility?.Play(EHelperAnim.Happy);

            _animAbility?.Play(EHelperAnim.Jump);
            if (_rotationCoroutine != null) StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = StartCoroutine(RotateLegendary(2.5f));
            Invoke(nameof(WaterOpen), 0.2f); // wateropen 이벤트를 추가하지않으면 써야되는 코드
        }
        else if(CurrentGrade == EHelperGrade.Epic)
        {
            _animAbility?.Play(EHelperAnim.Jump);

            if(_jumpCoroutine != null) StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = StartCoroutine(LoopJumpAnimation());
            Invoke(nameof(WaterOpen), 0.2f);
        }
        else
        {
            _animAbility?.Play(EHelperAnim.Water);
        }
    }

    private IEnumerator RotateLegendary(float duration)
    {
        float elapsed = 0f;
        Quaternion startRotation = _owner.transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float angle = (elapsed * 720f); // 720f 숫자가 크면 도는 속도가빨라진다.
            _owner.transform.rotation = startRotation * Quaternion.Euler(0, angle, 0);

            yield return null;
        }

        _owner.transform.rotation = startRotation;
        ResetState();
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
        if (cell == null)
        {
            return;
        }

        if (_owner.Grade.CurrentGrade < _owner.Data.SecondaryUnlockGrade)
        {
            return;
        }
        // 로컬만 중복 입력 방지, 원격은 소유자가 검증한 RPC이므로 그대로 실행
        if (_owner.IsMine && _isActing)
        {
           return;
        }
        if(!_owner.Energy.TryConsume(_secondaryEnergyCost))
        {
            return;
        }

        StartWaterAction(cell, isSecondary: true);
    }

    public void WaterOpen()
    {
        if (_currentCell == null) return;

        Vector3 spawnPos = _mouthPoint != null
            ? _mouthPoint.position
            : _owner.transform.position;

        List<TerrainCell> targetCells = _isSecondary
            ? new List<TerrainCell> { _currentCell }
            : GetTargetCells(_currentCell);

        HandleGradeVisualEffects();

        for (int i = 0; i < targetCells.Count; i++)
        {
            TerrainCell cell = targetCells[i];
            bool isCenter = (i == 0); // 경험치는 중앙 셀 1회만 나중에 적용된면적 3개, 5개등등 적용할지 생각하기, 가운데만해도좋을것같다.

            Vector3 targetPos = GetTargetPosition(cell);
            Vector3 direction = (targetPos - spawnPos).normalized;


            if (_isSecondary)
            {
                if (_iceVfxPrefab != null)
                {
                    TerrainCell capturedCell = cell;
                    Vector3 iceSpawnPos = targetPos + Vector3.up * _iceSpawnOffset;

                    GameObject vfxObj = Instantiate(_iceVfxPrefab, iceSpawnPos, Quaternion.identity);
                    IceVFX iceVfx = vfxObj.GetComponentInChildren<IceVFX>();
                    iceVfx?.Launch(targetPos, direction, () =>
                    {
                        ApplyEffects(capturedCell, _iceEffects);
                    });
                }
            }
            else
            {
                if (CurrentGrade == EHelperGrade.Normal)
                {
                    HandleWaterAction(cell, targetPos, direction, isCenter);
                }
                else
                {
                    StartCoroutine(DelayedLandEffect(cell, targetPos, isCenter));
                }
            }
        }
    }

    private IEnumerator DelayedLandEffect(TerrainCell cell, Vector3 targetPos, bool isCenter)
    {
        yield return new WaitForSeconds(0.1f);

        SpawnLandingEffect(targetPos);

        if (ApplyEffects(cell, _waterEffects) && isCenter)
        {
            _owner.Experience.Add(_waterExperience);
        }

        if (CurrentGrade == EHelperGrade.Epic)
        {
            yield return new WaitForSeconds(1.0f);
            ResetState();
        }
    }

    private IEnumerator LoopJumpAnimation()
    {
        //_animAbility.Animator.speed = 2f;

        while (true)
        {
            _animAbility?.Play(EHelperAnim.Jump);

            yield return new WaitForSeconds(0.6f);
        }
    }

    private void HandleGradeVisualEffects()
    {
        if(CurrentGrade == EHelperGrade.Epic)
        {
            if (_epicWaterHelperVfxPrefab != null)
            {
                GameObject waterhelpervfx = Instantiate(_epicWaterHelperVfxPrefab, _owner.transform.position, Quaternion.identity);
                waterhelpervfx.transform.SetParent(_owner.transform);
                Destroy(waterhelpervfx, _splashDuration);
            }
        }
        else if (CurrentGrade == EHelperGrade.Legendary)
        {
            if (_legendaryWaterHelperVfxPrefab != null)
            {
                GameObject auroraVfx = Instantiate(_legendaryWaterHelperVfxPrefab, _owner.transform.position, Quaternion.identity);
                auroraVfx.transform.SetParent(_owner.transform);
                Destroy(auroraVfx, _splashDuration);
            }
        }
    }

    private void HandleWaterAction(TerrainCell cell, Vector3 targetPos, Vector3 direction, bool isCenter)
    {
        if (_waterVfxPrefab == null) return;

        GameObject vfxObj = Instantiate(_waterVfxPrefab, _owner.transform.position, Quaternion.identity);
        WaterVFX waterVfx = vfxObj.GetComponent<WaterVFX>();

        waterVfx?.Launch(targetPos, direction, () =>
        {
            if (ApplyEffects(cell, _waterEffects) && isCenter)
            {
                _owner.Experience.Add(_waterExperience);
            }
        });
    }

    private void SpawnLandingEffect(Vector3 position)
    {
        GameObject effectPrefab = null;

        if (CurrentGrade == EHelperGrade.Legendary) effectPrefab = _legendaryWaterLandVfxPrefab;
        else if (CurrentGrade == EHelperGrade.Epic) effectPrefab = _epicWaterLandVfxPrefab;

        if (effectPrefab != null)
        {
            GameObject landVfx = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(landVfx, 2f);
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
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = null;
        }

        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = null;
        }

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

    private List<TerrainCell> GetTargetCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell>();

        if(!HasObject(centerCell))
        {
            cells.Add(centerCell);
        }

        int extension = _owner.Grade.GetRange() - 1; // Normal:0  Epic:1
        if(extension <= 0)
        {
            return cells;
        }

        Vector3Int rightOffset = GetGridRightOffset();
        for(int i = 1; i<= extension; i++)
        {
            var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            var leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset * i);
            if(rightCell != null)
            {
                if (!HasObject(rightCell) && rightCell.Data.IsTop) 
                {
                    cells.Add(rightCell);
                }
            }
            if(leftCell != null)
            {
                if (!HasObject(leftCell) && leftCell.Data.IsTop)
                {
                    cells.Add(leftCell);
                }
            }
        }
        return cells;
    }

    private bool HasObject(TerrainCell cell)
    {
        return cell.CurrentObject != null; //돌이나 나무가있는상태
    }

    private Vector3Int GetGridRightOffset()
    {
        if(_owner.PlayerOwner == null)
        {
            return Vector3Int.right;
        }

        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }
}
