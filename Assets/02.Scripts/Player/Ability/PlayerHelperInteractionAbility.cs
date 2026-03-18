using UnityEngine;

public class PlayerHelperInteractionAbility : PlayerAbility
{
    [SerializeField] private KeyCode _equipKey = KeyCode.F;
    [SerializeField] private Transform _equipSlot;

    private PlayerTerrainAbility _terrainAbility;
    private HelperController _currentHelper;

    public HelperController CurrentHelper => _currentHelper;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void Update()
    {
        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_equipKey))
            ToggleEquip();

        if (_currentHelper != null
            && _currentHelper.State == EHelperState.Equipped
            && Input.GetMouseButtonDown(0))
        {
            TryInteract();
        }
    }

    public void Summon(HelperController helper)
    {
        if (_currentHelper != null)
            Unsummon();

        _currentHelper = helper;
        _currentHelper.Summon(transform);
    }

    public void Unsummon()
    {
        if (_currentHelper == null) return;

        if (_currentHelper.State == EHelperState.Equipped)
            _currentHelper.Unequip();

        _currentHelper.gameObject.SetActive(false);
        _currentHelper = null;
    }

    private void ToggleEquip()
    {
        if (_currentHelper == null) return;

        if (_currentHelper.State == EHelperState.Equipped)
            _currentHelper.Unequip();
        else
            _currentHelper.Equip(_equipSlot);
    }

    private void TryInteract()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _currentHelper.Interact(cell);
    }
}