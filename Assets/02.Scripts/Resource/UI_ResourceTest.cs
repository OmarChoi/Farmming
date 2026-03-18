using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UI_ResourceTest : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Transform _canvas;
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
            Debug.Log($"Prefab activeSelf: {inventoryPrefab.activeSelf}");
            _inventoryUI = Instantiate(inventoryPrefab, _canvas);
            Debug.Log($"Instance activeSelf: {_inventoryUI.activeSelf}");
            // _inventoryUI.gameObject.SetActive(true);
        }
        else
        {
            Destroy(_inventoryUI);
        }
        _isOpened = !_isOpened;
    }
}
