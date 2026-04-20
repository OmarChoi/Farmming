using UnityEngine;
using System.Collections.Generic;

public class HelperUpgradeTextFormatter
{
    private static readonly Dictionary<EHelperType, IHelperUpgradeDescriptionProvider> _descriptionProviders
        = new()
        {
            { EHelperType.WoodCuttingMine, new WoodCuttingMineUpgradeDescriptionProvider() },
            { EHelperType.Water, new WaterUpgradeDescriptionProvider() },
            { EHelperType.Ground, new GroundUpgradeDescriptionProvider() },
            { EHelperType.Light, new LightUpgradeDescriptionProvider() }
        };

    private static readonly IHelperUpgradeDescriptionProvider _defaultProvider
        = new DefaultHelperUpgradeDescriptionProvider();

    public static string GetRangeText(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (data == null) return string.Empty;

        if (_descriptionProviders.TryGetValue(data.HelperType, out var provider))
        {
            return provider.GetUpgradeDescription(data, currentGrade);
        }

        return _defaultProvider.GetUpgradeDescription(data, currentGrade);
    }

    public static EHelperGrade GetNextGradeValue(EHelperGrade grade)
    {
        return grade switch
        {
            EHelperGrade.Normal => EHelperGrade.Epic,
            EHelperGrade.Epic => EHelperGrade.Legendary,
            _ => grade
        };
    }

    public static int GetInternalRangeValue(HelperDataSO data, EHelperGrade grade)
    {
        if (data == null) return 0;

        return grade switch
        {
            EHelperGrade.Normal => data.NormalRange,
            EHelperGrade.Epic => data.EpicRange,
            EHelperGrade.Legendary => data.LegendaryRange,
            _ => data.NormalRange
        };
    }

    public static int GetDisplayRangeValue(HelperDataSO data, EHelperGrade grade)
    {
        int internalRange = GetInternalRangeValue(data, grade);
        return internalRange <= 0 ? 0 : internalRange * 2 - 1;
    }

    public static Sprite GetHelperIcon(HelperDataSO data)
    {
        return GetHelperIcon(data, EHelperGrade.Normal);
    }

    public static Sprite GetHelperIcon(HelperDataSO data, EHelperGrade grade)
    {
        return data == null ? null : data.GetIconForGrade(grade);
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

            case EHelperUpgradeBlockReason.IsCantUpgrade:
                return "이 곡룡은 업그레이드할 수 없어요.";

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
