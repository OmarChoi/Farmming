using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerOcclusionAbility : PlayerAbility
{
    [SerializeField] private LayerMask _occlusionLayer;
    [SerializeField] private Vector3 _playerOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float _castRadius = 0.75f;
    [SerializeField, Range(0.1f, 1f)] private float _transparentAlpha = 0.35f;
    [SerializeField] private float _fadeSpeed = 4f;

    private readonly HashSet<Wood> _currentHits = new();
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[32];

    private Camera _mainCamera;
    private Transform _rootTransform;
    private TreeTransparencyController _transparencyController;

    protected override void Awake()
    {
        base.Awake();
        _rootTransform = transform.root;
        _transparencyController = new TreeTransparencyController(_transparentAlpha, _fadeSpeed);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _transparencyController.RestoreAll();
        _transparencyController.Dispose();
    }

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_owner == null || !_owner.IsMine) return;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        CollectCurrentHits();
        _transparencyController.Update(_currentHits, Time.deltaTime);
    }

    private void CollectCurrentHits()
    {
        _currentHits.Clear();

        Vector3 cameraPosition = _mainCamera.transform.position;
        Vector3 playerPosition = _owner.transform.position + _playerOffset;
        Vector3 direction = playerPosition - cameraPosition;
        float distance = direction.magnitude;

        if (distance <= 0.1f)
        {
            return;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            cameraPosition,
            _castRadius,
            direction / distance,
            _hitBuffer,
            distance,
            _occlusionLayer,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = _hitBuffer[i].collider;
            _hitBuffer[i] = default;

            Wood tree = ResolveTree(collider);
            if (tree != null)
            {
                _currentHits.Add(tree);
            }
        }
    }

    private Wood ResolveTree(Collider collider)
    {
        if (collider == null) return null;
        if (collider.transform.IsChildOf(_rootTransform)) return null;

        Wood tree = collider.GetComponentInParent<Wood>();
        if (tree == null) return null;
        if (tree.transform.IsChildOf(_rootTransform)) return null;

        return tree;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _transparencyController.RestoreAll();
        _transparencyController.Dispose();
        _mainCamera = null;
    }
}
