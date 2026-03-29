using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HelperActionInfo : MonoBehaviour
{
    [SerializeField] private Image _leftClickIcon;
    [SerializeField] private TextMeshProUGUI _leftClickExplanation;

    [SerializeField] private Image _rightClickIcon;
    [SerializeField] private TextMeshProUGUI _rightClickExplanation;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private float _visibleAlpha = 1f;
    [SerializeField] private float _invisibleAlpha = 0f;

    private void Awake()
    {
        if(_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable =false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if(_canvasGroup != null)
        {
            _canvasGroup.DOKill();
        }
    }

    public void Show(HelperDataSO data)
    {
        if(data == null)
        {
            Hide();
            return;
        }

        SetIcon(_leftClickIcon, data.LeftClickIcon);
        SetIcon(_rightClickIcon, data.RightClickIcon);

        if(_leftClickExplanation != null)
        {
            _leftClickExplanation.text = data.LeftClickExplanation;
        }

        if(_rightClickExplanation != null)
        {
            _rightClickExplanation.text = data.RightClickExplanation;
        }

        if( _canvasGroup != null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(_visibleAlpha, _animDuration).SetEase(Ease.OutCubic);
        }
    }

    public void Hide()
    {
        if(_canvasGroup != null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.DOFade(_invisibleAlpha, _animDuration).SetEase(Ease.InCubic);
        }
    }

    private void SetIcon(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
