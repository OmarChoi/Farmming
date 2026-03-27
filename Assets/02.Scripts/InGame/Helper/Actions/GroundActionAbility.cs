using UnityEngine;

public class GroundActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private ItemDataSO _dirtItem;
    [SerializeField] private int _toolLevel = 1;
    [SerializeField] private int _getDirtAmount = 1;
    [SerializeField] private int _generateDirtAmount = 1;

    private HelperAnimationAbility _animAbility;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        _animAbility = _owner.GetComponent<HelperAnimationAbility>();
    }

    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null)
        {
            return;
        }

        if(cell.Data.ObjectType == EGridObjectType.FarmLand)
        {
            return;
        }

        if(!cell.Data.CanDig(_toolLevel))
        {
            return;
        }

        _owner.BeginAction();

        _animAbility?.Play(EHelperAnim.Eat);

        bool dug = cell.TryDig(_toolLevel);
        if (dug)
        {
            PlayerInventoryAbility inventory = GetInventory();
            if (inventory != null && _dirtItem != null)
            {
                inventory.AddItem(_dirtItem, _getDirtAmount);
            }

        }

        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if(cell == null)
        {
            return;
        }

        PlayerInventoryAbility inventory = GetInventory();
        if(inventory == null)
        {
            return;
        }

        int dirtSlotIndex = FindDirtSlotIndex(inventory);
        if(dirtSlotIndex < 0)
        {
            Debug.Log("인벤토리에 흙 없음");
            return;
        }

        _owner.BeginAction();

        _animAbility?.Play(EHelperAnim.Eat);

        Vector3Int targetPos = GetPlacePosition(cell);

        TerrainGridManager.Instance.SetCell(targetPos, new TerrainCellData(ECellType.Dirt, cell.Data.TileType, _generateDirtAmount, EGridObjectType.None,0,false,isTop: true));

        inventory.RemoveAt(dirtSlotIndex, _generateDirtAmount);

        _owner.EndAction();
    }

    private int FindDirtSlotIndex(PlayerInventoryAbility inventory)
    {
        for(int i = 0; i<inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            if(slot == null || slot.IsEmpty)
            {
                continue;
            }

            if(slot.Item == _dirtItem)
            {
                return i;
            }
        }

        return -1;
    }

    private Vector3Int GetPlacePosition(TerrainCell cell)
    {
        Vector3Int cellPos = cell.GridPosition;

        if(cell.Data.CellType == ECellType.Empty)
        {
            return cellPos;
        }

        return cellPos + Vector3Int.up;
    }

    private void OnDisable()
    {
        _owner?.EndAction();
    }
}
