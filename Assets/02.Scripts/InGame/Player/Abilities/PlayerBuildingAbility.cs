using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBuildingAbility : PlayerAbility
{
    [Header("Input")]
    [SerializeField] private KeyCode _placeKey = KeyCode.B;
    [SerializeField] private KeyCode _removeKey = KeyCode.V;
    [SerializeField] private KeyCode _rotateKey = KeyCode.R;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    private PlayerTerrainAbility _terrainAbility;
    private PlayerBuildSession _session;
    private PlayerBuildResourceTracker _resourceHandler;
    private BuildingManager _boundBuildingManager;
    private bool _hasStarted;

    private bool IsLocalPlayer => _owner != null && _owner.IsMine;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner?.GetAbility<PlayerTerrainAbility>();
        
        GameSceneInit.OnCompleteInitialize += BindToBuildingManager;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindToBuildingManager();
    }

    private void BindToBuildingManager()
    {
        BuildingManager buildingManager = BuildingManager.Instance;
        if (buildingManager == null)
            return;

        if (_boundBuildingManager == buildingManager
            && _resourceHandler != null
            && _session != null)
        {
            return;
        }

        ReleaseBuildBindings();

        _boundBuildingManager = buildingManager;
        _resourceHandler = new PlayerBuildResourceTracker(buildingManager, _owner);
        GhostConfig ghostConfig = buildingManager.GhostConfig;
        _session = new PlayerBuildSession(buildingManager, _owner, _resourceHandler, ghostConfig);

        if (_hasStarted && isActiveAndEnabled && IsLocalPlayer)
            _resourceHandler.Bind();
    }
    
    private void Start()
    {
        _hasStarted = true;
        if (IsLocalPlayer) _resourceHandler?.Bind();
    }

    private void OnEnable()
    {
        if (!_hasStarted) return;
        BindToBuildingManager();
        if (IsLocalPlayer) _resourceHandler?.Bind();
    }

    private void OnDisable()
    {
        _resourceHandler?.Unbind();

        if (IsLocalPlayer)
        {
            _session?.Dispose();
            _session = null;
        }
    }

    private void OnDestroy()
    {
        ReleaseBuildBindings();
        GameSceneInit.OnCompleteInitialize -= BindToBuildingManager;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        BindToBuildingManager();
        if (_session == null) return;
        if (!IsLocalPlayer || !_owner.CanMove) return;

        if (_session.IsPreviewing)
        {
            if (Input.GetKeyDown(_cancelKey))
            {
                _session.Cancel();
                return;
            }
            if (Input.GetKeyDown(_rotateKey))
            {
                _session.Rotate();
            }
            if (Input.GetKeyDown(_placeKey))
            {
                _session.TryPlace(GetFrontCell());
                return;
            }
            _session.UpdatePreview(GetFrontCell());
        }
        else
        {
            if (Input.GetKeyDown(_removeKey))
            {
                _session.TryRemove(GetFrontCell());
                return;
            }
            if (Input.GetKeyDown(_placeKey))
            {
                _session.OpenSelectionUIAsync();
            }
        }
    }

    private TerrainCell GetFrontCell() => _terrainAbility?.GetFrontCell();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReleaseBuildBindings();
    }

    private void ReleaseBuildBindings()
    {
        _session?.Dispose();
        _session = null;

        _resourceHandler?.Dispose();
        _resourceHandler = null;
        _boundBuildingManager = null;
    }
}
