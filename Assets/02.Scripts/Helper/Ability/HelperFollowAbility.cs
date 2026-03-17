using UnityEngine;

public class HelperFollowAbility : HelperAbility
{
    // TODO : 추후 stat에서 가져오기
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _stopDistance = 1.5f;

    private void Update()
    {
        if (_owner.State != EHelperState.Summoned) return;
        if (_owner.FollowTarget == null) return;

        Vector3 targetPos = _owner.FollowTarget.position;
        Vector3 direction = targetPos - transform.position;
        float distance = direction.magnitude;

        if (distance > _stopDistance)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(moveDir);
        }
    }
}