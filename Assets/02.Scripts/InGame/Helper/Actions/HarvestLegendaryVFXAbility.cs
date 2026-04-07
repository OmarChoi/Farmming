using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestLegendaryVFXAbility : HelperAbility
{
    [Header("레전더리 등급 수확 이펙트")]
    [SerializeField] private GameObject _auroraPrefab;
    [SerializeField] private GameObject _tornadoPrefab;

    [SerializeField] private float _tornadoSweepDistance = 4f;
    [SerializeField] private float _tornadoSweepDuration = 2f;
    [SerializeField] private float _tornadoSpawnHeight = 0.5f;

    public void SpawnEffects(TerrainCell centerCell, Vector3 rightDirection,
        List<(TerrainCell cell, Action onHarvest)> cellHarvests, Action onComplete)
    {
        GameObject aurora = null;
        if (_auroraPrefab != null)
        {
            aurora = Instantiate(_auroraPrefab, _owner.transform.position, Quaternion.identity);
            aurora.transform.SetParent(_owner.transform);
        }

        if (_tornadoPrefab == null)
        {
            foreach (var (_, harvest) in cellHarvests)
                harvest?.Invoke();
            if (aurora != null) Destroy(aurora);
            onComplete?.Invoke();
            return;
        }

        Vector3 centerPos = GetCellPosition(centerCell);

        float sweepDistance = _tornadoSweepDistance;
        foreach (var (cell, _) in cellHarvests)
        {
            float abs = Mathf.Abs(Vector3.Dot(GetCellPosition(cell) - centerPos, rightDirection));
            if (abs > sweepDistance) sweepDistance = abs;
        }

        float halfDuration = _tornadoSweepDuration * 0.5f;
        foreach (var (cell, onHarvest) in cellHarvests)
        {
            if (onHarvest == null) continue;

            float offset = Vector3.Dot(GetCellPosition(cell) - centerPos, rightDirection);
            float delay = CalculateHarvestDelay(offset, halfDuration, sweepDistance);

            Action capturedHarvest = onHarvest;
            StartCoroutine(DelayedHarvest(delay, capturedHarvest));
        }

        GameObject tornado = Instantiate(_tornadoPrefab, centerPos, Quaternion.identity);
        TornadoVFX tornadoVfx = tornado.GetComponent<TornadoVFX>();
        if (tornadoVfx != null)
        {
            tornadoVfx.Initialize(rightDirection, sweepDistance, _tornadoSweepDuration, () =>
            {
                if (aurora != null) Destroy(aurora);
                onComplete?.Invoke();
            });
        }
        else
        {
            float totalDuration = halfDuration + _tornadoSweepDuration + 2f;
            Destroy(tornado, totalDuration);
            if (aurora != null) Destroy(aurora, totalDuration);
            onComplete?.Invoke();
        }
    }

    private float CalculateHarvestDelay(float offset, float halfDuration, float sweepDistance)
    {
        if (offset <= 0f)
        {
            return halfDuration * Mathf.Clamp01(-offset / sweepDistance);
        }
        else
        {
            return halfDuration + _tornadoSweepDuration * ((offset + sweepDistance) / (2f * sweepDistance));
        }
    }

    private IEnumerator DelayedHarvest(float delay, Action onHarvest)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        onHarvest?.Invoke();
    }

    private Vector3 GetCellPosition(TerrainCell cell)
    {
        return cell.transform.position + Vector3.up * _tornadoSpawnHeight;
    }
}
