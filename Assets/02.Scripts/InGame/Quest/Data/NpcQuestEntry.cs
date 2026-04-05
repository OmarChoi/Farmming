
public class NpcQuestEntry
{
    public QuestDataSO QuestData;
    public QuestRuntimeData RuntimeData;
    public ENpcQuestEntryType EntryType;

    public NpcQuestEntry(QuestDataSO questData, QuestRuntimeData runtimeData, ENpcQuestEntryType entryType)
    {
        QuestData = questData;
        RuntimeData = runtimeData;
        EntryType = entryType;
    }
}
