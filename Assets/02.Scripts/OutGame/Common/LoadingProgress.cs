using Photon.Pun;

public static class LoadingProgress
{
    public static float Value { get; set; }
    public static bool IsActive { get; private set; }

    private static bool _trackingSceneLoad;
    private static float _sceneLoadBase;
    private static float _sceneLoadRange;

    public static void Begin()
    {
        Value = 0f;
        IsActive = true;
        _trackingSceneLoad = false;
    }

    public static void Complete()
    {
        _trackingSceneLoad = false;
        IsActive = false;
    }

    public static void Reset()
    {
        Value = 0f;
        IsActive = false;
        _trackingSceneLoad = false;
    }

    /// <summary>
    /// 씬 로딩 진행률 추적 시작.
    /// base ~ base+range 범위를 PhotonNetwork.LevelLoadingProgress로 채움.
    /// </summary>
    public static void TrackSceneLoad(float baseValue, float range)
    {
        _sceneLoadBase = baseValue;
        _sceneLoadRange = range;
        _trackingSceneLoad = true;
    }

    /// <summary>
    /// UI의 Update에서 매 프레임 호출하여 씬 로딩 진행률을 반영.
    /// </summary>
    public static void Tick()
    {
        if (!_trackingSceneLoad || !PhotonNetwork.IsConnected) return;

        float sceneProgress = PhotonNetwork.LevelLoadingProgress;
        float mapped = _sceneLoadBase + _sceneLoadRange * sceneProgress;

        if (mapped > Value)
            Value = mapped;

        if (sceneProgress >= 1f)
            _trackingSceneLoad = false;
    }
}