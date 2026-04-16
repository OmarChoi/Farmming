using UnityEngine;

public class HelperUpgradeTextFormatter
{
    private static EHelperGrade GetNextGrade(EHelperGrade grade)
    {
        switch (grade)
        {
            case EHelperGrade.Normal:
                return EHelperGrade.Epic;
            case EHelperGrade.Epic:
                return EHelperGrade.Legendary;
            default:
                return grade; // Legendary는 그대로 둔다.
        }
    }

    private static int GetRange(HelperDataSO data, EHelperGrade grade)
    {
        return grade switch
        {
            EHelperGrade.Normal => data.NormalRange,
            EHelperGrade.Epic => data.EpicRange,
            EHelperGrade.Legendary => data.LegendaryRange,
            _ => data.NormalRange
        };
    }

    private static int GetDisplayRange(HelperDataSO data, EHelperGrade grade)
    {
        int internalRange = GetRange(data, grade);
        return internalRange <= 0 ? 0 : internalRange * 2 - 1;
    }

    public static Sprite GetHelperIcon(HelperDataSO data)
    {
        return data == null ? null : data.HelperIcon;
    }

    public static string GetHelperName(HelperDataSO data)
    {
        if (data == null) return string.Empty;

        return string.IsNullOrEmpty(data.HelperName)
            ? data.HelperId
            : data.HelperName;
    }

    public static string GetGradeText(EHelperGrade grade)
    {
        return $"현재 등급: {grade}";
    }

    public static string GetExpText(EHelperGrade grade, int exp, int maxExp)
    {
        if (grade == EHelperGrade.Legendary) return "현재 경험치: MAX";

        return $"현재 경험치: {exp} / {maxExp}";
    }

    public static string GetRangeText(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (data == null) return string.Empty;

        switch (data.HelperType)
        {
            case EHelperType.WoodCuttingMine:
                return GetWoodCuttingMineText(data, currentGrade);

            case EHelperType.Water:
                return GetWaterRangeText(data, currentGrade);

            default:
                return GetDefaultRangeText(data, currentGrade);
        }
    }

    private static string GetDefaultRangeText(HelperDataSO data, EHelperGrade currentGrade)
    {
        int currentRange = GetDisplayRange(data, currentGrade);

        if (currentGrade == EHelperGrade.Legendary)
        {
            return $"작업 범위가 {currentRange}칸이에요.";
        }

        EHelperGrade nextGrade = GetNextGrade(currentGrade);
        int nextRange = GetDisplayRange(data, nextGrade);

        return $"작업 범위가 {currentRange}칸에서 {nextRange}칸으로 늘어나요!";
    }

    private static string GetWoodCuttingMineText(HelperDataSO data, EHelperGrade currentGrade)
    {
        int currentTier = GetRange(data, currentGrade);

        if (currentGrade == EHelperGrade.Legendary)
        {
            return $"{currentTier}등급의 나무와 돌을 캘 수 있어요.";
        }

        EHelperGrade nextGrade = GetNextGrade(currentGrade);
        int nextTier = GetRange(data, nextGrade);

        return $"{nextTier}등급의 나무와 돌까지 캘 수 있게 돼요!";
    }

    private static string GetWaterRangeText(HelperDataSO data, EHelperGrade currentGrade)
    {
        int currentRange = GetDisplayRange(data, currentGrade);

        if (currentGrade == EHelperGrade.Legendary)
        {
            return $"작업 범위가 {currentRange}칸이에요.\n그리고 냉기로 용암을 얼릴 수 있어요!";
        }

        EHelperGrade nextGrade = GetNextGrade(currentGrade);
        int nextRange = GetDisplayRange(data, nextGrade);

        if (currentGrade == EHelperGrade.Epic && nextGrade == EHelperGrade.Legendary)
        {
            return $"작업 범위가 {currentRange}칸에서 {nextRange}칸으로 늘어나요!\n그리고 냉기로 용암을 얼릴 수 있게 돼요!";
        }

        return $"작업 범위가 {currentRange}칸에서 {nextRange}칸으로 늘어나요!";
    }

    public static string GetUpgradeMessage(bool canUpgrade, string blockReason)
    {
        return canUpgrade
            ? "업그레이드가 가능해요!!!"
            : blockReason;
    }

    public static string GetBlockReasonText(EHelperUpgradeBlockReason reason)
    {
        switch (reason)
        {
            case EHelperUpgradeBlockReason.InvalidData:
                return "곡룡 친구가 안 보여요.";

            case EHelperUpgradeBlockReason.InventoryNotReady:
                return "곡룡 인벤토리 정보를 확인할 수 없어요.";

            case EHelperUpgradeBlockReason.MaxGrade:
                return "곡룡이 이미 최고 등급이에요.";

            case EHelperUpgradeBlockReason.NotEnoughExperience:
                return "곡룡 경험치가 부족해요.";

            case EHelperUpgradeBlockReason.NotEnoughItemCost:
                return "업그레이드에 필요한 아이템이 부족해요.";

            case EHelperUpgradeBlockReason.NotEnoughGoldCost:
                return "업그레이드에 필요한 골드가 부족해요.";

            case EHelperUpgradeBlockReason.None:
            default:
                return string.Empty;
        }
    }
}
