using UnityEngine;
using Unity.AI.Navigation;
using System;

public class MapNavMeshController : MonoBehaviour
{
    public static MapNavMeshController Instance { get; private set; }

    [Header("NavMeshSurface")]
    [SerializeField] private NavMeshSurface _navMeshSurface;

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

    public void BuildInitialNavMesh()
    {
        if (_navMeshSurface == null)
        {
#if UNITY_EDITOR
            Debug.LogError("NavMeshSurface가 존재하지 않습니다.");
#endif
            return;
        }

        _navMeshSurface.BuildNavMesh();
        IsReady = true;
        OnNavMeshRebuilt?.Invoke();
    }
}
