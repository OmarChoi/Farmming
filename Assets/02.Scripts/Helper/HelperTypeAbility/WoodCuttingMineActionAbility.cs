using TMPro;
using UnityEngine;

// 벌목 채굴: 좌클릭(벌목) / 우클릭(채굴)
public enum EGatherType
{
    Wood,
    Rock,
}

public class WoodCuttingMineActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] protected GameObject _effectWoodPrefab;
    [SerializeField] protected Transform _effectSpawnPoint;

    public void Interact(TerrainCell cell)
    {
        // 이펙트 소환
        GameObject _effect = Instantiate(
    _effectWoodPrefab,
    _effectSpawnPoint.position,
    _effectSpawnPoint.rotation
);
        WoodCuttingVfx cuttingVfx = _effect.GetComponent<WoodCuttingVfx>();
        cuttingVfx.Initiate(_owner.DataSO.GatherDamage, EGatherType.Wood);
    }

    public void InteractR(TerrainCell cell)
    {
        // 이펙트 소환
        GameObject _effect = Instantiate(
    _effectWoodPrefab,
    _effectSpawnPoint.position,
    _effectSpawnPoint.rotation
);
        WoodCuttingVfx cuttingVfx = _effect.GetComponent<WoodCuttingVfx>();
        cuttingVfx.Initiate(_owner.DataSO.GatherDamage, EGatherType.Wood);
    }
}
