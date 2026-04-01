using UnityEngine;

public class TestSeedAdd : MonoBehaviour
{
    [SerializeField] private ItemDataSO _seedItem;
    [SerializeField] private int _amount = 10;

    [SerializeField] private PlayerInventoryAbility _inventory;

    private void Start()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
    }

    private void OnDestroy()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
    }

    private void OnPlayerReady(PlayerInventoryAbility inventory)
    {
        _inventory = inventory;
    }

    private void Update()
    {
        // T키 누르면 씨앗 추가
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (_inventory != null && _seedItem != null)
            {
                _inventory.AddItem(_seedItem, _amount);
                Debug.Log($"씨앗 {_amount}개 추가");
            }
        }
    }
}