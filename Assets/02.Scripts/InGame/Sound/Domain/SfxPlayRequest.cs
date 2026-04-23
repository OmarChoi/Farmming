using UnityEngine;

/// <summary>
/// SFX 재생 요청을 캡슐화하는 값 객체.
/// 클립 키, 공간 모드, 위치/추적 대상 정보를 포함한다.
/// </summary>
public readonly struct SfxPlayRequest
{
    public string ClipKey { get; }
    public ESpatialMode ESpatialMode { get; }
    public Vector3 Position { get; }
    public Transform FollowTarget { get; }
    public float Volume { get; }
    public float Pitch { get; }

    public SfxPlayRequest(string clipKey, ESpatialMode spatialMode = ESpatialMode.Flat2D, Vector3 position = default, Transform followTarget = null, float volume = 1f, float pitch = 1f)
    {
        ClipKey = clipKey;
        ESpatialMode = spatialMode;
        Position = position;
        FollowTarget = followTarget;
        Volume = volume;
        Pitch = pitch;
    }
}
