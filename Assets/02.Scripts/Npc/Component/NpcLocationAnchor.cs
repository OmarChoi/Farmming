using UnityEngine;

public class NpcLocationAnchor : MonoBehaviour
{
    [SerializeField] private NpcData _npcData;

    [SerializeField] private ENpcLocationType _locationType;

    [SerializeField] private string _locationKey;

    [SerializeField] private Transform _point;

    public string NpcId => _npcData != null ? _npcData.NpcId : "";

    public ENpcLocationType LocationType => _locationType;

    public string LocationKey => _locationKey;

    public Vector3 Position => _point != null ? _point.position : transform.position;

    private void Start()
    {
        if (NpcLocationManager.Instance != null)
        {
            NpcLocationManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        if (NpcLocationManager.Instance != null)
        {
            NpcLocationManager.Instance.Unregister(this);
        }
    }
}
