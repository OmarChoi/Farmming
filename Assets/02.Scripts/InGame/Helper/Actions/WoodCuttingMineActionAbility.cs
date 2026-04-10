using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 벌목 채굴: 좌클릭(벌목) / 우클릭(채굴)
public enum EGatherType
{
    Wood,
    Stone,
}

public class WoodCuttingMineActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] protected GameObject _effectWoodPrefab;
    [SerializeField] private GameObject _woodNormalEffect;
    [SerializeField] protected GameObject _effectStonePrefab;
    [SerializeField] protected Transform _effectSpawnPoint;
    [SerializeField] private float _idleTransition = 0.5f;
    [SerializeField] private float _slashMagicHeightOffset = 1f;
    [SerializeField] private float _slashMagicLifetime = 1f;
    [SerializeField] private Vector3 _slashMagicRotationOffset = Vector3.zero;

    [SerializeField] private float _wideGatherOffset = 2f;

    private StoneMineAbility _stoneMineAbility;
    private HelperAnimationAbility _animAbility;
    private RangeBoostEffect _rangeBoostEffect;

    protected override void Awake()
    {
        base.Awake();
        _stoneMineAbility = _owner.GetAbility<StoneMineAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _rangeBoostEffect = _owner.GetAbility<RangeBoostEffect>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _owner?.EndAction();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        return cell.Data.ObjectType == EGridObjectType.Tree;
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        return cell.Data.ObjectType == EGridObjectType.Rock;
    }

    public void InteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.WoodCutting);

        GatheringInfo info = new GatheringInfo(_owner);

        bool isWideActive = _rangeBoostEffect != null && _rangeBoostEffect.IsActive;
        if (_woodNormalEffect != null)
        {
            SpawnSlashMagicAndGather(cell, info);

            if (isWideActive)
            {
                TerrainCell[] wideCells = GetHorizontalAdjacentCells(cell);
                foreach (TerrainCell wideCell in wideCells)
                {
                    SpawnSlashMagicAndGather(wideCell, info);
                }
            }
        }
        else if (isWideActive)
        {
            Vector3 playerRight = _owner.PlayerOwner.transform.right;
            SpawnWoodVFX(info, Vector3.zero);                         
            SpawnWoodVFX(info, playerRight * _wideGatherOffset);  
            SpawnWoodVFX(info, -playerRight * _wideGatherOffset);   
        }
        else
        {
            SpawnWoodVFX(info, Vector3.zero);
        }

        StartCoroutine(AnimPlayCoroutine());
    }

    private void SpawnSlashMagicAndGather(TerrainCell cell, GatheringInfo info)
    {
        if (cell == null) return;
        if (cell.CurrentObject == null) return;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return;

        SpawnSlashMagic(cell);

        if (cell.CurrentObject.TryGetComponent<IGatherable>(out IGatherable gatherable) && gatherable is Wood)
        {
            gatherable.TryGather(info);
        }
    }

    private void SpawnWoodVFX(GatheringInfo info, Vector3 worldOffset)
    {
        Vector3 spawnPos = _effectSpawnPoint.position + worldOffset;
        GameObject effect = Instantiate(_effectWoodPrefab, spawnPos, _effectSpawnPoint.rotation);
        WoodCuttingVFX cuttingVfx = effect.GetComponent<WoodCuttingVFX>();
        cuttingVfx.Initiate(info, EGatherType.Wood);
    }

    private void SpawnSlashMagic(TerrainCell cell)
    {
        if (_woodNormalEffect == null || cell == null) return;

        Vector3 spawnPosition = GetSlashMagicSpawnPosition(cell);
        Quaternion spawnRotation = GetSlashMagicRotation();
        GameObject effect = Instantiate(_woodNormalEffect, spawnPosition, spawnRotation);

        if (_slashMagicLifetime > 0f)
        {
            Destroy(effect, _slashMagicLifetime);
        }
    }

    private Vector3 GetSlashMagicSpawnPosition(TerrainCell cell)
    {
        Vector3 spawnPosition = cell.transform.position + Vector3.up * _slashMagicHeightOffset;

        if (cell.CurrentObject != null)
        {
            Vector3 objectPosition = cell.CurrentObject.transform.position;
            spawnPosition.x = objectPosition.x;
            spawnPosition.z = objectPosition.z;

            Collider collider = cell.CurrentObject.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                spawnPosition.y = Mathf.Max(spawnPosition.y, collider.bounds.center.y);
            }
        }

        return spawnPosition;
    }

    private Quaternion GetSlashMagicRotation()
    {
        Vector3 forward = _owner?.PlayerOwner != null ? _owner.PlayerOwner.transform.forward : Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(_slashMagicRotationOffset);
    }

    private TerrainCell[] GetHorizontalAdjacentCells(TerrainCell centerCell)
    {
        if (centerCell == null) return new TerrainCell[0];
        if (TerrainGridManager.Instance == null) return new TerrainCell[0];
        if (_owner?.PlayerOwner == null) return new TerrainCell[0];

        Vector3 playerRight = _owner.PlayerOwner.transform.right;
        Vector3Int rightOffset = new Vector3Int(
            Mathf.RoundToInt(playerRight.x),
            0,
            Mathf.RoundToInt(playerRight.z));

        if (rightOffset == Vector3Int.zero)
            rightOffset = Vector3Int.right;

        Vector3Int centerGrid = centerCell.GridPosition;
        List<TerrainCell> result = new List<TerrainCell>();

        TerrainCell rightCell = TerrainGridManager.Instance.GetInteractionCell(centerGrid + rightOffset, false, out _);
        TerrainCell leftCell = TerrainGridManager.Instance.GetInteractionCell(centerGrid - rightOffset, false, out _);

        if (rightCell != null) result.Add(rightCell);
        if (leftCell != null) result.Add(leftCell);

        return result.ToArray();
    }

    private IEnumerator AnimPlayCoroutine()
    {
        yield return new WaitForSeconds(_idleTransition);
        _animAbility?.Play(EHelperAnim.Idle);
        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;

        if (cell != null && cell.CurrentObject != null && cell.Data.ObjectType == EGridObjectType.Tree)
            return;

        _owner.BeginAction();
        _stoneMineAbility?.JumpAndSmash(cell);
    }

    private TerrainCell GetInteractableCell(TerrainCell cell)
    {
        if (TerrainGridManager.Instance == null) return null;
        return TerrainGridManager.Instance.IsCellAvailableForInteraction(cell) ? cell : null;
    }
}
