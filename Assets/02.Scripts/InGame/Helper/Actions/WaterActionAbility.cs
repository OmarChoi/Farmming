using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 관수 곡룡: FarmDry => FarmWet
public class WaterActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _waterVfxPrefab;
    [SerializeField] private float _vfxDuration = 0.5f;

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
        if (cell == null) return;
        if (_isActing) return;

        StartCoroutine(WaterCoroutine(cell));
    }

    public void InteractSecondary(TerrainCell cell) { }

    private IEnumerator WaterCoroutine(TerrainCell cell)
    {
        _isActing = true;

        Vector3 spawnPos = _mouthPoint != null
            ? _mouthPoint.position
            : _owner.transform.position;

        Vector3 targetPos = GetTargetPosition(cell);
        Vector3 direction = (targetPos - spawnPos).normalized;

        _animAbility?.Play(EHelperAnim.Water);

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
                ApplyWaterEffects(cell);
            });
        }

        yield return new WaitForSeconds(_vfxDuration);

        _animAbility?.Play(EHelperAnim.Idle);
        _isActing = false;
    }

    private void ApplyWaterEffects(TerrainCell cell)
    {
        foreach (IWaterEffect effect in _waterEffects)
        {
            if (effect.CanHandle(cell))
            {
                effect.Apply(cell);
                return;
            }
        }
        Debug.Log("물을 줄 수 있는 상태 아님");
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
