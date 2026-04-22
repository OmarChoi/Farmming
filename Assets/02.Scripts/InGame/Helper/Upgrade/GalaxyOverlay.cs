using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GalaxyOverlay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _galaxyImage;

    [Header("Fade")]
    [SerializeField] private float _fadeInDuration = 0.6f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public IEnumerator FadeIn()
    {
        gameObject.SetActive(true);
        yield return Fade(0f, 1f, _fadeInDuration);
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(1f, 0f, _fadeOutDuration);
        gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        _canvasGroup.alpha = to;
    }
}
