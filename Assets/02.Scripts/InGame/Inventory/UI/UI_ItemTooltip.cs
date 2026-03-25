using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_ItemTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private RectTransform _leftArrow;
    [SerializeField] private RectTransform _rightArrow;
    [SerializeField] private float _offset = 10f;

    [Header("팝업 애니메이션")]
    [SerializeField] private float _popupDuration = 0.2f;

    [Header("화살표 애니메이션")]
    [SerializeField] private float _arrowMoveDistance = 5f;
    [SerializeField] private float _arrowDuration = 0.5f;

    private Tween _arrowTween;
    private Tween _popupTween;

    private void Awake()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    public void Show(ItemDataSO item, RectTransform slotRect, bool showRight)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        _nameText.text = item.DisplayName;
        _descriptionText.text = item.DisplayExplanation;

        _leftArrow.gameObject.SetActive(!showRight);
        _rightArrow.gameObject.SetActive(showRight);

        _popupTween?.Kill();
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(true);

        // scale이 1인 상태에서 레이아웃 갱신 후 위치 계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        UpdatePosition(slotRect, showRight);
        PlayArrowAnimation(showRight ? _rightArrow : _leftArrow, showRight);
        PlayPopup();
    }

    public void Hide()
    {
        _arrowTween?.Kill();
        _popupTween?.Kill();
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    private void PlayPopup()
    {
        _popupTween?.Kill();
        _rectTransform.localScale = Vector3.zero;
        _popupTween = _rectTransform.DOScale(Vector3.one, _popupDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);
    }

    private void PlayArrowAnimation(RectTransform arrow, bool showRight)
    {
        _arrowTween?.Kill();
        arrow.anchoredPosition = Vector2.zero;

        float direction = showRight ? _arrowMoveDistance : -_arrowMoveDistance;
        _arrowTween = arrow.DOAnchorPosX(direction, _arrowDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);
    }

    private void UpdatePosition(RectTransform slotRect, bool showRight)
    {
        Vector3 slotPos = slotRect.position;
        float slotHalfWidth = slotRect.rect.width * slotRect.lossyScale.x * 0.5f;
        float tooltipHalfWidth = _rectTransform.rect.width * _rectTransform.lossyScale.x * 0.5f;

        float xOffset = slotHalfWidth + tooltipHalfWidth + _offset;

        if (!showRight)
            xOffset = -xOffset;

        _rectTransform.position = new Vector3(slotPos.x + xOffset, slotPos.y, slotPos.z);
    }
}