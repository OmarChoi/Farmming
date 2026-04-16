
public class GroundUpgradeDescriptionProvider : IHelperUpgradeDescriptionProvider
{
    public string GetUpgradeDescription(HelperDataSO data, EHelperGrade currentGrade)
    {
        if (data == null) return string.Empty;

        return $"땅을 1칸씩 파거나 놓을 수 있어요.";
    }
}
