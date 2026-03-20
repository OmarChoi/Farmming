using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UI_ResourceTest : MonoBehaviour
{
    [SerializeField] private Transform _canvas;
    private Button _button;
    private GameObject _inventoryUI;
    private bool _isOpened;

    private void Start()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClickButton);
    }

    private void OnClickButton()
    {
        ToggleUI().Forget();
    }
    
    private async UniTaskVoid ToggleUI()
    {
        if (!_isOpened)
        {
            var inventoryPrefab = await ResourceManager.Instance.LoadAsync<GameObject>(AssetKey.UI.Inventory);
            _inventoryUI = Instantiate(inventoryPrefab, _canvas);
        }
        else
        {
            Destroy(_inventoryUI);
        }
        _isOpened = !_isOpened;
    }
}
