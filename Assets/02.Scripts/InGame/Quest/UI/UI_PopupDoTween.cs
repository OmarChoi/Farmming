using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class UI_PopupDoTween : MonoBehaviour
{
    [Header("효과 대상")]
    [SerializeField] private GameObject _root;          // 켜고 끌 루트 오브젝트
    [SerializeField] private RectTransform _panel;      // 실제 스케일 연출 대상

    [Header("열기")]
    [SerializeField] private float _openStartScale = 0.85f;
    [SerializeField] private float _openOvershootScale = 1.08f;
    [SerializeField] private float _openDuration = 0.2f;

    [Header("닫기")]
    [SerializeField] private float _closePunchScale = 1.05f;
    [SerializeField] private float _closeEndScale = 0f;
    [SerializeField] private float _closeDuration = 0.16f;

    private Tween _currentTween;

    private void Reset()
    {
        _root = gameObject;
        _panel = GetComponent<RectTransform>();
    }

    public async UniTask PlayOpenAsync()
    {
        KillTween();

        if (_root != null)
        {
            _root.SetActive(true);
        }

        if (_panel == null) return;

        _panel.localScale = Vector3.one * _openStartScale;

        Sequence seq = DOTween.Sequence();
        _currentTween = seq;

        seq.Append(_panel.DOScale(_openOvershootScale, _openDuration * 0.6f).SetEase(Ease.OutBack));
        seq.Append(_panel.DOScale(1f, _openDuration * 0.4f).SetEase(Ease.OutCubic));

        await seq.AsyncWaitForCompletion();
    }

    public async UniTask PlayCloseAsync()
    {
        KillTween();

        if (_panel == null)
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
            return;
        }

        _panel.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        _currentTween = seq;

        seq.Append(_panel.DOScale(_closePunchScale, _closeDuration * 0.35f).SetEase(Ease.OutQuad));
        seq.Append(_panel.DOScale(_closeEndScale, _closeDuration * 0.65f).SetEase(Ease.InBack));

        await seq.AsyncWaitForCompletion();

        if (_root != null)
        {
            _root.SetActive(false);
        }

        _panel.localScale = Vector3.one;
    }

    private void KillTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
        }
    }

    private void OnDisable()
    {
        KillTween();
    }
}
