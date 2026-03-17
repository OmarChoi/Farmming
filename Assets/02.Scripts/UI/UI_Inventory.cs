using UnityEngine;
using UnityEngine.UI;

public class UI_Inventory : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private UI_Slot _uiSlotPrefab;
    [SerializeField] private ScrollRect _scrollRect;

    private PlayerInventoryAbility _inventoryAbility;
    private UI_Slot[] _slotUIs;
    private UI_Slot _selectedSlot;

    private TradeService _tradeService;
    private EInventoryClickMode _clickMode = EInventoryClickMode.Normal;

    private void Awake()
    {
        _panel.SetActive(false);
        PlayerInventoryAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Bind(PlayerInventoryAbility ability)
    {
        Unbind();

        _inventoryAbility = ability;
        _inventoryAbility.OnToggle += OnToggle;
        _inventoryAbility.OnSlotChanged += RefreshSlot;
        _inventoryAbility.OnInventoryResized += RebuildSlots;

        CreateSlots();
        RefreshAll();
    }

    private void Unbind()
    {
        if (_inventoryAbility == null) return;

        _inventoryAbility.OnToggle -= OnToggle;
        _inventoryAbility.OnSlotChanged -= RefreshSlot;
        _inventoryAbility.OnInventoryResized -= RebuildSlots;
        _inventoryAbility = null;
    }

    private void OnToggle(bool open)
    {
        _panel.SetActive(open);
        _selectedSlot = null;

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (open)
            RefreshAll();
    }

    // 슬롯 생성

    private void CreateSlots()
    {
        _slotUIs = new UI_Slot[_inventoryAbility.SlotCount];

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            var slotUI = Instantiate(_uiSlotPrefab, _slotContainer);
            slotUI.Init(this, i);
            _slotUIs[i] = slotUI;
        }
    }

    private void RebuildSlots()
    {
        foreach (var slot in _slotUIs)
            Destroy(slot.gameObject);

        CreateSlots();
        RefreshAll();
    }

    // 갱신

    private void RefreshAll()
    {
        for (int i = 0; i < _slotUIs.Length; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (index < 0 || index >= _slotUIs.Length) return;
        _slotUIs[index].Refresh(_inventoryAbility.GetSlot(index));
    }

    // 클릭 모드 선택

    public void SetClickMode(EInventoryClickMode mode)
    {
        _clickMode = mode;
        _selectedSlot = null;
    }

    public void OnSlotClicked(UI_Slot clicked)
    {
        switch (_clickMode)
        {
            case EInventoryClickMode.Normal:
                HandleNormalClick(clicked);
                break;

            case EInventoryClickMode.Trading:
                HandleSellClick(clicked);
                break;
        }
    }

    // 클릭으로 교환

    private void HandleNormalClick(UI_Slot clicked)
    {
        if (_selectedSlot == null)
        {
            _selectedSlot = clicked;
            return;
        }

        _inventoryAbility.SwapSlots(_selectedSlot.SlotIndex, clicked.SlotIndex);
        _selectedSlot = null;
    }

    // 클릭으로 판매

    public void Init(TradeService tradeService)
    {
        _tradeService = tradeService;
    }

    private void HandleSellClick(UI_Slot clicked)
    {
        if (_tradeService == null) return;

        bool success = _tradeService.Sell(clicked.SlotIndex, 1);

#if UNITY_EDITOR
        if (success)
        {
            Debug.Log($"판매 성공 - Slot: {clicked.SlotIndex}, Amount: 1");
        }
        else
        {
            Debug.LogWarning($"판매 실패 - Slot: {clicked.SlotIndex}");
        }
#endif
    }
}