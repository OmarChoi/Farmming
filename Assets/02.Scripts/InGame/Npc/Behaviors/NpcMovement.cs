using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private IMovementAnimator _anim;

    private Coroutine _rotateCoroutine;
    private bool _isOwner;

    [Header("이동 옵션")]
    [SerializeField] private float _walkDistance = 18f;
    private float _walkSpeed = 2f;
    private float _runSpeed = 4f;

    [Header("회전 옵션")]
    [SerializeField] private float _rotationSpeed = 240f;  // 초당 돌 각도입니다. (360f = 초당 360도)
    [SerializeField] private float _turnMoveSpeed = 0.5f;
    private const float MinAngle = 0.5f;

    [Header("점프 옵션")]
    private float _jumpDuration = 0.8f;
    private float _jumpHeight = 1.6f;
    private const float JumpCurveScale = 4f;  // t * (1-t)의 최대값(0.25)을 1로 정규화하기 위한 값입니다.
    private bool _isJumping;

    public float MoveSpeed => _agent != null && _agent.enabled ? Mathf.Clamp01(_agent.desiredVelocity.magnitude / _runSpeed) : 0f;
    public bool IsOnNavMesh => _agent != null && _agent.isOnNavMesh;
    public bool IsJumping => _isJumping;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _walkSpeed;
    }

    public void SetOwner(bool isOwner)
    {
        _isOwner = isOwner;

        if (_agent == null) return;
        if (!_isOwner)
        {
            _agent.enabled = false;
        }
    }

    public void Initialize(IMovementAnimator anim, float walkSpeed, float runSpeed, float jumpDuration, float jumpHeight)
    {
        _anim = anim;
        _walkSpeed = walkSpeed;
        _runSpeed = runSpeed;
        _jumpDuration = jumpDuration;
        _jumpHeight = jumpHeight;

        if (_agent != null)
        {
            _agent.speed = _walkSpeed;
        }
    }

    private void Update()
    {
        if (!_isOwner || _agent == null || !_agent.enabled) return;

        if (_isJumping)
        {
            _anim?.SetMove(1f);
            return;
        }

        _anim?.SetMove(MoveSpeed);

        if (_agent.isOnOffMeshLink)
        {
            // Npc가 삭제될 때 점프 도중이여도 안전하게 작업을 마무리하기 위해 CancellationToken을 사용했습니다.
            HandleOffMeshLink(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    public void MoveTo(Vector3 destination, float stoppingDistance, bool forceRun)
    {
        if (_isJumping || _agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        if (forceRun)
        {
            _agent.speed = _runSpeed;
        }
        else
        {
            float distance = Vector3.Distance(transform.position, destination);
            _agent.speed = distance <= _walkDistance ? _walkSpeed : _runSpeed;
        }

        _agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    public void MoveTo(Vector3 destination, float stoppingDistance = 0f)
    {
        MoveTo(destination, stoppingDistance, false);
    }

    public void Stop()
    {
        if (_isJumping || _agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void Resume()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
    }

    // Npc가 특정 위치를 바라보도록 합니다.
    public UniTask FaceTargetAsync(Vector3 targetPosition)
    {
        if (_rotateCoroutine != null)
        {
            StopCoroutine(_rotateCoroutine);
        }

        var tcs = new UniTaskCompletionSource();
        _rotateCoroutine = StartCoroutine(RotateCoroutine(targetPosition, tcs));
        return tcs.Task;
    }

    private IEnumerator RotateCoroutine(Vector3 targetPosition, UniTaskCompletionSource tcs)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            tcs.TrySetResult();
            yield break;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (true)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

            _anim?.SetMove(_turnMoveSpeed);

            // 거의 다 돌았으면 종료합니다.
            if (Quaternion.Angle(transform.rotation, targetRotation) < MinAngle) break;
            yield return null;
        }
        transform.rotation = targetRotation;

        // 완료 신호를 보내줍니다.
        tcs.TrySetResult();
    }

    // 하루가 지났을 때 Npc의 위치를 처음 스케줄 장소로 이동시킵니다.
    public void TeleportTo(Vector3 position)
    {
        if (!_agent.isOnNavMesh)
        {
            transform.position = position;
            return;
        }

        _agent.ResetPath();
        _agent.Warp(position);
        _agent.velocity = Vector3.zero;
    }

    public bool HasArrived()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return false;
        if (_agent.pathPending) return false;

        return _agent.remainingDistance <= _agent.stoppingDistance
               && (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f);
    }

    // NavMeshLink를 만나면 포물선 모양으로 점프합니다.
    private async UniTask HandleOffMeshLink(CancellationToken cancellationToken)
    {
        if (_isJumping || _agent == null || !_agent.enabled || !_agent.isOnOffMeshLink) return;
        _isJumping = true;

        var linkData = _agent.currentOffMeshLinkData;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = linkData.endPos + Vector3.up * _agent.baseOffset;

        Vector3 direction = endPosition - startPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        float elapsed = 0f;

        _agent.isStopped = true;
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        try
        {
            while (elapsed < _jumpDuration)
            {
                // 이미 행동이 취소되었다면 예외 처리를 합니다.
                cancellationToken.ThrowIfCancellationRequested();

                float t = elapsed / _jumpDuration;

                // 부드러운 움직임을 위해 기본 위치를 보간합니다.
                Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

                // 포물선의 높이를 추가합니다.
                position.y += _jumpHeight * JumpCurveScale * (t * (1f - t));

                transform.position = position;

                elapsed += Time.deltaTime;

                // 토큰을 전달하여 취소 시 반응하도록 합니다.
                await UniTask.Yield(cancellationToken);
            }
            transform.position = endPosition;
            _agent.nextPosition = endPosition;
            _agent.CompleteOffMeshLink();
        }
        finally
        {
            _agent.updatePosition = true;
            _agent.updateRotation = true;
            _agent.isStopped = false;
            _isJumping = false;
        }
    }
}
