using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class UI_WorldEffectInfo : UI_InfoSectionBase
{
    private const string EmptyLabel = "없음";
    private const string SeparatorLabel = ", ";

    [Header("Buff Infos")]
    [SerializeField] private TextMeshProUGUI _buffText;
    [SerializeField] private TextMeshProUGUI _debuffText;

    private readonly List<string> _buffLabels = new List<string>();
    private readonly List<string> _debuffLabels = new List<string>();
    private readonly StringBuilder _labelBuilder = new StringBuilder(32);

    public override void Subscribe(PlayerController playerController)
    {
        WorldEffectEvents.OnEffectsChanged += HandleEffectsChanged;
    }
    public override void Unsubscribe()
    {
        WorldEffectEvents.OnEffectsChanged -= HandleEffectsChanged;
    }

    public override void Refresh()
    {
        WorldEffectManager manager = WorldEffectManager.Instance;

        _buffLabels.Clear();
        _debuffLabels.Clear();

        if (manager == null || manager.Database == null)
        {
            ApplyLabels();
            return;
        }

        IReadOnlyList<WorldEffectEntry> active = manager.ActiveEffects;
        foreach (WorldEffectEntry entry in active)
        {
            if (entry == null) continue;

            WorldEffectDataSO data = manager.Database.GetById(entry.EffectId);
            if (data == null) continue;

            string label = BuildEffectLabel(data.DisplayName, entry.RemainingDays);
            if ((EWorldEffectKind)entry.Kind == EWorldEffectKind.Buff)
            {
                _buffLabels.Add(label);
            }
            else
            {
                _debuffLabels.Add(label);
            }
        }

        ApplyLabels();
    }

    private void HandleEffectsChanged()
    {
        Refresh();
    }

    private void ApplyLabels()
    {
        _buffText?.SetText(_buffLabels.Count > 0 ? string.Join(SeparatorLabel, _buffLabels) : EmptyLabel);
        _debuffText?.SetText(_debuffLabels.Count > 0 ? string.Join(SeparatorLabel, _debuffLabels) : EmptyLabel);
    }

    private string BuildEffectLabel(string displayName, int remainingDays)
    {
        _labelBuilder.Clear();
        _labelBuilder.Append(string.IsNullOrEmpty(displayName) ? "?" : displayName);
        if (remainingDays <= 0) return _labelBuilder.ToString();
        
        _labelBuilder.Append(" (");
        _labelBuilder.Append(remainingDays);
        _labelBuilder.Append("일)");
        return _labelBuilder.ToString();
    }
}
