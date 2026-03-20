using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UI_Shop : MonoBehaviour
{
    [Header("컴포넌트 옵션")]
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_ShopItemSlot _slotPrefab;
    [SerializeField] private GameObject _uiShopRoot;
    [SerializeField] private TextMeshProUGUI _shopNameText;

    [Header("닫기 버튼")]
    [SerializeField] private Button _exitButton;

    private readonly List<UI_ShopItemSlot> _slots = new();

    private ShopData _currentShopData;
    private TradeService _tradeService;

    public Action OnCloseRequested;

    public void Init(TradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public void Open(ShopData shopData)
    {
        _currentShopData = shopData;
        _shopNameText.text = shopData.ShopName;
        _uiShopRoot.SetActive(true);

        CreateOrRefreshSlots();
    }

    public void Close()
    {
        _uiShopRoot.SetActive(false);
        _currentShopData = null;
    }

    private void CreateOrRefreshSlots()
    {
        if (_currentShopData == null || _currentShopData.SellItems == null) return;

        int itemCount = _currentShopData.SellItems.Count;

        while (_slots.Count < itemCount)
        {
            UI_ShopItemSlot newSlot = Instantiate(_slotPrefab, _slotParent);
            newSlot.Init(this, _slots.Count);
            _slots.Add(newSlot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            bool isActive = i < itemCount;
            _slots[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                _slots[i].Refresh(_currentShopData.SellItems[i]);
            }
        }
    }

    public void OnShopSlotClicked(UI_ShopItemSlot slot)
    {
        if (_currentShopData == null || slot.ItemData == null || _tradeService == null) return;

        bool success = _tradeService.Buy(_currentShopData, slot.ItemData, 1);

#if UNITY_EDITOR
        if (success)
        {
            Debug.Log($"구매 성공 - Item: {slot.ItemData.name}, Amount: 1");
        }
        else
        {
            Debug.LogWarning($"구매 실패 - Item: {(slot.ItemData != null ? slot.ItemData.name : "null")}");
        }
#endif
    }

    public void OnClickCloseButton()
    {
        OnCloseRequested?.Invoke();
    }
}
