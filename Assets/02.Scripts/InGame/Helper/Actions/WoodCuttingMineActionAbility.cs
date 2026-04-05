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

    [SerializeField] private float _wideGatherOffset = 2f;

    private StoneMineAbility _stoneMineAbility;
    private HelperAnimationAbility _animAbility;
    private RangeBoostEffect _rangeBoostEffect;

    protected override void Awake()
    {
        base.Awake();
        _stoneMineAbility = _owner.GetAbility<StoneMineAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _rangeBoostEffect = _owner.GetAbility<RangeBoostEffect>();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _owner?.EndAction();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.WoodCutting);

        GatheringInfo info = new GatheringInfo(_owner);

        bool isWideActive = _rangeBoostEffect != null && _rangeBoostEffect.IsActive;
        if (isWideActive)
        {
            Vector3 playerRight = _owner.PlayerOwner.transform.right;
            SpawnWoodVFX(info, Vector3.zero);                         
            SpawnWoodVFX(info, playerRight * _wideGatherOffset);  
            SpawnWoodVFX(info, -playerRight * _wideGatherOffset);   
        }
        else
        {
            SpawnWoodVFX(info, Vector3.zero);
        }

        StartCoroutine(AnimPlayCoroutine());
    }

    private void SpawnWoodVFX(GatheringInfo info, Vector3 worldOffset)
    {
        Vector3 spawnPos = _effectSpawnPoint.position + worldOffset;
        GameObject effect = Instantiate(_effectWoodPrefab, spawnPos, _effectSpawnPoint.rotation);
        WoodCuttingVFX cuttingVfx = effect.GetComponent<WoodCuttingVFX>();
        cuttingVfx.Initiate(info, EGatherType.Wood);
    }

    private IEnumerator AnimPlayCoroutine()
    {
        yield return new WaitForSeconds(_idleTransition);
        _animAbility?.Play(EHelperAnim.Idle);
        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        _owner.BeginAction();
        _stoneMineAbility?.JumpAndSmash(cell);
    }
}
