using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private NpcAnimatorController _anim;

    public float MoveSpeed => Mathf.Clamp01(_agent.desiredVelocity.magnitude / _originalSpeed);

    private float _originalSpeed;
    private const float HalfSpeedMultiplier = 0.5f;
    private const float MinAngle = 0.5f;

    private Coroutine _rotateCoroutine;

    [Header("이동 옵션")]
    [SerializeField] private float _walkDistance = 18f;

    [Header("회전 옵션")]
    [SerializeField] private float _rotationSpeed = 240f;  // 초당 돌 각도입니다. (360f = 초당 360도)
    [SerializeField] private float _turnMoveSpeed = 0.5f;

    [Header("점프 옵션")]
    [SerializeField] private float _jumpDuration = 1.2f;
    [SerializeField] private float _jumpHeight = 1.6f;
    private const float JumpCurveScale = 4f;  // t * (1-t)의 최대값(0.25)을 1로 정규화하기 위한 값입니다.
    private bool _isJumping;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<NpcAnimatorController>();

        _originalSpeed = _agent.speed;
    }

    private void Update()
    {
        _anim?.SetMove(MoveSpeed);

        if (_agent.isOnOffMeshLink && !_isJumping)
        {
            // 컴포넌트 파괴 시 비동기 작업이 안전하게 취소되도록 CancellationToken을 사용합니다.
            HandleOffMeshLink(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    // NavMeshLink를 만나면 포물선 모양으로 점프합니다.
    private async UniTask HandleOffMeshLink(CancellationToken cancellationToken)
    {
        _isJumping = true;

        var linkData = _agent.currentOffMeshLinkData;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = linkData.endPos + Vector3.up * _agent.baseOffset;

        float time = 0f;

        _agent.isStopped = true;

        while (time < _jumpDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            float t = time / _jumpDuration;

            // 부드러운 움직임을 위해 기본 위치를 보간합니다.
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

            // 포물선의 높이를 추가합니다.
            position.y += _jumpHeight * JumpCurveScale * (t * (1f - t));

            transform.position = position;

            time += Time.deltaTime;

            // 토큰을 전달하여 취소 시 반응하도록 합니다.
            await UniTask.Yield(cancellationToken);
        }
        transform.position = endPosition;

        _agent.CompleteOffMeshLink();
        _agent.isStopped = false;
        _isJumping = false;
    }

    public void Initialize(NpcAnimatorController anim)
    {
        _anim = anim;
    }

    public void MoveTo(Vector3 destination)
    {
        float distance = Vector3.Distance(transform.position, destination);

        if (distance <= _walkDistance)
        {
            _agent.speed = _originalSpeed * HalfSpeedMultiplier;
        }
        else
        {
            _agent.speed = _originalSpeed;
        }

        _agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void Resume()
    {
        if (!_agent.isOnNavMesh) return;

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
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );

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
        if (_agent.pathPending)
        {
            return false;
        }

        return _agent.remainingDistance <= _agent.stoppingDistance
               && (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
    }
}
