using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Coroutine _rotateCoroutine;

    [Header("회전 시간")]
    [SerializeField] private float _rotationDuration = 0.7f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 destination)
    {
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
    public void FaceTarget(Vector3 targetPosition)
    {
        if (_rotateCoroutine != null)
        {
            StopCoroutine(_rotateCoroutine);
        }
        _rotateCoroutine = StartCoroutine(RotateCoroutine(targetPosition, _rotationDuration));
    }

    private IEnumerator RotateCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
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
    }

    public bool HasArrived()
    {
        if (_agent.pathPending)
        {
            return false;
        }

        return _agent.remainingDistance <=
               _agent.stoppingDistance && (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f);
    }
}
