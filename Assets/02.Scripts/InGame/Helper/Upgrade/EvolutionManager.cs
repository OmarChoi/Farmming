using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class EvolutionManager : MonoBehaviour
{
    [Header("Studio")]
    [SerializeField] private Transform _studioAnchor;
    [SerializeField] private Transform _backgroundRoot;
    [SerializeField] private Transform _beforeModelRoot;
    [SerializeField] private Transform _afterModelRoot;
    [SerializeField] private Transform _orbitPivot;
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private string _focusLayerName = "Evolution_Focus";
    [SerializeField] private Vector3 _beforeLocalEuler;
    [SerializeField] private Vector3 _afterLocalEuler;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector _director;

    [Header("Camera")]
    [SerializeField] private Camera _baseCamera;
    [SerializeField] private Camera _evolutionOverlayCamera;
    [SerializeField] private CinemachineCamera _evolutionCinemachine;
    [SerializeField] private int _cutsceneCameraPriority = 100;

    [Header("White Flash")]
    [SerializeField] private CanvasGroup _whiteFlashCanvasGroup;
    [SerializeField] private Color _whiteFlashColor = Color.white;
    [SerializeField] private float _whiteFlashFadeIn = 0.06f;
    [SerializeField] private float _whiteFlashHold = 0.03f;
    [SerializeField] private float _whiteFlashFadeOut = 0.18f;

    public event Action<HelperDataSO> OnEvolutionFinished;
    public bool IsPlaying => _currentData != null;

    private readonly List<(Transform transform, int layer)> _layerCache = new();
    private readonly List<(Behaviour behaviour, bool enabled)> _behaviourCache = new();
    private readonly List<(Collider collider, bool enabled)> _colliderCache = new();

    private HelperDataSO _currentData;
    private HelperEvolutionProfileSO _currentProfile;
    private GameObject _beforeInstance;
    private GameObject _afterInstance;
    private Action<bool> _onFinished;
    private Transform _originalParent;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _originalScale;
    private bool _usedLiveHelper;
    private bool _wasOverlayEnabled;
    private bool _wasStacked;
    private int _previousPriority;
    private Coroutine _whiteFlashRoutine;
    private Coroutine _fallbackRoutine;

    private int FocusLayer => LayerMask.NameToLayer(_focusLayerName);

    private void Awake()
    {
        EnsureWhiteFlashOverlay();
        if (_backgroundRenderer != null)
            _backgroundRenderer.enabled = _backgroundRenderer.sprite != null;
        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, false);
    }

    private void OnDisable()
    {
        if (_director != null)
            _director.stopped -= OnDirectorStopped;
    }

    public bool BeginEvolution(HelperDataSO data, EHelperGrade currentGrade, HelperController liveHelper, Action<bool> onFinished = null)
    {
        if (IsPlaying || data == null || _studioAnchor == null || _beforeModelRoot == null || _afterModelRoot == null || _orbitPivot == null)
            return false;

        HelperController beforePrefab = data.GetPrefabForGrade(currentGrade);
        HelperController afterPrefab = data.GetPrefabForGrade(GetNextGrade(currentGrade));
        if (beforePrefab == null || afterPrefab == null)
            return false;

        _currentData = data;
        _currentProfile = data.EvolutionProfile;
        _onFinished = onFinished;
        _usedLiveHelper = liveHelper != null && liveHelper.HelperId == data.HelperId;

        ApplyProfile(_currentProfile);

        _beforeInstance = _usedLiveHelper ? liveHelper.gameObject : Instantiate(beforePrefab.gameObject);
        _afterInstance = Instantiate(afterPrefab.gameObject);

        PrepareBeforeInstance();
        PreparePreview(_afterInstance, _afterModelRoot, _afterLocalEuler, false);
        SetLayerRecursively(_beforeInstance, FocusLayer);
        SetLayerRecursively(_afterInstance, FocusLayer);
        EnableEvolutionCamera();

        if (_director != null && _director.playableAsset != null)
        {
            _director.stopped -= OnDirectorStopped;
            _director.stopped += OnDirectorStopped;
            _director.time = 0d;
            _director.Evaluate();
            _director.Play();
        }
        else
        {
            _fallbackRoutine = StartCoroutine(FallbackEvolution());
        }

        return true;
    }

    public void Timeline_PlayWhiteFlash()
    {
        if (_whiteFlashRoutine != null) StopCoroutine(_whiteFlashRoutine);
        _whiteFlashRoutine = StartCoroutine(WhiteFlash());
    }

    public void Timeline_SwapToEvolvedModel()
    {
        if (_afterInstance == null) return;
        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, true);
        _afterInstance.SetActive(true);
        PlayIdle(_afterInstance);
    }

    public void Timeline_PlayEvolvedIdle() => PlayIdle(_afterInstance);

    public void CancelEvolution()
    {
        if (!IsPlaying) return;
        if (_fallbackRoutine != null) StopCoroutine(_fallbackRoutine);
        if (_director != null) { _director.stopped -= OnDirectorStopped; _director.Stop(); }
        FinishEvolution(false);
    }

    private void OnDirectorStopped(PlayableDirector _) => FinishEvolution(true);

    private void ApplyProfile(HelperEvolutionProfileSO profile)
    {
        if (profile == null) return;
        _beforeLocalEuler = profile.BeforeLocalEuler;
        _afterLocalEuler = profile.AfterLocalEuler;
        _whiteFlashColor = profile.FlashColor;
        if (_director != null) _director.playableAsset = profile.TimelineAsset;
        if (_orbitPivot != null) _orbitPivot.localPosition = new Vector3(0f, profile.PivotHeight, 0f);
        if (_backgroundRenderer != null)
        {
            _backgroundRenderer.sprite = profile.BackgroundSprite;
            _backgroundRenderer.enabled = profile.BackgroundSprite != null;
        }
    }

    private void PrepareBeforeInstance()
    {
        if (_beforeInstance == null) return;
        if (_usedLiveHelper)
        {
            _originalParent = _beforeInstance.transform.parent;
            _originalPosition = _beforeInstance.transform.position;
            _originalRotation = _beforeInstance.transform.rotation;
            _originalScale = _beforeInstance.transform.localScale;
            CacheAndDisableLiveState(_beforeInstance);
        }
        PreparePreview(_beforeInstance, _beforeModelRoot, _beforeLocalEuler, true);
    }

    private void PreparePreview(GameObject instance, Transform parent, Vector3 localEuler, bool active)
    {
        if (instance == null || parent == null) return;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        instance.transform.localScale = Vector3.one;

        foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>(true))
        {
            if (!behaviour.enabled || behaviour is Animator || behaviour is HelperAnimationAbility) continue;
            if (_usedLiveHelper && behaviour.gameObject.transform.IsChildOf(instance.transform))
            {
                // live helper state cached elsewhere
            }
            else behaviour.enabled = false;
        }
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        instance.SetActive(active);
        if (active) PlayIdle(instance);
    }

    private void CacheAndDisableLiveState(GameObject root)
    {
        _layerCache.Clear(); _behaviourCache.Clear(); _colliderCache.Clear();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) _layerCache.Add((child, child.gameObject.layer));
        foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (!behaviour.enabled || behaviour is Animator || behaviour is HelperAnimationAbility || behaviour is HelperController) continue;
            _behaviourCache.Add((behaviour, behaviour.enabled));
            behaviour.enabled = false;
        }
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            _colliderCache.Add((collider, collider.enabled));
            collider.enabled = false;
        }
    }

    private void EnableEvolutionCamera()
    {
        if (_baseCamera == null) _baseCamera = Camera.main;
        if (_evolutionOverlayCamera == null) return;

        _wasOverlayEnabled = _evolutionOverlayCamera.enabled;
        _evolutionOverlayCamera.enabled = true;
        _evolutionOverlayCamera.cullingMask = FocusLayer >= 0 ? 1 << FocusLayer : 0;

        if (_baseCamera != null)
        {
            UniversalAdditionalCameraData baseData = _baseCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData overlayData = _evolutionOverlayCamera.GetUniversalAdditionalCameraData();
            if (overlayData != null) overlayData.renderType = CameraRenderType.Overlay;
            if (baseData != null)
            {
                _wasStacked = baseData.cameraStack.Contains(_evolutionOverlayCamera);
                if (!_wasStacked) baseData.cameraStack.Add(_evolutionOverlayCamera);
            }
        }

        if (_evolutionCinemachine != null)
        {
            _previousPriority = _evolutionCinemachine.Priority;
            _evolutionCinemachine.Priority = _cutsceneCameraPriority;
            _evolutionCinemachine.Follow = _orbitPivot;
            _evolutionCinemachine.LookAt = _orbitPivot;
        }
    }

    private void DisableEvolutionCamera()
    {
        if (_evolutionOverlayCamera != null)
        {
            if (_baseCamera != null)
            {
                UniversalAdditionalCameraData baseData = _baseCamera.GetUniversalAdditionalCameraData();
                if (baseData != null && !_wasStacked) baseData.cameraStack.Remove(_evolutionOverlayCamera);
            }
            _evolutionOverlayCamera.enabled = _wasOverlayEnabled;
        }
        if (_evolutionCinemachine != null)
        {
            _evolutionCinemachine.Priority = _previousPriority;
            _evolutionCinemachine.Follow = null;
            _evolutionCinemachine.LookAt = null;
        }
    }

    private IEnumerator FallbackEvolution()
    {
        HelperEvolutionProfileSO p = _currentProfile;
        if (_evolutionOverlayCamera == null) { FinishEvolution(true); yield break; }

        Transform cam = _evolutionOverlayCamera.transform;
        Vector3 intro = p != null ? p.IntroCameraLocalPosition : new Vector3(0f, 1.8f, -6f);
        Vector3 zoom = p != null ? p.ZoomCameraLocalPosition : new Vector3(0f, 1.35f, -3.4f);
        Vector3 reveal = p != null ? p.RevealCameraLocalPosition : new Vector3(0f, 0.45f, -2.5f);
        Vector3 final = p != null ? p.FinalCameraLocalPosition : new Vector3(0f, 1.5f, -5.5f);
        float introDur = p != null ? p.IntroDuration : 2f;
        float zoomDur = p != null ? p.ZoomDuration : 0.8f;
        float revealDur = p != null ? p.RevealDuration : 1.2f;
        float outroDur = p != null ? p.OutroDuration : 1f;
        float spin = p != null ? p.IntroSpinSpeed : 90f;

        cam.localPosition = intro;
        float t = 0f;
        while (t < introDur) { t += Time.deltaTime; RotateVisibleModel(spin * Time.deltaTime); LookAtPivot(cam); yield return null; }
        yield return MoveCamera(cam, intro, zoom, zoomDur);
        Timeline_PlayWhiteFlash();
        yield return new WaitForSeconds(Mathf.Max(_whiteFlashFadeIn * 0.75f, 0.03f));
        Timeline_SwapToEvolvedModel();
        cam.localPosition = reveal;
        yield return MoveCamera(cam, reveal, final, revealDur);
        yield return MoveCamera(cam, final, final + new Vector3(0f, 0.15f, -1.1f), outroDur);
        _fallbackRoutine = null;
        FinishEvolution(true);
    }

    private IEnumerator MoveCamera(Transform cam, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cam.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            LookAtPivot(cam);
            yield return null;
        }
        cam.localPosition = to;
        LookAtPivot(cam);
    }

    private void RotateVisibleModel(float yaw)
    {
        if (_beforeModelRoot != null && _beforeModelRoot.gameObject.activeSelf) _beforeModelRoot.Rotate(0f, yaw, 0f);
        else if (_afterModelRoot != null && _afterModelRoot.gameObject.activeSelf) _afterModelRoot.Rotate(0f, yaw, 0f);
    }

    private void LookAtPivot(Transform cam)
    {
        if (cam == null || _orbitPivot == null) return;
        Vector3 dir = _orbitPivot.position - cam.position;
        if (dir.sqrMagnitude > 0.0001f) cam.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private IEnumerator WhiteFlash()
    {
        if (_whiteFlashCanvasGroup == null) yield break;
        GameObject root = _whiteFlashCanvasGroup.transform.parent.gameObject;
        Image image = _whiteFlashCanvasGroup.GetComponent<Image>();
        if (image != null) image.color = _whiteFlashColor;
        root.SetActive(true);
        yield return FadeFlash(0f, 1f, _whiteFlashFadeIn);
        if (_whiteFlashHold > 0f) yield return new WaitForSeconds(_whiteFlashHold);
        yield return FadeFlash(1f, 0f, _whiteFlashFadeOut);
        root.SetActive(false);
        _whiteFlashRoutine = null;
    }

    private IEnumerator FadeFlash(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _whiteFlashCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _whiteFlashCanvasGroup.alpha = to;
    }

    private void FinishEvolution(bool completed)
    {
        DisableEvolutionCamera();
        if (_whiteFlashRoutine != null) StopCoroutine(_whiteFlashRoutine);
        if (_whiteFlashCanvasGroup != null) { _whiteFlashCanvasGroup.alpha = 0f; _whiteFlashCanvasGroup.transform.parent.gameObject.SetActive(false); }

        if (_usedLiveHelper && _beforeInstance != null)
        {
            foreach (var entry in _layerCache) if (entry.transform != null) entry.transform.gameObject.layer = entry.layer;
            foreach (var entry in _behaviourCache) if (entry.behaviour != null) entry.behaviour.enabled = entry.enabled;
            foreach (var entry in _colliderCache) if (entry.collider != null) entry.collider.enabled = entry.enabled;
            _beforeInstance.transform.SetParent(_originalParent, true);
            _beforeInstance.transform.position = _originalPosition;
            _beforeInstance.transform.rotation = _originalRotation;
            _beforeInstance.transform.localScale = _originalScale;
        }
        else if (_beforeInstance != null) Destroy(_beforeInstance);

        if (_afterInstance != null) Destroy(_afterInstance);
        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, false);

        HelperDataSO finished = _currentData;
        Action<bool> callback = _onFinished;
        _currentData = null; _currentProfile = null; _beforeInstance = null; _afterInstance = null; _onFinished = null; _usedLiveHelper = false;
        callback?.Invoke(completed);
        if (finished != null) OnEvolutionFinished?.Invoke(finished);
    }

    private void EnsureWhiteFlashOverlay()
    {
        if (_whiteFlashCanvasGroup != null) return;
        GameObject canvasGo = new GameObject("EvolutionWhiteFlashCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGo.AddComponent<GraphicRaycaster>();
        GameObject imageGo = new GameObject("WhiteFlash");
        imageGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image image = imageGo.AddComponent<Image>();
        image.color = _whiteFlashColor;
        _whiteFlashCanvasGroup = imageGo.AddComponent<CanvasGroup>();
        _whiteFlashCanvasGroup.alpha = 0f;
        canvasGo.SetActive(false);
    }

    private static void PlayIdle(GameObject target)
    {
        if (target == null) return;
        HelperAnimationAbility ability = target.GetComponent<HelperAnimationAbility>() ?? target.GetComponentInChildren<HelperAnimationAbility>(true);
        if (ability != null) { ability.PlayLocal(EHelperAnim.Idle); return; }
        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null) return;
        animator.Rebind(); animator.Update(0f); animator.Play(0, 0, 0f);
    }

    private static void SetRoot(Transform root, bool active) { if (root != null) root.gameObject.SetActive(active); }

    private static EHelperGrade GetNextGrade(EHelperGrade currentGrade)
    {
        return currentGrade == EHelperGrade.Normal ? EHelperGrade.Epic :
               currentGrade == EHelperGrade.Epic ? EHelperGrade.Legendary : EHelperGrade.Legendary;
    }

    public void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0) return;
        foreach (Transform child in target.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }
}
