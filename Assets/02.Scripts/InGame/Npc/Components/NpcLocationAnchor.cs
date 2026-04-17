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

    [Header("자동 등록")]
    [SerializeField] private bool _registerOnEnable = false;

    private bool _isRegistered;

    public string NpcId => _npcData != null ? _npcData.NpcId : "";

    public string RuntimeNpcKey => _runtimeNpcKey;

    public ENpcLocationType LocationType => _locationType;

    public string LocationKey => _locationKey;

    public Transform Point => _point != null ? _point : transform;

    public GameObject StylingHideRoot
    {
        get
        {
            if (_stylingHideRoot != null) return _stylingHideRoot;
            return transform.root.gameObject;
        }
    }

    private void OnEnable()
    {
        if (!_registerOnEnable) return;
        TryRegister();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void Initialize(
        NpcDataSO npcData,
        string runtimeNpcKey,
        ENpcLocationType locationType,
        string locationKey,
        Transform point = null,
        GameObject stylingHideRoot = null)
    {
        Unregister();

        _npcData = npcData;
        _runtimeNpcKey = runtimeNpcKey;
        _locationType = locationType;
        _locationKey = locationKey;
        _point = point != null ? point : transform;
        _stylingHideRoot = stylingHideRoot;

        TryRegister();
    }

    public void SetRegisterOnEnable(bool registerOnEnable)
    {
        _registerOnEnable = registerOnEnable;
    }

    private void TryRegister()
    {
        if (_isRegistered) return;
        if (NpcLocationManager.Instance == null) return;
        if (_npcData == null) return;
        if (string.IsNullOrEmpty(_runtimeNpcKey)) return;

        NpcLocationManager.Instance.Register(this);
        _isRegistered = true;
    }

    private void Unregister()
    {
        if (!_isRegistered) return;
        if (NpcLocationManager.Instance == null) return;

        NpcLocationManager.Instance.Unregister(this);
        _isRegistered = false;
    }
}
