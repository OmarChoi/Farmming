using UnityEngine;

public class PlayerHelperAbility : PlayerAbility
{
    [SerializeField] private KeyCode _equipKey = KeyCode.F;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;
    [SerializeField] private HelperPickup _currentHelper;

    private PlayerTerrainAbility _terrainAbility;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void Update()
    {
        if (_owner.IsUIOpen) return;

        if (Input.GetKeyDown(_equipKey))
            ToggleEquip();

        if (Input.GetKeyDown(_interactKey))
            TryInteract();
    }

    private void ToggleEquip()
    {
        if (_currentHelper == null) return;

        if (_currentHelper.IsEquipped)
            _currentHelper.UnEquip();
        else
            _currentHelper.Equip();
    }

    private void TryInteract()
    {
        if (_currentHelper == null || !_currentHelper.IsEquipped)
        {
            Debug.Log("곡룡 들고 있지 않음");
            return;
        }

        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null)
        {
            Debug.Log("앞에 셀 없음");
            return;
        }

        var action = _currentHelper.GetComponent<IHelperAction>();
        if (action != null)
            action.Interact(cell);
        else
            Debug.Log("이 곡룡은 행동이 없음");
    }
}