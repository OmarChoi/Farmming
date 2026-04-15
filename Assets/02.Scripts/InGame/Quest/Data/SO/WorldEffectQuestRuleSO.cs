using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectQuestRuleSO", menuName = "Scriptable Objects/Quest/WorldEffectQuestRuleSO")]
public class WorldEffectQuestRuleSO : ScriptableObject
{
    [Header("퀘스트")]
    [SerializeField] private QuestDataSO _quest;

    [Header("기한 (일 단위)")]
    [SerializeField] private int _durationDays = 3;

    [Header("효과")]
    [SerializeField] private WorldEffectDataSO _successEffect;
    [SerializeField] private WorldEffectDataSO _failureEffect;

    public QuestDataSO Quest => _quest;
    public int DurationDays => _durationDays;
    public WorldEffectDataSO SuccessEffect => _successEffect;
    public WorldEffectDataSO FailureEffect => _failureEffect;

    private void OnValidate()
    {
        _durationDays = Mathf.Max(1, _durationDays);
    }
}