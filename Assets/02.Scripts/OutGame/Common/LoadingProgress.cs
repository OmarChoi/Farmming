public static class LoadingProgress
{
    public static float Value { get; set; }
    public static bool IsActive { get; private set; }

    public static void Begin()
    {
        Value = 0f;
        IsActive = true;
    }

    public static void Complete()
    {
        IsActive = false;
    }

    public static void Reset()
    {
        Value = 0f;
        IsActive = false;
    }
}
