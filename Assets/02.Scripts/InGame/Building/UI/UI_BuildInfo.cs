using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_BuildInfo : UIBase
{
    private const float DefaultSize = 200;
    private const float HeightPerCostItem = 200;
    private const int CostColumns = 2;
    private const string ConstructionDaysFormat = "건설 일수 : {0}일";
    private const string InstantBuildText = "즉시 완성";
    
    private RectTransform _panelTransform;
    
    [Header("건물 정보")]
    [SerializeField] private TextMeshProUGUI _nameLabel;
    [SerializeField] private TextMeshProUGUI _constructionDaysLabel;
    [SerializeField] private TextMeshProUGUI _descriptionLabel;

    [Header("건설 비용")]
    [SerializeField] private Transform _costSlotParent;
    [SerializeField] private BuildCostItemSlot _costItemSlotPrefab;

    private readonly List<BuildCostItemSlot> _costSlots = new List<BuildCostItemSlot>();

    private void Awake()
    {
        _panelTransform = GetComponent<RectTransform>();
    }
    
    protected override void OnOpen()
    {
        BindData();
    }

    protected override void OnClose()
    {
        ClearCostSlots();
    }

    // Manager에서 선택된 건물 데이터를 읽어 UI에 바인딩
    private void BindData()
    {
        BuildingDataSO data = BuildingManager.Instance?.SelectedBuilding;
        if (data == null) return;

        // 기본 정보 표시
        _nameLabel.text = data.DisplayName;
        _descriptionLabel.text = data.Description;
        _constructionDaysLabel.text = data.ConstructionDays > 0 ? string.Format(ConstructionDaysFormat, data.ConstructionDays) : InstantBuildText;

        // 건설 비용 슬롯 갱신
        RefreshCostSlots(data.Costs);
    }

    // 비용 슬롯을 필요한 만큼 생성/재사용하여 갱신
    private void RefreshCostSlots(IReadOnlyList<BuildingCostEntry> costs)
    {
        // 부족한 슬롯 생성 (풀링)
        // todo. Pooling 기반의 Slot 생성 방식으로 수정
        int nCostItem = costs.Count;
        float newHeight = DefaultSize + Mathf.Ceil((float)nCostItem / CostColumns) * HeightPerCostItem;
        _panelTransform.sizeDelta = new Vector2(_panelTransform.sizeDelta.x, newHeight);
        while (_costSlots.Count < costs.Count)
        {
            BuildCostItemSlot itemSlot = Instantiate(_costItemSlotPrefab, _costSlotParent);
            _costSlots.Add(itemSlot);
        }

        for (var i = 0; i < _costSlots.Count; i++)
        {
            if (i < costs.Count)
            {
                _costSlots[i].Refresh(costs[i]);
                _costSlots[i].gameObject.SetActive(true);
            }
            else
            {
                _costSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void ClearCostSlots()
    {
        foreach (BuildCostItemSlot slot in _costSlots)
        {
            slot.gameObject.SetActive(false);
        }
    }
}
