using DG.Tweening;
using UnityEngine;

public class TitleLogoAnimation : MonoBehaviour
{
    [Header("Rotation Sway")]
    [SerializeField] private float _rotationAngle = 3f;
    [SerializeField] private float _rotationDuration = 2.5f;

    [Header("Vertical Float")]
    [SerializeField] private float _floatDistance = 8f;
    [SerializeField] private float _floatDuration = 3f;

    private RectTransform _rect;
    private Tween _rotationTween;
    private Tween _floatTween;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Vector3 startPos = _rect.anchoredPosition;

        _rotationTween = _rect
            .DOLocalRotate(new Vector3(0f, 0f, _rotationAngle), _rotationDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(new Vector3(0f, 0f, -_rotationAngle));

        _floatTween = _rect
            .DOAnchorPosY(startPos.y + _floatDistance, _floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(new Vector2(startPos.x, startPos.y - _floatDistance));
    }

    private void OnDisable()
    {
        _rotationTween?.Kill();
        _floatTween?.Kill();
        _rotationTween = null;
        _floatTween = null;
    }
}