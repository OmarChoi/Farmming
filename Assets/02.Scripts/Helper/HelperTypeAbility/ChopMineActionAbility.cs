using UnityEngine;

// 벌목 채굴: 좌클릭(벌목) / 우클릭(채굴)

public enum EChopMine
{
    Chop, // 벌목
    Mine // 채굴
}
public class ChopMineActionAbility : FarmBaseAbility
{
    [SerializeField] private EChopMine _mode;

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
                TryChop(cell);
                break;

            case EChopMine.Mine:
                TryMine(cell);
                break;
        }
    }

    private void TryChop(TerrainCell cell)
    {
        Debug.Log("벌목시도");
    }

    private void TryMine(TerrainCell cell)
    {
        Debug.Log("채굴시도");
    }
}
