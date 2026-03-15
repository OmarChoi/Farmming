using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcMovement : MonoBehaviour
{
    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 destination)
    {
        _agent.SetDestination(destination);
    }

    // 하루가 지났을 때 Npc의 위치를 처음 스케줄 장소로 이동시키기 위한 메서드이다.
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
