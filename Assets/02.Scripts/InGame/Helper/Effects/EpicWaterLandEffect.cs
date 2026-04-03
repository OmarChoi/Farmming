using System;
using UnityEngine;

public class EpicWaterLandEffect : MonoBehaviour
{
    private TerrainCell _targetCell;
    private bool _isCenter;
    private Action<TerrainCell, bool> _onCellLand;
    private bool _applied = false;

    public void Initialize(TerrainCell cell, bool isCenter, Action<TerrainCell, bool> onCellLand)
    {
        _targetCell = cell;
        _isCenter = isCenter;
        _onCellLand = onCellLand;
    }

    private void OnParticleCollision(GameObject other)
    {
        if (_applied) return;

        TerrainCell cell = other.GetComponentInParent<TerrainCell>();
        if (cell == null || cell != _targetCell) return;

        FarmTile farmTile = cell.FarmTile;
        if (farmTile == null || !farmTile.gameObject.activeSelf) return;
        if (farmTile.StateMachine.CurrentStateType != EFarmTileStateType.FarmDry) return;

        _applied = true;
        _onCellLand?.Invoke(cell, _isCenter);
    }
}
