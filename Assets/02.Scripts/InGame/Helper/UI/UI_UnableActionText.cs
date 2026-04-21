using DG.Tweening;
using TMPro;
using UnityEngine;

public static class UI_UnableActionText
{
    private const string TextObjectName = "UnableActionText";

    private static TextMeshProUGUI _text;
    private static Tween _fadeTween;

    public static void Show(string message, float fadeDuration = 1.5f)
    {
        if (!TryResolveText())
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

    private static bool TryResolveText()
    {
        if (_text != null)
            return true;

        TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null) continue;
            if (!text.gameObject.scene.IsValid()) continue;
            if (text.gameObject.name != TextObjectName) continue;

            _text = text;
            return true;
        }

        return false;
    }
}
