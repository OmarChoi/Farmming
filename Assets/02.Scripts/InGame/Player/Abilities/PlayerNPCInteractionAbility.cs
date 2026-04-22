using UnityEngine;

/// E키로 NPC와 상호작용.
/// 범위 내 INpcInteraction이 있으면 E키를 선점하여 헬퍼 소환보다 우선 처리.
[DefaultExecutionOrder(-5)] // PlayerHelperInventoryAbility보다 먼저 실행, PlayerObjectInteractionAbility보다 나중 실행
public class PlayerNPCInteractionAbility : PlayerAbility
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _interactionLayer;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private CameraPreset _cameraPreset;

    private PlayerCameraAbility _cameraAbility;
    private PlayerAnimationAbility _animation;
    private Transform _faceTarget;

    private NpcInteractionComponent _currentNpcInteraction;

    protected override void Awake()
    {
        base.Awake();
        _cameraAbility = _owner.GetAbility<PlayerCameraAbility>();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
    }

    private void Update()
    {
        if (!_owner.IsMine) return;

        if (_faceTarget != null)
        {
            RotateTowardTarget();
            return;
        }

        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        IInteraction closest = GetClosestInteraction();
        if (closest == null) return;

        if (!_owner.TryConsumeInteract()) return;

        _faceTarget = (closest as Component).transform;
        _owner.LockAction();
        _animation?.PlayGreet();
        _cameraAbility?.SetPreset(_cameraPreset, _faceTarget);

        _currentNpcInteraction = closest as NpcInteractionComponent;
        closest.RequestInteract(_owner);
    }

    public IInteraction GetClosestInteraction()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _interactionLayer);

        IInteraction closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            IInteraction interactable = hit.GetComponentInParent<IInteraction>();
            if (interactable == null) continue;

            if (interactable is NpcInteractionComponent npcInteraction)
            {
                if (!npcInteraction.CanShowPrompt()) continue;
            }

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = interactable;
            }
        }

        return closest;
    }

    public void EndInteraction()
    {
        _currentNpcInteraction = null;
        _faceTarget = null;
        _cameraAbility?.ClearPreset();
        _owner.UnlockAction();
    }

    public void ForceCancelCurrentInteraction()
    {
        if (!_owner.IsMine) return;

        NpcInteractionComponent active = _currentNpcInteraction;
        if (active != null)
        {
            active.ForceEndInteraction();
            return;
        }

        // 만약에 참조가 끊겼는데 상태만 남은 경우를 대비합니다.
        _faceTarget = null;
        _cameraAbility?.ClearPreset();
        _owner.UnlockAction();

        if (NpcDialogueController.Instance != null)
        {
            NpcDialogueController.Instance.ForceCloseInteraction();
        }
    }

    private void RotateTowardTarget()
    {
        Vector3 dir = _faceTarget.position - _owner.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        _owner.transform.rotation = Quaternion.Slerp(
        _owner.transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    public void BeginAutoInteraction(IInteraction target)
    {
        if (target == null) return;

        Component targetComponent = target as Component;
        if (targetComponent == null) return;

        _faceTarget = targetComponent.transform;
        _owner.LockAction();
        _cameraAbility?.SetPreset(_cameraPreset, _faceTarget);

        _currentNpcInteraction = target as NpcInteractionComponent;
        target.RequestInteract(_owner);
    }
}
