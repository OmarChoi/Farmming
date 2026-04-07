using UnityEngine;

public class NpcLocationAnchor : MonoBehaviour
{
    [SerializeField] private NpcDataSO _npcData;

    [SerializeField] private ENpcLocationType _locationType;

    [SerializeField] private string _locationKey;

    [SerializeField] private Transform _point;

    [Header("스타일링 숨김 대상")]
    [SerializeField] private GameObject _stylingHideRoot;

    public string NpcId => _npcData != null ? _npcData.NpcId : "";

    public ENpcLocationType LocationType => _locationType;

    public string LocationKey => _locationKey;

    public Vector3 Position => _point != null ? _point.position : transform.position;

    private void OnEnable()
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

    public GameObject StylingHideRoot
    {
        get
        {
            if (_stylingHideRoot != null) return _stylingHideRoot;

            return transform.root.gameObject;
        }
    }
}
