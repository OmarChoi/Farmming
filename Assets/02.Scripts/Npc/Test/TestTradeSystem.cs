using UnityEngine;

public class TestTradeSystem : MonoBehaviour
{
    [Header("참고 데이터")]
    [SerializeField] private PlayerInventoryAbility _playerInventory;
    [SerializeField] private ShopData _shopData;

    [Header("테스트 키")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.T;

    private TradeService _tradeService;
    private bool _isTradeMode = false;

    private void Start()
    {
        if (_playerInventory == null)
        {
#if UNITY_EDITOR
            Debug.LogError("PlayerInventoryAbility 참조가 없습니다.");
#endif
            enabled = false;
            return;
        }

        if (_shopData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("ShopData 참조가 없습니다.");
#endif
            enabled = false;
            return;
        }

        _tradeService = new TradeService(_playerInventory);

#if UNITY_EDITOR
        Debug.Log("준비 완료. T 키로 거래 테스트 모드를 켜고 끌 수 있습니다.");
#endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _isTradeMode = !_isTradeMode;
#if UNITY_EDITOR
            Debug.Log($"거래 테스트 모드: {(_isTradeMode ? "활성화" : "비활성화")}");
#endif
        }

        if (!_isTradeMode) return;

        HandleBuyTestInput();
        HandleSellTestInput();
    }

    private void HandleBuyTestInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            TryBuyByIndex(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            TryBuyByIndex(1);
        }
    }

    private void HandleSellTestInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            TrySellBySlotIndex(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            TrySellBySlotIndex(1);
        }
    }

    private void TryBuyByIndex(int shopItemIndex)
    {
        if (_shopData.SellItems == null || _shopData.SellItems.Count <= shopItemIndex)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"상점 아이템 인덱스 {shopItemIndex} 는 존재하지 않습니다.");
#endif
            return;
        }

        ItemDataSO item = _shopData.SellItems[shopItemIndex];
        bool result = _tradeService.Buy(_shopData, item, 1);
    }

    private void TrySellBySlotIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _playerInventory.SlotCount)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"잘못된 슬롯 인덱스: {slotIndex}");
#endif
            return;
        }

        InventorySlot slot = _playerInventory.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"판매 실패 - 슬롯 {slotIndex} 이 비어 있습니다.");
#endif
            return;
        }

        string itemName = slot.Item.DisplayName;
        int beforeAmount = slot.Count;

        bool result = _tradeService.Sell(slotIndex, 1);
#if UNITY_EDITOR
        if (result)
        {
            Debug.Log($"판매 성공 - Slot: {slotIndex}, Item: {itemName}, Amount: 1, Before: {beforeAmount}");
        }
        else
        {
            Debug.LogWarning($"판매 실패 - Slot: {slotIndex}, Item: {itemName}");
        }
#endif
    }
}
