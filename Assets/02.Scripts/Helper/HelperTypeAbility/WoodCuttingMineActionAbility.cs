using UnityEngine;

// 벌목 채굴: 좌클릭(벌목) / 우클릭(채굴)
public enum EGatherType
{
    Wood,
    Stone,
}

public class WoodCuttingMineActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] protected GameObject _effectWoodPrefab;
    [SerializeField] protected GameObject _effectStonePrefab;
    [SerializeField] protected Transform _effectSpawnPoint;

    public void Interact(TerrainCell cell)
    {
        // 이펙트 소환
        GameObject _effect = Instantiate(_effectWoodPrefab, _effectSpawnPoint.position, _effectSpawnPoint.rotation);
        WoodCuttingVFX cuttingVfx = _effect.GetComponent<WoodCuttingVFX>();
        cuttingVfx.Initiate(_owner.Data.GatherDamage, EGatherType.Wood);
    }

    public void InteractL(TerrainCell cell)
    {
        // 이펙트 소환
        GameObject _effect = Instantiate(_effectStonePrefab, _effectSpawnPoint.position, _effectSpawnPoint.rotation);
        WoodCuttingVFX cuttingVfx = _effect.GetComponent<WoodCuttingVFX>();
        cuttingVfx.Initiate(_owner.Data.GatherDamage, EGatherType.Stone);
    }
}
