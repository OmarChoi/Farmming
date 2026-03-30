using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedDataItemSO", menuName = "Scriptable Objects/SeedDataItemSO")]
public class SeedItemDataSO : ItemDataSO
{
    [SerializeField] private List<SeedGrowthStageData> _seedGrowthStage;

    [SerializeField] private int _harvestAmountMin;
    [SerializeField] private int _harvestAmountMax;
    [SerializeField] private ItemDataSO _harvestItem;
    [SerializeField] private ESeedGrade _seedGrade;

    public List<SeedGrowthStageData> SeedGrowthStage => _seedGrowthStage;
    public int HarvestAmountMin => _harvestAmountMin;
    public int HarvestAmountMax => _harvestAmountMax;
    public ItemDataSO HarvestItem => _harvestItem;
    public ESeedGrade SeedGrade => _seedGrade;
}
