using UnityEngine;

public readonly struct NpcSpawnRequest
{
    public readonly NpcDataSO Data;
    public readonly Vector3 RequestedPosition;
    public readonly Quaternion Rotation;
    public readonly Transform Parent;
    public readonly bool ForceRespawn;
    public readonly string Reason;  // 스폰 사유 간단히 적는 용도입니다.
    public bool IsLocalOnly { get; }
    public string RuntimeNpcKey { get; }

    public string NpcId => Data?.NpcId;

    public GameObject Prefab => Data?.Prefab;
    public bool HasValidData => Data != null;

    public NpcSpawnRequest(
        NpcDataSO data,
        Vector3 requestedPosition,
        Quaternion rotation,
        Transform parent = null,
        bool forceRespawn = false,
        string reason = null,
        bool isLocalOnly = false,
        string runtimeNpcKey = null)
    {
        Data = data;
        RequestedPosition = requestedPosition;
        Rotation = rotation;
        Parent = parent;
        ForceRespawn = forceRespawn;
        Reason = reason;
        IsLocalOnly = isLocalOnly;
        RuntimeNpcKey = runtimeNpcKey;
    }
}
