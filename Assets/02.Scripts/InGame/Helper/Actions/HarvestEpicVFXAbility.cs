using System;
using System.Collections;
using UnityEngine;

public class HarvestEpicVFXAbility : HelperAbility
{
    [Header("에픽 등급 수확 이펙트")]
    [SerializeField] private GameObject _sproutTornadoPrefab;
    [SerializeField] private float _totalDuration = 2f;
    [SerializeField] private float _tornadoLifetime = 1.5f;
    [SerializeField] private float _tornadoSpawnHeight = 0.5f;

    // 순서: 가운데 → 왼쪽 → 오른쪽, 모두 _totalDuration 안에 완료
    public void SpawnEffects(TerrainCell centerCell, Action<TerrainCell> onCellHarvest, Action onComplete)
    {
        Vector3Int rightOffset = GetGridRightOffset();

        TerrainCell leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset);
        TerrainCell rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset);

        float interval = _totalDuration / 3f;

        StartCoroutine(SpawnSequence(centerCell, leftCell, rightCell, interval, onCellHarvest, onComplete));
    }

    private IEnumerator SpawnSequence(
        TerrainCell centerCell, TerrainCell leftCell, TerrainCell rightCell,
        float interval, Action<TerrainCell> onCellHarvest, Action onComplete)
    {
        SpawnTornadoAt(centerCell);
        onCellHarvest?.Invoke(centerCell);

        yield return new WaitForSeconds(interval);

        if (leftCell != null && leftCell.Data.IsTop)
        {
            SpawnTornadoAt(leftCell);
            onCellHarvest?.Invoke(leftCell);
        }

        yield return new WaitForSeconds(interval);

        if (rightCell != null && rightCell.Data.IsTop)
        {
            SpawnTornadoAt(rightCell);
            onCellHarvest?.Invoke(rightCell);
        }

        yield return new WaitForSeconds(interval);

        onComplete?.Invoke();
    }

    private void SpawnTornadoAt(TerrainCell cell)
    {
        if (_sproutTornadoPrefab == null) return;

        Vector3 pos = cell.transform.position + Vector3.up * _tornadoSpawnHeight;
        GameObject tornado = Instantiate(_sproutTornadoPrefab, pos, Quaternion.identity);
        Destroy(tornado, _tornadoLifetime);
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null) return Vector3Int.right;
        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }
}
