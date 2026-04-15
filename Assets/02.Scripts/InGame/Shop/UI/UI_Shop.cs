using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Shop : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_ShopItemSlot _slotPrefab;
    [SerializeField] private GameObject _uiShopRoot;
    [SerializeField] private TextMeshProUGUI _shopNameText;

    [Header("수량 팝업")]
    [SerializeField] private UI_TradeAmountPopup _tradeAmountPopup;

    [Header("닫기 버튼")]
    [SerializeField] private Button _exitButton;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _slideDistance = 300f;

    private RectTransform _shopRect;
    private Vector2 _shopOriginPosition;
    private Tween _slideTween;

    private readonly List<UI_ShopItemSlot> _slots = new();

    private ShopData _currentShopData;
    private TradeService _tradeService;

    public Action OnCloseRequested;

    private void Awake()
    {
        if (_uiShopRoot != null)
        {
            _shopRect = _uiShopRoot.GetComponent<RectTransform>();
            _shopOriginPosition = _shopRect.anchoredPosition;
            _uiShopRoot.SetActive(false);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnClickCloseButton);
        }
    }

    private void OnDestroy()
    {
        _slideTween?.Kill();
        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnClickCloseButton);
        }
    }

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
        PlayOpenAnimation();
    }

    public void Close()
    {
        PlayCloseAnimation();
    }

    public void CloseImmediate()
    {
        _slideTween?.Kill();

        if (_shopRect != null)
        {
            _shopRect.anchoredPosition = _shopOriginPosition;
        }

        if (_uiShopRoot != null)
        {
            _uiShopRoot.SetActive(false);
        }

        _currentShopData = null;
    }

    private void PlayOpenAnimation()
    {
        if (_shopRect == null) return;

        _slideTween?.Kill();

        _shopRect.anchoredPosition = _shopOriginPosition + Vector2.left * _slideDistance;
        _slideTween = _shopRect.DOAnchorPos(_shopOriginPosition, _slideDuration).SetEase(Ease.OutBack).SetLink(_uiShopRoot);
    }

    private void PlayCloseAnimation()
    {
        if (_shopRect == null)
        {
            _uiShopRoot.SetActive(false);
            _currentShopData = null;
            return;
        }

        _slideTween?.Kill();

        Vector2 target = _shopOriginPosition + Vector2.left * _slideDistance;
        _slideTween = _shopRect.DOAnchorPos(target, _slideDuration).SetEase(Ease.InBack).SetLink(_uiShopRoot)
            .OnComplete(() =>
            {
                _uiShopRoot.SetActive(false);
                _shopRect.anchoredPosition = _shopOriginPosition;
                _currentShopData = null;
            });
    }
    public void OnShopSlotClicked(UI_ShopItemSlot slot)
    {
        if (_currentShopData == null || slot.ItemData == null || _tradeService == null) return;
        if (_tradeAmountPopup == null || CurrencyManager.Instance == null) return;
        ItemDataSO item = slot.ItemData;

        int maxAffordableAmount = item.BuyCost <= 0
            ? 999
            : CurrencyManager.Instance.CurrentGold / item.BuyCost;

        if (maxAffordableAmount <= 0)
        {
#if UNITY_EDITOR
            Debug.Log("골드가 부족합니다.");
#endif
            return;
        }

        if (_tradeAmountPopup != null && _tradeAmountPopup.IsOpen) return;
        _tradeAmountPopup.OpenAsync(
            item,
            ETradeType.Buy,
            maxAffordableAmount,
            amount =>
            {
                bool success = _tradeService.Buy(_currentShopData, item, amount);

#if UNITY_EDITOR
                if (success)
                    Debug.Log($"구매 성공 - Item: {item.DisplayName}, Amount: {amount}");
                else
                    Debug.LogWarning($"구매 실패 - Item: {item.DisplayName}, Amount: {amount}");
#endif
            }).Forget();
    }

    public void OnClickCloseButton()
    {
        OnCloseRequested?.Invoke();
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
}
