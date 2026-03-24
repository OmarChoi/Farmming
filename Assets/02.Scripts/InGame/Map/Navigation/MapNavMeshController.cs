using UnityEngine;
using Unity.AI.Navigation;
using System;

public class MapNavMeshController : MonoBehaviour
{
    public static MapNavMeshController Instance { get; private set; }

    [Header("NavMeshSurface")]
    [SerializeField] private NavMeshSurface _navMeshSurface;

    [Header("재생성 시간 딜레이")]
    [SerializeField] private float _rebuildDelay = 0.4f;

    private bool _isInitialized;
    private bool _isDirty;
    private bool _isRebuildScheduled;
    private float _dirtyTimer;
    private Bounds _dirtyBounds;

    public bool IsReady { get; private set; }

    public event Action OnNavMeshRebuilt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_navMeshSurface == null)
        {
            _navMeshSurface = GetComponent<NavMeshSurface>();
        }
    }

    private void Update()
    {
        if (!_isInitialized) return;
        if (!_isRebuildScheduled) return;

        _dirtyTimer += Time.deltaTime;
        if (_dirtyTimer < _rebuildDelay) return;

        _dirtyTimer = 0f;
        _isRebuildScheduled = false;

        RebuildNow();
    }

    public void Initialize()
    {
        if (_navMeshSurface == null)
        {
#if UNITY_EDITOR
            Debug.LogError("NavMeshSurface가 존재하지 않습니다.");
#endif
            return;
        }

        _isInitialized = true;
        IsReady = false;
    }

    public void BuildInitialNavMesh()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        if (_navMeshSurface == null) return;

#if UNITY_EDITOR
        Debug.Log("NavMeshSurface 초기 생성");
#endif

        _navMeshSurface.BuildNavMesh();

        IsReady = true;
        _isDirty = false;
        _isRebuildScheduled = false;

        OnNavMeshRebuilt?.Invoke();
    }

    public void MarkDirty(Bounds changedBounds)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        if (!_isDirty)
        {
            _dirtyBounds = changedBounds;
            _isDirty = true;
        }
        else
        {
            _dirtyBounds.Encapsulate(changedBounds);
        }

        _dirtyTimer = 0f;
        _isRebuildScheduled = true;

#if UNITY_EDITOR
        Debug.Log($"[MapNavMeshController] MarkDirty: center={changedBounds.center}, size={changedBounds.size}");
#endif
    }

    public void RequestFullRebuild()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        _isDirty = true;
        _dirtyTimer = 0f;
        _isRebuildScheduled = true;

#if UNITY_EDITOR
        Debug.Log("NavMeshSurface 전체 재생성 리퀘스트");
#endif
    }

    public void RebuildNow()
    {
        if (_navMeshSurface == null) return;
        if (!_isDirty && IsReady) return;

#if UNITY_EDITOR
        Debug.Log("NavMeshSurface 지금 재생성");
#endif

        _navMeshSurface.BuildNavMesh();

        IsReady = true;
        _isDirty = false;

        OnNavMeshRebuilt?.Invoke();
    }
}
