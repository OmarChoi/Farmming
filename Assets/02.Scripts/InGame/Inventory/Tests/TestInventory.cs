using UnityEngine;

public class TestInventory : MonoBehaviour
{
    [SerializeField] private ItemDataSO _woodItem;
    [SerializeField] private ItemDataSO _dirtItem;
    [SerializeField] private int _testGoldAmount = 1000;

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
            _inventoryAbility.AddItem(_woodItem);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            _inventoryAbility.AddItem(_dirtItem);

        if (Input.GetKeyDown(KeyCode.O))
            CurrencyManager.Instance?.AddGold(_testGoldAmount);
    }
}
