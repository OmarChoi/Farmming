
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

    public static string GetRangeText(HelperDataSO data, EHelperGrade currentGrade, bool canUpgrade)
    {
        if (data == null) return string.Empty;

        int currentRange = GetRange(data, currentGrade);

        if (currentGrade == EHelperGrade.Legendary)
        {
            return $"작업 범위: {currentRange} (MAX)";
        }

        if (!canUpgrade)
        {
            return $"작업 범위: {currentRange}";
        }

        EHelperGrade nextGrade = GetNextGrade(currentGrade);
        int nextRange = GetRange(data, nextGrade);

        return $"작업 범위: {currentRange} -> {nextRange}";
    }

    public static string GetUpgradeMessage(bool canUpgrade, string blockReason)
    {
        return canUpgrade
            ? "업그레이드가 가능합니다!"
            : blockReason;
    }

    public static string GetBlockReasonText(EHelperUpgradeBlockReason reason)
    {
        switch (reason)
        {
            case EHelperUpgradeBlockReason.InvalidData:
                return "곡룡 정보가 없습니다.";

            case EHelperUpgradeBlockReason.InventoryNotReady:
                return "곡룡 인벤토리 정보를 확인할 수 없습니다.";

            case EHelperUpgradeBlockReason.MaxGrade:
                return "곡룡이 이미 최고 등급입니다.";

            case EHelperUpgradeBlockReason.NotEnoughExperience:
                return "곡룡 경험치가 부족합니다.";

            case EHelperUpgradeBlockReason.None:
            default:
                return string.Empty;
        }
    }
}
