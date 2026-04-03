public enum ETransitionType
{
    None,
    VillageToDungeon,
    DungeonToVillage
}

public static class SceneTransitionData
{
    public static ETransitionType Type { get; set; } = ETransitionType.None;
    public static int DungeonFloor { get; set; }
    public static int DungeonSeed { get; set; }
    public static bool SeedReady { get; set; }

    public static void Clear()
    {
        Type = ETransitionType.None;
        DungeonFloor = 0;
        DungeonSeed = 0;
        SeedReady = false;
    }
}

public static class SceneTransitionRoomProps
{
    public const string TransitionType = "trType";
    public const string DungeonSeed = "dSeed";
    public const string DungeonFloor = "dFloor";
}
