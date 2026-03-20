using UnityEngine;

public abstract class HelperAbility : MonoBehaviour
{
    protected HelperController _owner { get; private set; }

    protected virtual void Awake()
    {
        _owner = GetComponentInParent<HelperController>();
    }
}