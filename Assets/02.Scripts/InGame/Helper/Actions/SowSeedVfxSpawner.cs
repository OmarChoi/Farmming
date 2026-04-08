using System;
using UnityEngine;

public sealed class SowSeedVfxSpawner
{
    private readonly Func<Transform> _getMouthPoint;
    private readonly Func<GameObject> _getSeedVfxPrefab;

    public SowSeedVfxSpawner(Func<Transform> getMouthPoint, Func<GameObject> getSeedVfxPrefab)
    {
        _getMouthPoint = getMouthPoint;
        _getSeedVfxPrefab = getSeedVfxPrefab;
    }

    public bool HasMouthPoint => _getMouthPoint?.Invoke() != null;

    public Transform MouthPoint => _getMouthPoint?.Invoke();

    public void SpawnTo(FarmTile tile)
    {
        Transform mouthPoint = MouthPoint;
        if (tile == null || mouthPoint == null)
        {
            return;
        }

        Vector3 spawnPos = mouthPoint.position;
        Vector3 targetPos = tile.CropSpawnPoint != null
            ? tile.CropSpawnPoint.position
            : tile.transform.position;
        Vector3 direction = (targetPos - spawnPos).normalized;

        GameObject prefab = _getSeedVfxPrefab?.Invoke();
        if (prefab == null)
        {
            return;
        }

        GameObject vfxObj = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        SowVFX sowVfx = vfxObj.GetComponent<SowVFX>();
        sowVfx?.Launch(targetPos, direction);
    }
}
