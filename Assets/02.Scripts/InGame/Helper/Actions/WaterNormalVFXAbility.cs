using System;
using UnityEngine;

public class WaterNormalVFXAbility : HelperAbility, IWaterGradeVFX
{
    [SerializeField] private GameObject _waterVfxPrefab;

    private HelperAnimationAbility _animAbility;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public void BeginAction(Action onWaterOpen, Action onComplete)
    {
        _animAbility?.Play(EHelperAnim.Water);
    }

    public void SpawnHelperVFX()
    {
        // Normal 등급은 헬퍼 본체 VFX 없음
    }

    public void SpawnCellVFX(TerrainCell cell, Vector3 targetPos, bool isCenter,
        Vector3 spawnPos, Action<TerrainCell, bool> onCellLand)
    {
        if (_waterVfxPrefab == null) return;

        Vector3 direction = (targetPos - spawnPos).normalized;
        TerrainCell capturedCell = cell;
        bool capturedIsCenter = isCenter;

        GameObject vfxObj = Instantiate(_waterVfxPrefab, spawnPos, Quaternion.identity);
        WaterVFX waterVfx = vfxObj.GetComponent<WaterVFX>();
        waterVfx?.Launch(targetPos, direction, () =>
        {
            onCellLand?.Invoke(capturedCell, capturedIsCenter);
        });
    }

    public void Cancel() { }
}
