using UnityEngine;

public class NpcRuntimeIdentity : MonoBehaviour
{
    [SerializeField] private string _runtimeNpcKey;

    private NpcController _controller;

    public string RuntimeNpcKey => _runtimeNpcKey;

    public void Initialize(string runtimeNpcKey, NpcController controller)
    {
        if (!string.IsNullOrEmpty(_runtimeNpcKey) && _controller != null && NpcRegistry.Instance != null)
        {
            NpcRegistry.Instance.Unregister(_runtimeNpcKey, _controller);
        }

        _runtimeNpcKey = runtimeNpcKey;
        _controller = controller;

        if (!string.IsNullOrEmpty(_runtimeNpcKey) && _controller != null && NpcRegistry.Instance != null)
        {
            NpcRegistry.Instance.Register(_runtimeNpcKey, _controller);
        }
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(_runtimeNpcKey) && _controller != null && NpcRegistry.Instance != null)
        {
            NpcRegistry.Instance.Register(_runtimeNpcKey, _controller);
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(_runtimeNpcKey) && _controller != null && NpcRegistry.Instance != null)
        {
            NpcRegistry.Instance.Unregister(_runtimeNpcKey, _controller);
        }
    }
}
