using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectQuestConfigSO", menuName = "Scriptable Objects/Quest/WorldEffectQuestConfigSO")]
public class WorldEffectQuestConfigSO : ScriptableObject
{
    [Header("주기 (일 단위)")]
    [SerializeField] private int _firstTriggerDay = 1;
    [SerializeField] private int _questIntervalDays = 7;

    [Header("효과 지속 (일 단위)")]
    [SerializeField] private int _effectDurationDays = 7;

    [Header("완료 건물")]
    [SerializeField] private string _completionBuildingId = "Shrine";

    [Header("Rule 풀")]
    [SerializeField] private List<WorldEffectQuestRuleSO> _rules = new();

    public int FirstTriggerDay => _firstTriggerDay;
    public int QuestIntervalDays => _questIntervalDays;
    public int EffectDurationDays => _effectDurationDays;
    public string CompletionBuildingId => _completionBuildingId;
    public IReadOnlyList<WorldEffectQuestRuleSO> Rules => _rules;

    private void OnValidate()
    {
        _firstTriggerDay = Mathf.Max(1, _firstTriggerDay);
        _questIntervalDays = Mathf.Max(2, _questIntervalDays);
        _effectDurationDays = Mathf.Max(1, _effectDurationDays);

        if (_rules == null) return;
        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (rule == null) continue;
            if (rule.DurationDays >= _questIntervalDays)
            {
                Debug.LogWarning($"[WorldEffectQuestConfig] Rule '{rule.name}' DurationDays({rule.DurationDays}) >= QuestIntervalDays({_questIntervalDays}). 겹침 가능.", this);
            }
        }
    }
}