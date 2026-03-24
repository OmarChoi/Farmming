using UnityEngine;

public class NpcRuntimeIdentity : MonoBehaviour
{
    [SerializeField] private string _npcId;

    private NpcController _controller;

    public string NpcId => _npcId;

    public void Initialize(string npcId, NpcController controller)
    {
        _npcId = npcId;
        _controller = controller;
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(_npcId) && _controller != null && NpcRegistry.Instance != null)
        {
            NpcRegistry.Instance.Register(_npcId, _controller);
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(_npcId) && _controller != null && NpcRegistry.Instance != null)
        {
            NpcRegistry.Instance.Unregister(_npcId, _controller);
        }
    }
}
