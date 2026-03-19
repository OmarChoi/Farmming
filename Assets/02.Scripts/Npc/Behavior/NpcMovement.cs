using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private NpcAnimatorController _anim;

    public float MoveSpeed => Mathf.Clamp01(_agent.desiredVelocity.magnitude / _originalSpeed);

    private float _originalSpeed;
    private const float _halfSpeedMultiplier = 0.5f;
    private const float _minAngle = 0.5f;

    private Coroutine _rotateCoroutine;

    [Header("이동 옵션")]
    [SerializeField] private float _walkDistance = 18f;

    [Header("회전 옵션")]
    [SerializeField] private float _rotationSpeed = 240f;  // 초당 돌 각도입니다. (360f = 초당 360도)
    [SerializeField] private float _turnMoveSpeed = 0.5f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<NpcAnimatorController>();

        _originalSpeed = _agent.speed;
    }

    private void Update()
    {
        _anim?.SetMove(MoveSpeed);
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
            _agent.speed = _originalSpeed * _halfSpeedMultiplier;
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

    // Npc가 특정 위치를 바라보도록 하는 메서드입니다.
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
            if (Quaternion.Angle(transform.rotation, targetRotation) < _minAngle) break;

            yield return null;
        }

        transform.rotation = targetRotation;

        // 완료 신호를 보내줍니다.
        tcs.TrySetResult();
    }

    // 하루가 지났을 때 Npc의 위치를 처음 스케줄 장소로 이동시키기 위한 메서드입니다.
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
