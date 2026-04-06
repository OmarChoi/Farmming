/// <summary>
/// SFX의 공간 재생 방식을 정의하는 열거형.
/// </summary>
public enum ESpatialMode
{
    Flat2D, // 2D 평면 재생 (UI, 시스템 효과음)
    Positional3D, // 3D 위치 기반 재생 (환경음, 상호작용)
    FollowTransform // 특정 Transform을 추적하며 재생 (캐릭터 발소리 등)
}
