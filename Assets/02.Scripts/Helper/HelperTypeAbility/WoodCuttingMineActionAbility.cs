using System.Collections;
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
    [SerializeField] private float _idleTransition = 0.5f;

    private StoneMineAbility _stoneMineAbility;
    private HelperAnimationAbility _animAbility;

    protected override void Awake()
    {
        base.Awake();
        _stoneMineAbility = _owner.GetAbility<StoneMineAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public void Interact(TerrainCell cell)
    {
        InteractPrimary(cell);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        _animAbility?.Play(EHelperAnim.WoodCutting);

        // 이펙트 소환
        GameObject effect = Instantiate(_effectWoodPrefab, _effectSpawnPoint.position, _effectSpawnPoint.rotation);
        WoodCuttingVFX cuttingVfx = effect.GetComponent<WoodCuttingVFX>();
        GatheringInfo info = new GatheringInfo(_owner);
        cuttingVfx.Initiate(info, EGatherType.Wood);

        StartCoroutine(AnimPlayCoroutine());
    }

    private IEnumerator AnimPlayCoroutine()
    {
        yield return new WaitForSeconds(_idleTransition);
        _animAbility?.Play(EHelperAnim.Idle);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // 이펙트 소환
        _stoneMineAbility?.JumpAndSmash(cell);
    }
}
