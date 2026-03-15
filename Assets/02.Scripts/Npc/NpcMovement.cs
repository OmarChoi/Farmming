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
