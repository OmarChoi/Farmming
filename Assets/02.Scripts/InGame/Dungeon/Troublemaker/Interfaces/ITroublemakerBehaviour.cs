using UnityEngine;

public interface ITroublemakerBehaviour
{
    void Initialize(TroublemakerController controller);

    void Tick(float deltaTime);

    void OnIdle();
    void OnChase(Transform target);
    void OnReachTarget(Transform target);
    void OnReturnToHome();
}
