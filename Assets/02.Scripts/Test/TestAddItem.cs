using Unity.VisualScripting;
using UnityEngine;

public class TestAddItem : MonoBehaviour
{
    [SerializeField] private KeyCode _inputKey;
    [SerializeField] private ItemDatabase _database;
    private PlayerInventoryAbility _inventory;
    private bool _isActive;
    
    private void OnEnable()
    {
        GameSceneInit.OnCompleteInitialize += ActiveCheatKey;
    }

    private void OnDisable()
    {
        GameSceneInit.OnCompleteInitialize -= ActiveCheatKey;
    }
    
    private void ActiveCheatKey()
    {
        _inventory = FindFirstObjectByType<PlayerInventoryAbility>();
        if (_inventory == null)
        {
            enabled = false;
        }
        _isActive = true;
        GameSceneInit.OnCompleteInitialize -= ActiveCheatKey;
    }

    private void Update()
    {
        if (!_isActive) return;
        if (Input.GetKeyDown(_inputKey))
        {
            foreach (ItemDataSO item in _database.Items)
            {
                _inventory.AddItem(item, item.MaxStack - 1);
            }
            if (CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.AddGold(100000000);
        }
    }
}
