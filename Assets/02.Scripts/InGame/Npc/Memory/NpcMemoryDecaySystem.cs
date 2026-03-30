using System;

public class NpcMemoryDecaySystem
{
    private const long DECAY_TIME = TimeSpan.TicksPerMinute * 30; // 30분

    public void ApplyDecay(NpcMemoryProfile profile)
    {
        long now = DateTime.UtcNow.Ticks;

        profile.Entries.RemoveAll(entry =>
        {
            if (entry.Category != EMemoryCategory.Episode) return false;

            return (now - entry.UpdatedAtTicks) > DECAY_TIME;
        });
    }
}
