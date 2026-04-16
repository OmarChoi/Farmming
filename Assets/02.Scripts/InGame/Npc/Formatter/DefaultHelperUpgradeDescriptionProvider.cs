
public class DefaultHelperUpgradeDescriptionProvider : IHelperUpgradeDescriptionProvider
{
    public string GetUpgradeDescription(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (data == null) return string.Empty;

        int currentRange = HelperUpgradeTextFormatter.GetDisplayRangeValue(data, currentGrade);

        if (currentGrade == EHelperGrade.Legendary) return $"작업 범위가 {currentRange}칸입니다.";

        EHelperGrade nextGrade = HelperUpgradeTextFormatter.GetNextGradeValue(currentGrade);
        int nextRange = HelperUpgradeTextFormatter.GetDisplayRangeValue(data, nextGrade);

        return $"작업 범위가 {currentRange}칸에서 {nextRange}칸으로 증가합니다.";
    }
}
