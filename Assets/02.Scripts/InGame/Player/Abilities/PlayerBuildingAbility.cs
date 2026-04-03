using UnityEngine;

public class PlayerBuildingAbility : PlayerAbility
{
    // ── Inspector 설정 ──────────────────────────────────
    [Header("Ghost 설정")]
    [SerializeField] private Material _ghostMaterial;
    [SerializeField] private Color _ghostValidColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color _ghostInvalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("키 설정")]
    [SerializeField] private KeyCode _placeKey = KeyCode.B;
    [SerializeField] private KeyCode _removeKey = KeyCode.V;
    [SerializeField] private KeyCode _rotateKey = KeyCode.R;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    // ── 외부 참조 ───────────────────────────────────────
    private PlayerTerrainAbility _terrainAbility;
    private PlayerBuildSession _session;
    private PlayerBuildResourceTracker _resourceHandler;
    private bool _hasStarted;

    private bool IsLocalPlayer => _owner != null && _owner.IsMine;

    protected override void Awake()
    {
        base.Awake();

        var buildingManager = BuildingManager.Instance;
        _terrainAbility = _owner?.GetAbility<PlayerTerrainAbility>();
        _resourceHandler = new PlayerBuildResourceTracker(buildingManager, _owner);

        var ghostConfig = new GhostConfig(_ghostMaterial, _ghostValidColor, _ghostInvalidColor);
        _session = new PlayerBuildSession(buildingManager, _owner, _resourceHandler, ghostConfig);
    }

    private void Start()
    {
        _hasStarted = true;
        if (IsLocalPlayer) _resourceHandler?.Bind();
    }

    private void OnEnable()
    {
        if (!_hasStarted) return;
        if (IsLocalPlayer) _resourceHandler?.Bind();
    }

    private void OnDisable()
    {
        _resourceHandler?.Unbind();

        if (IsLocalPlayer)
        {
            _session?.Dispose();
        }
    }

    private void OnDestroy()
    {
        _resourceHandler?.Dispose();
    }

    private void Update()
    {
        if (!IsLocalPlayer || !_owner.CanMove) return;

        if (_session.IsPreviewing)
        {
            if (Input.GetKeyDown(_cancelKey))  { _session.Cancel(); return; }
            if (Input.GetKeyDown(_rotateKey))  { _session.Rotate(); }
            if (Input.GetKeyDown(_placeKey))   { _session.TryPlace(GetFrontCell()); return; }
            _session.UpdatePreview(GetFrontCell());
        }
        else
        {
            if (Input.GetKeyDown(_removeKey))  { _session.TryRemove(GetFrontCell()); return; }
            if (Input.GetKeyDown(_placeKey))   { _session.OpenSelectionUIAsync(); }
        }
    }

    private TerrainCell GetFrontCell() => _terrainAbility?.GetFrontCell();
}
