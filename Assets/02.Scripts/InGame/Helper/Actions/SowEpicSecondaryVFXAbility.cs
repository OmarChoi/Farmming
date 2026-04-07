using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SowEpicSecondaryVFXAbility : HelperAbility
{
    [Header("에픽 등급 파종 이펙트 (Secondary)")]
    [SerializeField] private GameObject _sowEpicParticlePrefab;
    [SerializeField] private float _arcHeight = 3f;
    [SerializeField] private float _flightDuration = 0.6f;
    [SerializeField] private float _particleLifetime = 1f;
    [SerializeField] private float _totalDuration = 2f;

    public void SpawnEffects(Transform mouthPoint, List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand, Action onComplete, Action onEachSpawn = null)
    {
        if (orderedCells == null || orderedCells.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SpawnSequence(mouthPoint, orderedCells, onCellLand, onComplete, onEachSpawn));
    }

    private IEnumerator SpawnSequence(Transform mouthPoint, List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand, Action onComplete, Action onEachSpawn)
    {
        float interval = orderedCells.Count > 1
            ? _totalDuration / (orderedCells.Count - 1)
            : 0f;

        int remaining = orderedCells.Count;

        for (int i = 0; i < orderedCells.Count; i++)
        {
            TerrainCell captured = orderedCells[i];

            if (_sowEpicParticlePrefab == null || mouthPoint == null)
            {
                onEachSpawn?.Invoke();
                onCellLand?.Invoke(captured);
                remaining--;
                if (remaining == 0) onComplete?.Invoke();
            }
            else
            {
                onEachSpawn?.Invoke();

                Vector3 spawnPos = mouthPoint.position;
                Vector3 targetPos = GetCellTopPosition(captured);

                GameObject vfx = Instantiate(_sowEpicParticlePrefab, spawnPos, Quaternion.identity);
                vfx.transform.DOJump(targetPos, _arcHeight, 1, _flightDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        onCellLand?.Invoke(captured);
                        Destroy(vfx, _particleLifetime);
                        remaining--;
                        if (remaining == 0) onComplete?.Invoke();
                    });
            }

            if (i < orderedCells.Count - 1)
                yield return new WaitForSeconds(interval);
        }
    }

    private Vector3 GetCellTopPosition(TerrainCell cell)
    {
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf && cell.FarmTile.CropSpawnPoint != null)
            return cell.FarmTile.CropSpawnPoint.position;
        return cell.transform.position + Vector3.up * 0.1f;
    }
}
