using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressUI : MonoBehaviour
{
    public static LoadingProgressUI Instance { get; private set; }

    [SerializeField] private Image _fillBar;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _smoothSpeed = 5f;
    [SerializeField] private float _fadeOutSpeed = 2f;

    private float _sceneLoadBase;
    private float _sceneLoadRange;
    private bool _trackingSceneLoad;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 씬 로딩 진행률 추적 시작.
    /// base ~ base+range 범위를 PhotonNetwork.LevelLoadingProgress로 채움.
    /// </summary>
    public void TrackSceneLoad(float baseValue, float range)
    {
        _sceneLoadBase = baseValue;
        _sceneLoadRange = range;
        _trackingSceneLoad = true;
    }

    private void Update()
    {
        if (_trackingSceneLoad && PhotonNetwork.IsConnected)
        {
            float sceneProgress = PhotonNetwork.LevelLoadingProgress;
            float mapped = _sceneLoadBase + _sceneLoadRange * sceneProgress;

            if (mapped > LoadingProgress.Value)
                LoadingProgress.Value = mapped;

            if (sceneProgress >= 1f)
                _trackingSceneLoad = false;
        }

        if (_fillBar != null)
        {
            float targetFill = Mathf.Clamp01(LoadingProgress.Value);
            _fillBar.fillAmount = Mathf.Lerp(_fillBar.fillAmount, targetFill, Time.unscaledDeltaTime * _smoothSpeed);
        }

        if (LoadingProgress.IsActive)
            return;

        if (_canvasGroup == null)
        {
            Destroy(gameObject);
            return;
        }

        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 0f, Time.unscaledDeltaTime * _fadeOutSpeed);
        if (_canvasGroup.alpha <= 0f)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}