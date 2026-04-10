using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EvolutionManager : MonoBehaviour
{
    [Header("Studio")]
    [SerializeField] private Transform _studioAnchor;
    [SerializeField] private Transform _beforeModelRoot;
    [SerializeField] private Transform _afterModelRoot;
    [SerializeField] private Transform _orbitPivot;
    [SerializeField] private string _focusLayerName = "Evolution_Focus";
    [SerializeField] private Vector3 _beforeLocalEuler;
    [SerializeField] private Vector3 _afterLocalEuler;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector _director;

    [Header("URP Camera Stack")]
    [SerializeField] private Camera _baseCamera;
    [SerializeField] private Camera _evolutionOverlayCamera;
    [SerializeField] private CinemachineCamera _evolutionCinemachine;
    [SerializeField] private int _cutsceneCameraPriority = 100;
    [SerializeField] private bool _disableBaseCameraWhilePlaying;

    [Header("White Flash")]
    [SerializeField] private CanvasGroup _whiteFlashCanvasGroup;
    [SerializeField] private Color _whiteFlashColor = Color.white;
    [SerializeField] private float _whiteFlashFadeIn = 0.06f;
    [SerializeField] private float _whiteFlashHold = 0.03f;
    [SerializeField] private float _whiteFlashFadeOut = 0.18f;

    public event Action<HelperDataSO> OnEvolutionFinished;

    private readonly List<LayerCacheEntry> _layerCache = new();
    private readonly List<BehaviourCacheEntry> _behaviourCache = new();
    private readonly List<ColliderCacheEntry> _colliderCache = new();
    private readonly List<RigidbodyCacheEntry> _rigidbodyCache = new();

    private HelperDataSO _currentData;
    private GameObject _sourceInstance;
    private GameObject _evolvedInstance;
    private bool _sourceIsLiveHelper;
    private bool _cameraWasStacked;
    private bool _baseCameraWasEnabled;
    private bool _overlayCameraWasEnabled;
    private int _previousCinemachinePriority;
    private int _focusLayer = -1;

    private Transform _originalParent;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _originalLocalScale;

    private Action<bool> _onFinished;
    private Coroutine _whiteFlashCoroutine;

    public bool IsPlaying => _currentData != null;

    private void Reset()
    {
        AutoSetupEvolutionStudio();
    }

    private void Awake()
    {
        _focusLayer = LayerMask.NameToLayer(_focusLayerName);
        EnsureWhiteFlashOverlay();
        SetRootActive(_beforeModelRoot, false);
        SetRootActive(_afterModelRoot, false);
    }

    [ContextMenu("Auto Setup Evolution Studio")]
    private void AutoSetupEvolutionStudio()
    {
        Transform studioAnchor = FindOrCreateChild(transform, "StudioAnchor");
        Transform cameraRig = FindOrCreateChild(transform, "EvolutionCameraRig");
        Transform orbitPivot = FindOrCreateChild(cameraRig, "OrbitPivot");
        Transform beforeRoot = FindOrCreateChild(transform, "BeforeModelRoot");
        Transform afterRoot = FindOrCreateChild(transform, "AfterModelRoot");
        Transform overlayCameraTransform = FindOrCreateChild(cameraRig, "EvolutionOverlayCamera");
        Transform timelineTransform = FindOrCreateChild(transform, "EvolutionTimelineDirector");
        Transform cinemachineTransform = FindOrCreateChild(transform, "EvolutionCinemachineCamera");

        _studioAnchor = studioAnchor;
        _orbitPivot = orbitPivot;
        _beforeModelRoot = beforeRoot;
        _afterModelRoot = afterRoot;

        studioAnchor.localPosition = Vector3.zero;
        studioAnchor.localRotation = Quaternion.identity;

        cameraRig.localPosition = Vector3.zero;
        cameraRig.localRotation = Quaternion.identity;

        orbitPivot.localPosition = new Vector3(0f, 1.2f, 0f);
        orbitPivot.localRotation = Quaternion.identity;

        beforeRoot.localPosition = Vector3.zero;
        beforeRoot.localRotation = Quaternion.identity;
        afterRoot.localPosition = Vector3.zero;
        afterRoot.localRotation = Quaternion.identity;

        overlayCameraTransform.localPosition = new Vector3(0f, 1.8f, -6f);
        overlayCameraTransform.localRotation = Quaternion.identity;

        Camera overlayCamera = GetOrAddComponent<Camera>(overlayCameraTransform.gameObject);
        overlayCamera.enabled = false;
        overlayCamera.clearFlags = CameraClearFlags.Depth;
        overlayCamera.nearClipPlane = 0.01f;
        overlayCamera.farClipPlane = 100f;
        overlayCamera.cullingMask = GetFocusLayerMask();

        UniversalAdditionalCameraData overlayData = GetOrAddComponent<UniversalAdditionalCameraData>(overlayCameraTransform.gameObject);
        overlayData.renderType = CameraRenderType.Overlay;

        GetOrAddComponent<CinemachineBrain>(overlayCameraTransform.gameObject);

        _evolutionOverlayCamera = overlayCamera;

        _director = GetOrAddComponent<PlayableDirector>(timelineTransform.gameObject);
        _evolutionCinemachine = GetOrAddComponent<CinemachineCamera>(cinemachineTransform.gameObject);
        _evolutionCinemachine.Priority = 0;
        _evolutionCinemachine.Follow = orbitPivot;
        _evolutionCinemachine.LookAt = orbitPivot;

        if (_baseCamera == null)
            _baseCamera = Camera.main;

        EnsureWhiteFlashOverlay();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (_director != null) EditorUtility.SetDirty(_director);
        if (_evolutionOverlayCamera != null) EditorUtility.SetDirty(_evolutionOverlayCamera);
        if (_evolutionCinemachine != null) EditorUtility.SetDirty(_evolutionCinemachine);
#endif
    }

    private void OnDisable()
    {
        if (_director != null)
            _director.stopped -= HandleDirectorStopped;
    }

    public bool BeginEvolution(
        HelperDataSO data,
        EHelperGrade currentGrade,
        HelperController liveHelper,
        Action<bool> onFinished = null)
    {
        if (IsPlaying || data == null || _director == null)
            return false;
        if (_studioAnchor == null || _beforeModelRoot == null || _afterModelRoot == null)
            return false;

        HelperController beforePrefab = data.GetPrefabForGrade(currentGrade);
        HelperController afterPrefab = data.GetPrefabForGrade(GetNextGrade(currentGrade));
        if (beforePrefab == null || afterPrefab == null)
            return false;

        _currentData = data;
        _onFinished = onFinished;
        _sourceIsLiveHelper = liveHelper != null && liveHelper.HelperId == data.HelperId;

        GameObject beforeObject = _sourceIsLiveHelper
            ? liveHelper.gameObject
            : Instantiate(beforePrefab.gameObject);

        _sourceInstance = beforeObject;
        _evolvedInstance = Instantiate(afterPrefab.gameObject);

        PrepareRoots();
        PrepareSourceInstance(beforeObject);
        PreparePreviewInstance(_evolvedInstance, _afterModelRoot, _afterLocalEuler, startActive: false);

        SetLayerRecursively(_sourceInstance, _focusLayer);
        SetLayerRecursively(_evolvedInstance, _focusLayer);

        SetRootActive(_beforeModelRoot, true);
        SetRootActive(_afterModelRoot, false);

        EnableEvolutionCamera();

        _director.time = 0d;
        _director.Evaluate();
        _director.stopped -= HandleDirectorStopped;
        _director.stopped += HandleDirectorStopped;
        _director.Play();
        return true;
    }

    public void Timeline_PlayWhiteFlash()
    {
        if (!isActiveAndEnabled)
            return;

        if (_whiteFlashCoroutine != null)
            StopCoroutine(_whiteFlashCoroutine);

        _whiteFlashCoroutine = StartCoroutine(WhiteFlashCoroutine());
    }

    public void Timeline_SwapToEvolvedModel()
    {
        if (!IsPlaying || _evolvedInstance == null)
            return;

        SetRootActive(_beforeModelRoot, false);
        SetRootActive(_afterModelRoot, true);
        _evolvedInstance.SetActive(true);
        PlayIdleImmediately(_evolvedInstance);
    }

    public void Timeline_PlayEvolvedIdle()
    {
        if (_evolvedInstance == null)
            return;

        PlayIdleImmediately(_evolvedInstance);
    }

    public void CancelEvolution()
    {
        if (!IsPlaying)
            return;

        if (_director != null)
        {
            _director.stopped -= HandleDirectorStopped;
            _director.Stop();
        }

        FinishEvolution(false);
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        FinishEvolution(true);
    }

    private void FinishEvolution(bool completed)
    {
        if (_director != null)
            _director.stopped -= HandleDirectorStopped;

        DisableEvolutionCamera();

        if (_whiteFlashCoroutine != null)
        {
            StopCoroutine(_whiteFlashCoroutine);
            _whiteFlashCoroutine = null;
        }

        if (_whiteFlashCanvasGroup != null)
        {
            _whiteFlashCanvasGroup.alpha = 0f;
            _whiteFlashCanvasGroup.gameObject.SetActive(false);
        }

        RestoreSourceInstance();
        DestroyPreviewInstances();

        HelperDataSO finishedData = _currentData;
        Action<bool> callback = _onFinished;

        _currentData = null;
        _onFinished = null;
        _sourceInstance = null;
        _evolvedInstance = null;
        _sourceIsLiveHelper = false;

        callback?.Invoke(completed);
        if (finishedData != null)
            OnEvolutionFinished?.Invoke(finishedData);
    }

    private void PrepareRoots()
    {
        _beforeModelRoot.SetParent(_studioAnchor, false);
        _afterModelRoot.SetParent(_studioAnchor, false);

        _beforeModelRoot.localPosition = Vector3.zero;
        _beforeModelRoot.localRotation = Quaternion.identity;
        _beforeModelRoot.localScale = Vector3.one;

        _afterModelRoot.localPosition = Vector3.zero;
        _afterModelRoot.localRotation = Quaternion.identity;
        _afterModelRoot.localScale = Vector3.one;

        if (_orbitPivot != null)
        {
            _orbitPivot.SetParent(_studioAnchor, false);
            _orbitPivot.localPosition = Vector3.zero;
            _orbitPivot.localRotation = Quaternion.identity;
        }
    }

    private void PrepareSourceInstance(GameObject source)
    {
        if (source == null)
            return;

        if (_sourceIsLiveHelper)
        {
            CacheOriginalTransform(source.transform);
            CacheLayers(source);
            CacheAndDisableLiveBehaviours(source);
        }
        else
        {
            PreparePreviewInstance(source, _beforeModelRoot, _beforeLocalEuler, startActive: true);
            return;
        }

        source.transform.SetParent(_beforeModelRoot, false);
        source.transform.localPosition = Vector3.zero;
        source.transform.localRotation = Quaternion.Euler(_beforeLocalEuler);
        source.transform.localScale = Vector3.one;
        source.SetActive(true);
        PlayIdleImmediately(source);
    }

    private void PreparePreviewInstance(GameObject instance, Transform parent, Vector3 localEuler, bool startActive)
    {
        if (instance == null || parent == null)
            return;

        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        instance.transform.localScale = Vector3.one;

        foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>(true))
        {
            if (!behaviour.enabled)
                continue;
            if (behaviour is Animator)
                continue;
            if (behaviour is HelperAnimationAbility)
                continue;

            behaviour.enabled = false;
        }

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        instance.SetActive(startActive);
    }

    private void CacheOriginalTransform(Transform target)
    {
        _originalParent = target.parent;
        _originalPosition = target.position;
        _originalRotation = target.rotation;
        _originalLocalScale = target.localScale;
    }

    private void CacheLayers(GameObject root)
    {
        _layerCache.Clear();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            _layerCache.Add(new LayerCacheEntry
            {
                Transform = child,
                Layer = child.gameObject.layer
            });
        }
    }

    private void CacheAndDisableLiveBehaviours(GameObject root)
    {
        _behaviourCache.Clear();
        _colliderCache.Clear();
        _rigidbodyCache.Clear();

        foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (!behaviour.enabled)
                continue;
            if (behaviour is Animator)
                continue;
            if (behaviour is HelperController)
                continue;
            if (behaviour is HelperAnimationAbility)
                continue;

            _behaviourCache.Add(new BehaviourCacheEntry
            {
                Behaviour = behaviour,
                WasEnabled = true
            });
            behaviour.enabled = false;
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            _colliderCache.Add(new ColliderCacheEntry
            {
                Collider = collider,
                WasEnabled = collider.enabled
            });
            collider.enabled = false;
        }

        foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            _rigidbodyCache.Add(new RigidbodyCacheEntry
            {
                Rigidbody = rb,
                WasKinematic = rb.isKinematic,
                OriginalVelocity = rb.linearVelocity,
                OriginalAngularVelocity = rb.angularVelocity
            });

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreSourceInstance()
    {
        if (_sourceInstance == null)
            return;

        if (_sourceIsLiveHelper)
        {
            foreach (LayerCacheEntry entry in _layerCache)
            {
                if (entry.Transform != null)
                    entry.Transform.gameObject.layer = entry.Layer;
            }

            foreach (BehaviourCacheEntry entry in _behaviourCache)
            {
                if (entry.Behaviour != null)
                    entry.Behaviour.enabled = entry.WasEnabled;
            }

            foreach (ColliderCacheEntry entry in _colliderCache)
            {
                if (entry.Collider != null)
                    entry.Collider.enabled = entry.WasEnabled;
            }

            foreach (RigidbodyCacheEntry entry in _rigidbodyCache)
            {
                if (entry.Rigidbody == null)
                    continue;

                entry.Rigidbody.isKinematic = entry.WasKinematic;
                entry.Rigidbody.linearVelocity = entry.OriginalVelocity;
                entry.Rigidbody.angularVelocity = entry.OriginalAngularVelocity;
            }

            _sourceInstance.transform.SetParent(_originalParent, true);
            _sourceInstance.transform.position = _originalPosition;
            _sourceInstance.transform.rotation = _originalRotation;
            _sourceInstance.transform.localScale = _originalLocalScale;
            _sourceInstance.SetActive(true);
        }
        else
        {
            Destroy(_sourceInstance);
        }

        SetRootActive(_beforeModelRoot, false);
        SetRootActive(_afterModelRoot, false);
    }

    private void DestroyPreviewInstances()
    {
        if (_evolvedInstance != null)
            Destroy(_evolvedInstance);
    }

    private void EnableEvolutionCamera()
    {
        if (_baseCamera == null)
            _baseCamera = Camera.main;

        if (_baseCamera != null)
            _baseCameraWasEnabled = _baseCamera.enabled;

        if (_evolutionOverlayCamera != null)
        {
            _overlayCameraWasEnabled = _evolutionOverlayCamera.enabled;
            _evolutionOverlayCamera.enabled = true;

            UniversalAdditionalCameraData baseData = _baseCamera != null
                ? _baseCamera.GetUniversalAdditionalCameraData()
                : null;
            UniversalAdditionalCameraData overlayData = _evolutionOverlayCamera.GetUniversalAdditionalCameraData();

            if (overlayData != null)
            {
                overlayData.renderType = CameraRenderType.Overlay;
            }

            if (baseData != null)
            {
                _cameraWasStacked = baseData.cameraStack.Contains(_evolutionOverlayCamera);
                if (!_cameraWasStacked)
                    baseData.cameraStack.Add(_evolutionOverlayCamera);
            }
        }

        bool usesOverlayStack = _evolutionOverlayCamera != null;
        if (_disableBaseCameraWhilePlaying && !usesOverlayStack && _baseCamera != null)
            _baseCamera.enabled = false;

        if (_evolutionCinemachine != null)
        {
            _previousCinemachinePriority = _evolutionCinemachine.Priority;
            _evolutionCinemachine.Priority = _cutsceneCameraPriority;
            Transform target = _orbitPivot != null ? _orbitPivot : _beforeModelRoot;
            _evolutionCinemachine.Follow = target;
            _evolutionCinemachine.LookAt = target;
        }
    }

    private void DisableEvolutionCamera()
    {
        if (_baseCamera != null)
            _baseCamera.enabled = _baseCameraWasEnabled;

        if (_evolutionOverlayCamera != null)
        {
            UniversalAdditionalCameraData baseData = _baseCamera != null
                ? _baseCamera.GetUniversalAdditionalCameraData()
                : null;

            if (baseData != null && !_cameraWasStacked)
                baseData.cameraStack.Remove(_evolutionOverlayCamera);

            _evolutionOverlayCamera.enabled = _overlayCameraWasEnabled;
        }

        if (_evolutionCinemachine != null)
        {
            _evolutionCinemachine.Priority = _previousCinemachinePriority;
            _evolutionCinemachine.Follow = null;
            _evolutionCinemachine.LookAt = null;
        }
    }

    private void EnsureWhiteFlashOverlay()
    {
        if (_whiteFlashCanvasGroup != null)
            return;

        GameObject canvasGo = new GameObject("EvolutionWhiteFlashCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imageGo = new GameObject("WhiteFlash");
        imageGo.transform.SetParent(canvasGo.transform, false);

        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageGo.AddComponent<Image>();
        image.color = _whiteFlashColor;

        _whiteFlashCanvasGroup = imageGo.AddComponent<CanvasGroup>();
        _whiteFlashCanvasGroup.alpha = 0f;
        canvasGo.SetActive(false);
    }

    private IEnumerator WhiteFlashCoroutine()
    {
        if (_whiteFlashCanvasGroup == null)
            yield break;

        GameObject flashRoot = _whiteFlashCanvasGroup.transform.parent != null
            ? _whiteFlashCanvasGroup.transform.parent.gameObject
            : _whiteFlashCanvasGroup.gameObject;

        flashRoot.SetActive(true);
        yield return FadeWhiteFlash(0f, 1f, _whiteFlashFadeIn);

        if (_whiteFlashHold > 0f)
            yield return new WaitForSeconds(_whiteFlashHold);

        yield return FadeWhiteFlash(1f, 0f, _whiteFlashFadeOut);
        flashRoot.SetActive(false);
        _whiteFlashCoroutine = null;
    }

    private IEnumerator FadeWhiteFlash(float from, float to, float duration)
    {
        if (_whiteFlashCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            _whiteFlashCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _whiteFlashCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        _whiteFlashCanvasGroup.alpha = to;
    }

    private void PlayIdleImmediately(GameObject target)
    {
        if (target == null)
            return;

        HelperAnimationAbility animationAbility = target.GetComponent<HelperAnimationAbility>()
            ?? target.GetComponentInChildren<HelperAnimationAbility>(true);
        if (animationAbility != null)
        {
            animationAbility.PlayLocal(EHelperAnim.Idle);
            return;
        }

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
        animator.Play(0, 0, 0f);
    }

    private static void SetRootActive(Transform root, bool active)
    {
        if (root != null)
            root.gameObject.SetActive(active);
    }

    private int GetFocusLayerMask()
    {
        if (_focusLayer < 0)
            _focusLayer = LayerMask.NameToLayer(_focusLayerName);

        if (_focusLayer < 0)
            return 0;

        return 1 << _focusLayer;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();
        return component;
    }

    private static EHelperGrade GetNextGrade(EHelperGrade currentGrade)
    {
        return currentGrade switch
        {
            EHelperGrade.Normal => EHelperGrade.Epic,
            EHelperGrade.Epic => EHelperGrade.Legendary,
            _ => EHelperGrade.Legendary
        };
    }

    public void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    [Serializable]
    private struct LayerCacheEntry
    {
        public Transform Transform;
        public int Layer;
    }

    [Serializable]
    private struct BehaviourCacheEntry
    {
        public Behaviour Behaviour;
        public bool WasEnabled;
    }

    [Serializable]
    private struct ColliderCacheEntry
    {
        public Collider Collider;
        public bool WasEnabled;
    }

    [Serializable]
    private struct RigidbodyCacheEntry
    {
        public Rigidbody Rigidbody;
        public bool WasKinematic;
        public Vector3 OriginalVelocity;
        public Vector3 OriginalAngularVelocity;
    }
}
