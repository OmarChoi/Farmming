using UnityEngine;

public class NpcLocationAnchor : MonoBehaviour
{
    [Header("NPC 데이터")]
    [SerializeField] private NpcDataSO _npcData;
    [SerializeField] private string _runtimeNpcKey;

    [Header("위치 정보")]
    [SerializeField] private ENpcLocationType _locationType;
    [SerializeField] private string _locationKey;
    [SerializeField] private Transform _point;

    [Header("스타일링 숨김 대상")]
    [SerializeField] private GameObject _stylingHideRoot;

    public string NpcId => _npcData != null ? _npcData.NpcId : "";

    public string RuntimeNpcKey => string.IsNullOrEmpty(_runtimeNpcKey) ? NpcId : _runtimeNpcKey;

    public ENpcLocationType LocationType => _locationType;

    public string LocationKey => _locationKey;

    public Transform Point => _point != null ? _point : transform;

    private void OnEnable()
    {
        if (GetComponentInParent<BuildingGhostMarker>(true) != null) return;

        if (NpcLocationManager.Instance != null)
        {
            NpcLocationManager.Instance.Register(this);
        }
    }

    private void OnDisable()
    {
        if (GetComponentInParent<BuildingGhostMarker>(true) != null) return;

        if (NpcLocationManager.Instance != null)
        {
            NpcLocationManager.Instance.Unregister(this);
        }
    }

    public void Initialize(
        NpcDataSO npcData,
        string runtimeNpcKey,
        ENpcLocationType locationType,
        string locationKey,
        Transform point = null,
        GameObject stylingHideRoot = null)
    {
        if (NpcLocationManager.Instance != null)
        {
            NpcLocationManager.Instance.Unregister(this);
        }

        _npcData = npcData;
        _runtimeNpcKey = runtimeNpcKey;
        _locationType = locationType;
        _locationKey = locationKey;
        _point = point != null ? point : transform;
        _stylingHideRoot = stylingHideRoot;

        if (NpcLocationManager.Instance != null)
        {
            NpcLocationManager.Instance.Register(this);
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
