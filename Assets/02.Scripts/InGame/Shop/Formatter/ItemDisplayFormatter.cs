using UnityEngine;

public static class ItemDisplayFormatter
{
    public static string GetName(ItemDataSO item)
    {
        return item == null ? string.Empty : item.DisplayName;
    }

    public static string GetExplanation(ItemDataSO item)
    {
        return item == null ? string.Empty : item.DisplayExplanation;
    }

    public static Sprite GetIcon(ItemDataSO item)
    {
        return item == null ? null : item.Icon;
    }

    public static string GetBuyCostText(ItemDataSO item)
    {
        if (item == null) return string.Empty;
        return item.BuyCost.ToString("N0");
    }

    public static string GetSellCostText(ItemDataSO item)
    {
        if (item == null) return string.Empty;
        return item.SellCost.ToString("N0");
    }

    public static string GetCostText(ItemDataSO item, ETradeType tradeType)
    {
        if (item == null) return string.Empty;

        int cost = tradeType == ETradeType.Buy ? item.BuyCost : item.SellCost;
        return cost.ToString("N0");
    }

    public static string GetTotalCostText(ItemDataSO item, ETradeType tradeType, int amount)
    {
        int unit = GetUnitCost(item, tradeType);
        int total = unit * amount;
        return $"총 {total:N0}";
    }

    public static int GetUnitCost(ItemDataSO item, ETradeType tradeType)
    {
        if (item == null) return 0;
        return tradeType == ETradeType.Buy ? item.BuyCost : item.SellCost;
    }
}