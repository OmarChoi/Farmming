using UnityEngine;

public class TroublemakerBehaviourBase : MonoBehaviour, ITroublemakerBehaviour
{
    protected TroublemakerController Controller { get; private set; }

    public virtual void Initialize(TroublemakerController controller)
    {
        Controller = controller;
    }

    public virtual void Tick(float deltaTime) { }

    public virtual void OnIdle() { }
    public virtual void OnChase(Transform target) { }
    public virtual void OnReachTarget(Transform target) { }
    public virtual void OnReturnToHome() { }
}
