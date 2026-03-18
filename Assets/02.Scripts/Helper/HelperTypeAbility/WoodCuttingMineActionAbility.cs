using UnityEngine;

// 벌목 채굴: 좌클릭(벌목) / 우클릭(채굴)

public enum EChopMine
{
    Chop, // 벌목
    Mine // 채굴
}
public class WoodCuttingMineActionAbility : FarmBaseAbility
{
    [SerializeField] private EChopMine _mode;

    private IGatherable _currentTarget;
    private HelperVfx _vfx;

    protected override void Awake()
    {
        base.Awake();
        _vfx = GetComponent<HelperVfx>();

        if(_vfx != null )
        {
            _vfx.OnWoodCuttingHitEvent += OnChopHit;
        }
    }

    private void OnDestroy()
    {
        if(_vfx != null )
        { 
            _vfx.OnWoodCuttingHitEvent -= OnChopHit;
        }
    }

    public void SetMode(EChopMine mode)
    {
        _mode = mode;
    }

    public override void Interact(TerrainCell cell)
    {
        if(cell == null)
        {
            return;
        }

        switch(_mode)
        {
            case EChopMine.Chop:
                TryWoodCutting(cell);
                break;

            case EChopMine.Mine:
                TryMine(cell);
                break;
        }
    }

    private void TryWoodCutting(TerrainCell cell)
    {
        if(cell.CurrentObject == null)
        {
            Debug.Log("벌목할 나무 없음");
            return;
        }

        if(!cell.CurrentObject.TryGetComponent<IGatherable>(out IGatherable gatherable))
        {
            Debug.Log("벌목 가능한 나무 아님");
            return;
        }

        _currentTarget = gatherable;

        Vector3 targetPosition = cell.CurrentObject.transform.position;

        if(_vfx != null)
        {
            _vfx.PlayChop(targetPosition);
        }
        else
        {
            OnChopHit();
        }

    }

    public void OnChopHit()
    {
        if(_currentTarget == null)
        {
            return;
        }
        _currentTarget.TryGather(_owner.Data.GatherDamage);
        _currentTarget = null;
        Debug.Log("벌목시도");
    }

    private void TryMine(TerrainCell cell)
    {
        if(cell.CurrentObject == null)
        {
            Debug.Log("채굴할 돌 없음");
            return;
        }

        if(!cell.CurrentObject.TryGetComponent(out IGatherable gatherable))
        {
            Debug.Log("채굴 가능한 돌 아님");
            return;
        }

        gatherable.TryGather(_owner.Data.GatherDamage);
        Debug.Log("채굴시도");
    }
}
