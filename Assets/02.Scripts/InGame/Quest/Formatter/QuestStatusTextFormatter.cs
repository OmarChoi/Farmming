
public class QuestStatusTextFormatter
{
    public static string BuildStatusText(QuestRuntimeData runtimeData)
    {
        if (runtimeData == null) return string.Empty;

        switch (runtimeData.Status)
        {
            case EQuestStatus.InProgress:
                return "상태: 진행 중";

            case EQuestStatus.CanComplete:
                return "상태: 완료 가능";

            case EQuestStatus.Completed:
                return "상태: 완료";

            default:
                return string.Empty;
        }
    }
}
