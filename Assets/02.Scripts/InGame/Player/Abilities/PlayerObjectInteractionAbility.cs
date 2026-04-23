using UnityEngine;

/// E키로 IWorldInteractable(창고 등) 월드 오브젝트와 상호작용.
/// 범위 내 IWorldInteractable이 있으면 E키를 선점하여 헬퍼 소환보다 우선 처리.
[DefaultExecutionOrder(-10)] // PlayerHelperInventoryAbility보다 먼저 실행
public class PlayerObjectInteractionAbility : PlayerAbility
{
    [SerializeField] private float _radius = 3f;
    [SerializeField] private LayerMask _interactionLayer;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;

    private PlayerAnimationAbility _animation;

    protected override void Awake()
    {
        base.Awake();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
    }

    private void Update()
    {
        if (!_owner.IsMine) return;
        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_interactKey))
            TryInteract();
    }

    private void TryInteract()
    {
        IWorldInteractable closest = GetClosestInteraction();
        if (closest == null) return;

        // E키 소비 — 같은 프레임에서 헬퍼 소환이 동시에 발동하지 않도록
        if (!_owner.TryConsumeInteract()) return;

        // 애니메이션 (오브젝트별로 다른 트리거 사용 가능)
        if (!string.IsNullOrEmpty(closest.AnimationTrigger))
            _animation?.PlayTrigger(closest.AnimationTrigger);

        closest.Interact(_owner);
    }

    public IWorldInteractable GetClosestInteraction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _interactionLayer);

        IWorldInteractable closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            IWorldInteractable interactable = hit.GetComponentInParent<IWorldInteractable>();
            if (interactable == null) continue;

            // 상호작용 불가능한 상태(예: 건설 중)면 후보에서 제외
            if (!interactable.CanInteract) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = interactable;
            }
        }

        return closest;
    }
}