using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HarvestNotification : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private RectTransform _background;
    [SerializeField] private CanvasGroup _canvasGroup;

    [SerializeField] private float _floatDistance = 80f;   // 떠오르는 거리
    [SerializeField] private float _duration = 1.8f;       // 전체 지속 시간
    [SerializeField] private float _fadeStartRatio = 0.5f; // 몇 % 지점부터 페이드

    public void Setup(Sprite icon, string seedName, int amount)
    {
        if (_icon != null)
        {
            _icon.gameObject.SetActive(true);
            _icon.sprite = icon;
        }

        if (_text != null)
            _text.text = $"{seedName}  x{amount}";

        // 텍스트 길이에 맞게 배경 가로 크기 자동 조절
        if (_background != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_background);
        StartCoroutine(PlayAnimation());
    }

    public void SetupMessage(string message)
    {
        if (_icon != null)
            _icon.gameObject.SetActive(false);

        if (_text != null)
            _text.text = message ?? string.Empty;

        if (_background != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_background);

        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * _floatDistance;

        _canvasGroup.alpha = 1f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;

            // 위로 떠오르기
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            // 페이드 아웃
            if (t >= _fadeStartRatio)
            {
                float fadeT = (t - _fadeStartRatio) / (1f - _fadeStartRatio);
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

}
