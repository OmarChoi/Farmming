using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SowLegendarySecondaryVFXAbility : HelperAbility
{
    [Header("레전더리 등급 파종 이펙트 (Secondary)")]
    [SerializeField] private GameObject _seedParticlePrefab;
    [SerializeField] private float _sweepDuration = 2f;       // 도착 지점이 좌→우로 이동하는 총 시간
    [SerializeField] private float _spawnInterval = 0.08f;    // 씨앗 발사 간격 (작을수록 연속적)
    [SerializeField] private float _arcHeight = 2.5f;         // 포물선 높이
    [SerializeField] private float _flightDuration = 0.4f;    // 씨앗 비행 시간
    [SerializeField] private float _particleLifetime = 0.5f;  // 착지 후 파티클 유지 시간
    [SerializeField] private float _targetHeight = 0.1f;      // 도착 지점 y 오프셋

    // mouthPoint 고정, 도착 지점만 orderedCells[0] → orderedCells[last]로 sweep.
    // sweep이 각 cell 위치에 도달하는 타이밍에 onCellLand 호출.
    // 모든 파티클 착지 후 onComplete 호출.
    public void SpawnEffects(Transform mouthPoint, List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand, Action onComplete)
    {
        if (orderedCells == null || orderedCells.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SweepCoroutine(mouthPoint, orderedCells, onCellLand, onComplete));
    }

    private IEnumerator SweepCoroutine(Transform mouthPoint, List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand, Action onComplete)
    {
        Vector3 startTarget = GetCellTarget(orderedCells[0]);
        Vector3 endTarget   = GetCellTarget(orderedCells[orderedCells.Count - 1]);

        for (int i = 0; i < orderedCells.Count; i++)
        {
            float t = orderedCells.Count > 1 ? (float)i / (orderedCells.Count - 1) : 0f;
            TerrainCell captured = orderedCells[i];
            StartCoroutine(DelayedCallback(t * _sweepDuration, () => onCellLand?.Invoke(captured)));
        }

        float elapsed = 0f;
        while (elapsed < _sweepDuration)
        {
            float t = elapsed / _sweepDuration;
            Vector3 currentTarget = Vector3.Lerp(startTarget, endTarget, t);
            SpawnSeedParticle(mouthPoint, currentTarget);

            yield return new WaitForSeconds(_spawnInterval);
            elapsed += _spawnInterval;
        }

        yield return new WaitForSeconds(_flightDuration + _particleLifetime);

        onComplete?.Invoke();
    }

    private void SpawnSeedParticle(Transform mouthPoint, Vector3 targetPos)
    {
        if (_seedParticlePrefab == null || mouthPoint == null) return;

        GameObject vfx = Instantiate(_seedParticlePrefab, mouthPoint.position, Quaternion.identity);
        vfx.transform.DOJump(targetPos, _arcHeight, 1, _flightDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(vfx, _particleLifetime));
    }

    private Vector3 GetCellTarget(TerrainCell cell)
    {
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf && cell.FarmTile.CropSpawnPoint != null)
            return cell.FarmTile.CropSpawnPoint.position;
        return cell.transform.position + Vector3.up * _targetHeight;
    }

    private IEnumerator DelayedCallback(float delay, Action callback)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}
