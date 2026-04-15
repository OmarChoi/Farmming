using UnityEngine;

/// 월드 오브젝트 상호작용 인터페이스 (창고, 제작대 등).
/// NPC 상호작용(INpcInteraction)과 분리하여 애니메이션·흐름을 독립적으로 관리한다.
public interface IWorldInteractable
{
    /// 상호작용 실행. player는 상호작용을 요청한 플레이어.
    void Interact(PlayerController player);

    /// 상호작용 종료 시 호출.
    void EndInteract();

    /// 상호작용 시 재생할 애니메이션 트리거 이름. null이면 애니메이션 없음.
    string AnimationTrigger { get; }
}