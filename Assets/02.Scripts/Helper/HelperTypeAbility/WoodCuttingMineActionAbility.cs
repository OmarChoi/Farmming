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

    private StoneMineAbility _stoneMineAbility;

    protected override void Awake()
    {
        base.Awake();
        _stoneMineAbility = _owner.GetAbility<StoneMineAbility>();
    }

    public void Interact(TerrainCell cell)
    {
        InteractPrimary(cell);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        // 이펙트 소환
        GameObject _effect = Instantiate(_effectWoodPrefab, _effectSpawnPoint.position, _effectSpawnPoint.rotation);
        WoodCuttingVFX cuttingVfx = _effect.GetComponent<WoodCuttingVFX>();
        cuttingVfx.Initiate(_owner.Data.GatherDamage, EGatherType.Wood);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // 이펙트 소환
        _stoneMineAbility?.JumpAndSmash(cell);
    }
}
