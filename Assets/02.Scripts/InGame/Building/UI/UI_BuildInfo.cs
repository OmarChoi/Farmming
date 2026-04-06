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
    private BuildingDataSO _buildingData;
    private int[] _ownedCounts;
    
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
        _buildingData = null;
        _ownedCounts = null;
    }

    public void SetData(BuildingDataSO buildingData, int[] ownedCounts)
    {
        _buildingData = buildingData;
        _ownedCounts = ownedCounts;
        BindData();
    }

    // Manager에서 선택된 건물 데이터를 읽어 UI에 바인딩
    private void BindData()
    {
        if (_buildingData == null)
        {
            _nameLabel.text = string.Empty;
            _descriptionLabel.text = string.Empty;
            _constructionDaysLabel.text = string.Empty;
            RefreshCostSlots(null);
            return;
        }

        // 기본 정보 표시
        _nameLabel.text = _buildingData.DisplayName;
        _descriptionLabel.text = _buildingData.Description;
        _constructionDaysLabel.text = _buildingData.ConstructionDays > 0
            ? string.Format(ConstructionDaysFormat, _buildingData.ConstructionDays)
            : InstantBuildText;

        // 건설 비용 슬롯 갱신
        RefreshCostSlots(_buildingData.Costs);
    }

    // 비용 슬롯을 필요한 만큼 생성/재사용하여 갱신
    private void RefreshCostSlots(IReadOnlyList<BuildingCostEntry> costs)
    {
        // 부족한 슬롯 생성 (풀링)
        // todo. Pooling 기반의 Slot 생성 방식으로 수정
        if (costs == null)
        {
            _panelTransform.sizeDelta = new Vector2(_panelTransform.sizeDelta.x, DefaultSize);
            ClearCostSlots();
            return;
        }

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
                int owned = (_ownedCounts != null && i < _ownedCounts.Length) ? _ownedCounts[i] : 0;
                _costSlots[i].Refresh(costs[i], owned);
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
