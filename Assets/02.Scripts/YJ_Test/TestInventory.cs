using UnityEngine;

public class TestInventory : MonoBehaviour
{
    [SerializeField] private ItemData _woodItem;
    [SerializeField] private ItemData _dirtItem;

    [SerializeField] private PlayerInventoryAbility _inventoryAbility;

    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
    }

    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
        _inventoryAbility = ability;
    }

    private void Update()
    {
        if (_inventoryAbility == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            _inventoryAbility.Inventory.AddItem(_woodItem);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            _inventoryAbility.Inventory.AddItem(_dirtItem);
    }
}