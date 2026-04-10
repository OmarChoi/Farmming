using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _iceVfxPrefab;

    [SerializeField] private float _secondaryEnergyCost = 25f;
    [SerializeField] private float _iceSpawnOffset = 17.5f;
    [SerializeField] private float _secondaryIceOpenDelay = 0.2f;
    [SerializeField] private float _secondaryIceCompleteDelay = 1.0f;
    [SerializeField] private int _waterExperience = 10;

    private static readonly int WaterStateHash = Animator.StringToHash("Water");

    private HelperAnimationAbility _animAbility;
    private WaterNormalVFXAbility _normalVFX;
    private WaterEpicVFXAbility _epicVFX;
    private WaterLegendaryVFXAbility _legendaryVFX;

    private readonly List<IWaterEffect> _waterEffects = new();
    private readonly List<IWaterEffect> _iceEffects = new();

    private bool _isActing;
    private bool _isSecondary;
    private bool _anyWatered;
    private bool _waterOpened;
    private TerrainCell _currentCell;
    private Coroutine _secondaryIceCoroutine;

    public float GetSecondaryCost() => _secondaryEnergyCost;

    private EHelperGrade CurrentGrade => _owner.Grade.CurrentGrade;

    protected override void Awake()
    {
        base.Awake();
        _waterEffects.Add(new FarmDryWaterEffect());
        _iceEffects.Add(new LavaToStoneWaterEffect());
    }

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _normalVFX = _owner.GetAbility<WaterNormalVFXAbility>();
        _epicVFX = _owner.GetAbility<WaterEpicVFXAbility>();
        _legendaryVFX = _owner.GetAbility<WaterLegendaryVFXAbility>();
    }

    private void Update()
    {
        if (!_isActing) return;
        if (_owner.Grade.CurrentGrade == EHelperGrade.Normal || !_isSecondary) return;

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

    public bool CanInteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        return cell != null;
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        return _owner.Grade.CurrentGrade >= _owner.Data.SecondaryUnlockGrade;
    }

    public void InteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (CurrentGrade == EHelperGrade.Normal && HasBlockingFrontObject(cell)) return;

        if (_owner.IsMine && _isActing) return;
        StartWaterAction(cell, isSecondary: false);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (_owner.Grade.CurrentGrade < _owner.Data.SecondaryUnlockGrade) return;
        if (_owner.IsMine && _isActing) return;

        StartWaterAction(cell, isSecondary: true);
    }

    private void StartWaterAction(TerrainCell cell, bool isSecondary)
    {
        _isActing = true;
        _isSecondary = isSecondary;
        _anyWatered = false;
        _waterOpened = false;
        _currentCell = cell;

        _owner.BeginAction();

        if (isSecondary)
        {
            _animAbility?.Replay(EHelperAnim.LegendaryIce);
            if (_secondaryIceCoroutine != null) StopCoroutine(_secondaryIceCoroutine);
            _secondaryIceCoroutine = StartCoroutine(SecondaryIceFlow());
        }
        else
        {
            GetCurrentGradeVFX().BeginAction(WaterOpen, ResetState);
        }
    }

    private IEnumerator SecondaryIceFlow()
    {
        if (_secondaryIceOpenDelay > 0f)
        {
            yield return new WaitForSeconds(_secondaryIceOpenDelay);
        }

        WaterOpen();

        if (_secondaryIceCompleteDelay > 0f)
        {
            yield return new WaitForSeconds(_secondaryIceCompleteDelay);
        }

        _secondaryIceCoroutine = null;
        ResetState();
    }

    public void WaterOpen()
    {
        if (_currentCell == null) return;
        if (_waterOpened) return;

        _waterOpened = true;

        Vector3 spawnPos = _mouthPoint != null ? _mouthPoint.position : _owner.transform.position;

        List<TerrainCell> targetCells = _isSecondary
            ? new List<TerrainCell> { _currentCell }
            : GetTargetCells(_currentCell);

        if (targetCells.Count == 0)
        {
            ResetState();
            return;
        }

        GetCurrentGradeVFX().SpawnHelperVFX();

        for (int i = 0; i < targetCells.Count; i++)
        {
            TerrainCell cell = targetCells[i];
            bool isCenter = (i == 0);
            Vector3 targetPos = GetTargetPosition(cell);

            if (_isSecondary)
            {
                SpawnIceVFX(cell, targetPos, (targetPos - spawnPos).normalized);
            }
            else
            {
                GetCurrentGradeVFX().SpawnCellVFX(cell, targetPos, isCenter, spawnPos, OnCellLand);
            }
        }
    }


    private void OnCellLand(TerrainCell cell, bool isCenter)
    {
        if (ApplyEffects(cell, _waterEffects) && !_anyWatered)
        {
            _anyWatered = true;
            _owner.Experience.Add(_waterExperience);
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

    private void SpawnIceVFX(TerrainCell cell, Vector3 targetPos, Vector3 direction)
    {
        if (_iceVfxPrefab == null) return;

        TerrainCell capturedCell = cell;
        Vector3 iceSpawnPos = targetPos + Vector3.up * _iceSpawnOffset;

        GameObject vfxObj = Instantiate(_iceVfxPrefab, iceSpawnPos, Quaternion.identity);
        IceVFX iceVfx = vfxObj.GetComponentInChildren<IceVFX>();
        iceVfx?.Launch(targetPos, direction, () =>
        {
            ApplyEffects(capturedCell, _iceEffects);
        });
    }

    private void ResetState()
    {
        if (!_isActing) return;

        if (_secondaryIceCoroutine != null)
        {
            StopCoroutine(_secondaryIceCoroutine);
            _secondaryIceCoroutine = null;
        }

        GetCurrentGradeVFX().Cancel();

        _isActing = false;
        _isSecondary = false;
        _anyWatered = false;
        _waterOpened = false;
        _currentCell = null;
        _animAbility?.Play(EHelperAnim.Idle);
        _owner?.EndAction();
    }

    private IWaterGradeVFX GetCurrentGradeVFX() => CurrentGrade switch
    {
        EHelperGrade.Epic => _epicVFX,
        EHelperGrade.Legendary => _legendaryVFX,
        _ => _normalVFX
    };

    private Vector3 GetTargetPosition(TerrainCell cell)
    {
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            Vector3 pos = cell.FarmTile.CropSpawnPoint != null
                ? cell.FarmTile.CropSpawnPoint.position
                : cell.transform.position;
            return pos + Vector3.up * 0.1f;
        }

        Vector3 rayOrigin = cell.transform.position + Vector3.up * 3f;
        if (Physics.Raycast(new Ray(rayOrigin, Vector3.down), out RaycastHit hit, 5f))
        {
            return hit.point + Vector3.up * 0.1f;
        }

        return cell.transform.position + Vector3.up * 0.5f;
    }

    private List<TerrainCell> GetTargetCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell>();

        centerCell = GetInteractableCell(centerCell);
        if (centerCell == null)
            return cells;

        if (!HasObject(centerCell))
        {
            cells.Add(centerCell);
        }

        int extension = _owner.Grade.GetRange() - 1; // Normal:0, Epic:1, Legendary:2
        if (extension <= 0) return cells;

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= extension; i++)
        {
            var rightCell = GetGridInteractableCell(centerCell.GridPosition + rightOffset * i);
            var leftCell = GetGridInteractableCell(centerCell.GridPosition - rightOffset * i);

            if (rightCell != null && !HasObject(rightCell))
                cells.Add(rightCell);

            if (leftCell != null && !HasObject(leftCell))
                cells.Add(leftCell);
        }
        return cells;
    }

    private bool HasObject(TerrainCell cell) => cell.CurrentObject != null;

    private bool HasBlockingFrontObject(TerrainCell cell)
    {
        if (cell?.CurrentObject == null) return false;

        EGridObjectType objectType = cell.Data.ObjectType;
        return objectType == EGridObjectType.Rock || objectType == EGridObjectType.Tree;
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null) return Vector3Int.right;

        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }

}
