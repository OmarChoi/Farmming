using UnityEngine;

[CreateAssetMenu(fileName = "CameraPreset", menuName = "Scriptable Objects/CameraPreset")]
public class CameraPreset : ScriptableObject
{
    [Header("위치")]
    public Vector3 PositionOffset = new Vector3(0f, 1.5f, -3f);

    [Header("회전")]
    public Vector3 RotationOffset = Vector3.zero;

    [Header("전환")]
    public float TransitionSpeed = 5f;
}