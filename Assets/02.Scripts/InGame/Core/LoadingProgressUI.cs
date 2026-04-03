using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressUI : MonoBehaviour
{
    public static LoadingProgressUI Instance { get; private set; }

    [SerializeField] private Image _fillBar;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _smoothSpeed = 5f;
    [SerializeField] private float _fadeOutSpeed = 2f;

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

    private void Update()
    {
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
