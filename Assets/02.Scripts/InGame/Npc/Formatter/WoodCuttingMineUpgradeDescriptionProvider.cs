
public class WoodCuttingMineUpgradeDescriptionProvider : IHelperUpgradeDescriptionProvider
{
    public string GetUpgradeDescription(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (data == null) return string.Empty;

        int currentTier = HelperUpgradeTextFormatter.GetInternalRangeValue(data, currentGrade);

        if (currentGrade == EHelperGrade.Legendary)
        {
            return $"{currentTier}등급의 나무와 돌을 캘 수 있습니다.";
        }

        EHelperGrade nextGrade = HelperUpgradeTextFormatter.GetNextGradeValue(currentGrade);
        int nextTier = HelperUpgradeTextFormatter.GetInternalRangeValue(data, nextGrade);

        return $"{currentTier}등급의 나무와 돌에서 {nextTier}등급의 나무와 돌을 캘 수 있게 됩니다.";
    }
}
