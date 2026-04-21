using DG.Tweening;
using TMPro;
using UnityEngine;

public class UI_UnableActionText : MonoBehaviour
{
    public static UI_UnableActionText Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _text;

    private Tween _fadeTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UI_UnableActionText] Multiple instances found. Using the latest scene instance.");
        }

        Instance = this;

        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();

        if (_text != null)
            _text.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();

        if (Instance == this)
            Instance = null;
    }

    public static void Show(string message, float fadeDuration = 1.5f)
    {
        if (Instance == null)
        {
            Debug.Log(message);
            return;
        }

        Instance.ShowInternal(message, fadeDuration);
    }

    private void ShowInternal(string message, float fadeDuration)
    {
        if (_text == null)
        {
            Debug.Log(message);
            return;
        }

        _fadeTween?.Kill();
        _fadeTween = null;

        _text.gameObject.SetActive(true);
        _text.text = message;

        Color color = _text.color;
        color.a = 1f;
        _text.color = color;

        _fadeTween = _text
            .DOFade(0f, Mathf.Max(0.01f, fadeDuration))
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (_text != null)
                    _text.gameObject.SetActive(false);

                _fadeTween = null;
            });
    }

    private void OnValidate()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
    }
}
