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
    [SerializeField] private GameObject _rightClickGroup;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private float _visibleAlpha = 1f;
    [SerializeField] private float _invisibleAlpha = 0f;

    private void Awake()
    {
        if(_canvasGroup != null)
        {
            _canvasGroup.alpha = _invisibleAlpha;
            _canvasGroup.interactable = false;
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

    public void Show(HelperDataSO data, EHelperGrade currentGrade = EHelperGrade.Normal)
    {
        if(data == null)
        {
            Hide();
            return;
        }

        bool secondaryUnlocked = currentGrade >= data.SecondaryUnlockGrade;

        SetIcon(_leftClickIcon, data.LeftClickIcon);
        SetExplanation(_leftClickExplanation, data.LeftClickExplanation);

        if (_rightClickGroup != null)
        {
            _rightClickGroup.SetActive(secondaryUnlocked);
        }
        SetIcon(_rightClickIcon, secondaryUnlocked ? data.RightClickIcon : null);
        SetExplanation(_rightClickExplanation, secondaryUnlocked ? data.RightClickExplanation : null);

        if ( _canvasGroup != null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(_visibleAlpha, _animDuration).SetEase(Ease.OutCubic);
        }
    }

    private void SetExplanation(TextMeshProUGUI textElement, string explanation)
    {
        if (textElement == null)
        {
            return;
        }
        bool hasText = !string.IsNullOrEmpty(explanation);
        textElement.gameObject.SetActive(hasText);
        if (hasText)
        {
            textElement.text = explanation;
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
        if (image == null)
        {
            return;
        }
        bool hasSprite = sprite != null;
        image.gameObject.SetActive(hasSprite);
        if (hasSprite)
        {
            image.sprite = sprite;
        }
    }
}
