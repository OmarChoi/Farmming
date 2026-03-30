using System;
using System.Collections.Generic;
using System.Linq;

public class NpcMemoryUpdater
{
    public void Apply(NpcMemoryProfile profile, List<NpcMemoryEntry> newEntries)
    {
        if (profile == null || newEntries == null || newEntries.Count == 0)
        {
            return;
        }

        foreach (var newEntry in newEntries)
        {
            ApplySingle(profile, newEntry);
        }
    }

    private void ApplySingle(NpcMemoryProfile profile, NpcMemoryEntry newEntry)
    {
        NpcMemoryCategoryDefinition definition = NpcMemoryCategoryTable.Get(newEntry.Category);

        switch (definition.Policy)
        {
            case EMemoryUpdatePolicy.Override:
                ApplyOverride(profile, newEntry, definition);
                break;

            case EMemoryUpdatePolicy.Accumulate:
                ApplyAccumulate(profile, newEntry, definition);
                break;

            case EMemoryUpdatePolicy.Refresh:
                ApplyRefresh(profile, newEntry, definition);
                break;

            case EMemoryUpdatePolicy.Decay:
                ApplyDecay(profile, newEntry, definition);
                break;
        }
    }

    private void ApplyOverride(NpcMemoryProfile profile, NpcMemoryEntry newEntry, NpcMemoryCategoryDefinition definition)
    {
        var oldEntry = profile.Entries
            .FirstOrDefault(e => e.Category == newEntry.Category);

        if (ShouldCreateNameChangeEpisode(oldEntry, newEntry))
        {
            var episodeEntry = CreateNameChangedEpisode(oldEntry, newEntry);
            profile.Entries.Add(episodeEntry);

            var episodeDefinition = NpcMemoryCategoryTable.Get(EMemoryCategory.Episode);
            TrimByDefinition(profile, episodeDefinition);
        }

        profile.Entries.RemoveAll(e => e.Category == newEntry.Category);
        profile.Entries.Add(newEntry);

        TrimByDefinition(profile, definition);
    }

    private bool ShouldCreateNameChangeEpisode(NpcMemoryEntry oldEntry, NpcMemoryEntry newEntry)
    {
        if (oldEntry == null || newEntry == null)
        {
            return false;
        }

        if (newEntry.Category != EMemoryCategory.PlayerName)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(oldEntry.Text) || string.IsNullOrWhiteSpace(newEntry.Text))
        {
            return false;
        }

        return oldEntry.Text != newEntry.Text;
    }

    private NpcMemoryEntry CreateNameChangedEpisode(NpcMemoryEntry oldEntry, NpcMemoryEntry newEntry)
    {
        return new NpcMemoryEntry
        {
            MemoryId = Guid.NewGuid().ToString("N"),
            NpcId = newEntry.NpcId,
            PlayerId = newEntry.PlayerId,
            Category = EMemoryCategory.Episode,
            Text = $"플레이어가 이름을 바꿨다. 이전: {oldEntry.Text}, 현재: {newEntry.Text}",
            CreatedAtTicks = newEntry.CreatedAtTicks,
            UpdatedAtTicks = newEntry.UpdatedAtTicks,
            Priority = 40
        };
    }

    private void ApplyAccumulate(NpcMemoryProfile profile, NpcMemoryEntry newEntry, NpcMemoryCategoryDefinition definition)
    {
        bool duplicated = profile.Entries.Any(e => e.Category == newEntry.Category && e.Text == newEntry.Text);

        if (!duplicated)
        {
            profile.Entries.Add(newEntry);
        }

        TrimByDefinition(profile, definition);
    }

    private void ApplyRefresh(NpcMemoryProfile profile, NpcMemoryEntry newEntry, NpcMemoryCategoryDefinition definition)
    {
        if (definition.IsUnique)
        {
            profile.Entries.RemoveAll(e => e.Category == newEntry.Category);
            profile.Entries.Add(newEntry);
            TrimByDefinition(profile, definition);
            return;
        }

        var existing = profile.Entries.FirstOrDefault(e => e.Category == newEntry.Category);

        if (existing != null)
        {
            existing.Text = newEntry.Text;
            existing.UpdatedAtTicks = newEntry.UpdatedAtTicks;
            existing.Priority = newEntry.Priority;
        }
        else
        {
            profile.Entries.Add(newEntry);
        }

        TrimByDefinition(profile, definition);
    }

    private void ApplyDecay(NpcMemoryProfile profile, NpcMemoryEntry newEntry, NpcMemoryCategoryDefinition definition)
    {
        bool duplicated = profile.Entries.Any(e => e.Category == newEntry.Category && e.Text == newEntry.Text);

        if (!duplicated)
        {
            profile.Entries.Add(newEntry);
        }

        TrimByDefinition(profile, definition);
    }

    private void TrimByDefinition(NpcMemoryProfile profile, NpcMemoryCategoryDefinition definition)
    {
        var sameCategoryEntries = profile.Entries
            .Where(e => e.Category == definition.Category)
            .OrderByDescending(e => e.UpdatedAtTicks)
            .ToList();

        if (sameCategoryEntries.Count <= definition.MaxCount)
        {
            return;
        }

        var removeTargets = sameCategoryEntries.Skip(definition.MaxCount).ToList();

        foreach (var target in removeTargets)
        {
            profile.Entries.Remove(target);
        }
    }
}
