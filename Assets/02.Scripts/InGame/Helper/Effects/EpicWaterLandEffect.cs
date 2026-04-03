using UnityEngine;

public class EpicWaterLandEffect : MonoBehaviour
{
    private bool _applied = false;

    private void OnParticleCollision(GameObject other)
    {
        if (_applied) return;

        TerrainCell cell = other.GetComponentInParent<TerrainCell>();
        if(cell == null) return;

        FarmTile farmTile = cell.FarmTile;
        if (farmTile == null || !farmTile.gameObject.activeSelf) return;
        if (farmTile.StateMachine.CurrentStateType != EFarmTileStateType.FarmDry) return;

        _applied = true;
        farmTile.Water();
        Debug.Log("충돌");
    }
}
