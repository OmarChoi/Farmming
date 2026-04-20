using System;
using UnityEngine;

public class EpicWaterLandEffect : MonoBehaviour
{
    private TerrainCell _targetCell;
    private bool _isCenter;
    private Action<TerrainCell, bool> _onImpact;
    private Action<TerrainCell, bool> _onCellLand;
    private bool _impacted = false;
    private bool _applied = false;

    public void Initialize(TerrainCell cell, bool isCenter,
        Action<TerrainCell, bool> onImpact, Action<TerrainCell, bool> onCellLand)
    {
        _targetCell = cell;
        _isCenter = isCenter;
        _onImpact = onImpact;
        _onCellLand = onCellLand;
    }

    private void OnParticleCollision(GameObject other)
    {
        TerrainCell cell = other.GetComponentInParent<TerrainCell>();
        if (cell == null || cell != _targetCell) return;

        if (!_impacted)
        {
            _impacted = true;
            _onImpact?.Invoke(cell, _isCenter);
        }

        if (_applied) return;

        FarmTile farmTile = cell.FarmTile;
        if (farmTile == null || !farmTile.gameObject.activeSelf) return;
        if (farmTile.StateMachine.CurrentStateType != EFarmTileStateType.FarmDry) return;

        _applied = true;
        _onCellLand?.Invoke(cell, _isCenter);
    }
}
