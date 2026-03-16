using UnityEngine;

public class Stone : GatheringObject
{
    protected override void Hit()
    {
        // todo. 채광 연출 (바위 흔들림, 파티클, 사운드)
    }
    
    protected override void OnDepleted()
    {
        // todo: 부서지는 연출 (파티클, 사운드 등)
        base.OnDepleted();
    }
}
