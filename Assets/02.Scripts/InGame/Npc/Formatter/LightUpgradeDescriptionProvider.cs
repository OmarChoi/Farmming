
public class LightUpgradeDescriptionProvider : IHelperUpgradeDescriptionProvider
{
    public string GetUpgradeDescription(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (data == null) return string.Empty;

        return $"어두운 곳에서도 밝게 빛나요.";
    }
}
