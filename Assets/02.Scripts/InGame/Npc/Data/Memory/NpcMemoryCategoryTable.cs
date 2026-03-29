using System.Collections.Generic;

public static class NpcMemoryCategoryTable
{
    public static readonly Dictionary<EMemoryCategory, NpcMemoryCategoryDefinition> Table
        = new()
        {
            {
                EMemoryCategory.PlayerName,
                new NpcMemoryCategoryDefinition
                {
                    Category = EMemoryCategory.PlayerName,
                    Policy = EMemoryUpdatePolicy.Override,
                    IsUnique = true,
                    MaxCount = 1
                }
            },
            {
                EMemoryCategory.PlayerPreference,
                new NpcMemoryCategoryDefinition
                {
                    Category = EMemoryCategory.PlayerPreference,
                    Policy = EMemoryUpdatePolicy.Accumulate,
                    IsUnique = false,
                    MaxCount = 10
                }
            },
            {
                EMemoryCategory.Impression,
                new NpcMemoryCategoryDefinition
                {
                    Category = EMemoryCategory.Impression,
                    Policy = EMemoryUpdatePolicy.Refresh,
                    IsUnique = true,
                    MaxCount = 1
                }
            },
            {
                EMemoryCategory.Episode,
                new NpcMemoryCategoryDefinition
                {
                    Category = EMemoryCategory.Episode,
                    Policy = EMemoryUpdatePolicy.Decay,
                    IsUnique = false,
                    MaxCount = 10
                }
            },
            {
                EMemoryCategory.Promise,
                new NpcMemoryCategoryDefinition
                {
                    Category = EMemoryCategory.Promise,
                    Policy = EMemoryUpdatePolicy.Accumulate,
                    IsUnique = false,
                    MaxCount = 5
                }
            }
        };

    public static NpcMemoryCategoryDefinition Get(EMemoryCategory category)
    {
        if (Table.TryGetValue(category, out var definition))
        {
            return definition;
        }

        return new NpcMemoryCategoryDefinition
        {
            Category = category,
            Policy = EMemoryUpdatePolicy.Accumulate,
            IsUnique = false,
            MaxCount = 10
        };
    }
}
