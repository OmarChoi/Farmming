using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HelperInventory : MonoBehaviour
{
    [Header("슬롯 아이콘")]
    [SerializeField] private Image _leftIcon;
    [SerializeField] private Image _centerIcon;
    [SerializeField] private Image _rightIcon;

    [Header("슬롯 배경")]
    [SerializeField] private GameObject _iconBgGroup;

    [Header("장식 아이콘")]
    [SerializeField] private Image _leftDecor;
    [SerializeField] private Image _centerDecor;
    [SerializeField] private Image _rightDecor;
    [SerializeField] private Color _decorGrayColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private float _decorScale = 1.2f;

    [Header("확장 시 표시")]
    [SerializeField] private GameObject _showOnExpand1;
    [SerializeField] private GameObject _showOnExpand2;
    [SerializeField] private TMP_Text _helperNameText;

    [Header("선택 강조")]
    [SerializeField] private GameObject _summonedIndicator;

    [Header("표시 애니메이션")]
    [SerializeField] private Image _background;
    [SerializeField] private float _smallScale = 0.5f;
    [SerializeField] private float _sideScale = 1f;
    [SerializeField] private float _centerScale = 1.7f;
    [SerializeField] private float _spreadOffset = 50f;
    [SerializeField] private float _offScreenOffset = 100f;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private float _hideDelay = 3f;

    [SerializeField] private UI_HelperActionInfo _actionInfoPanel;

    private PlayerHelperInventoryAbility _ability;
    private float _hideTimer;
    private bool _isShowing;

    private Image[] _icons;
    private Image[] _sideDecors;
    private float[] _originX;
    private float[] _slotX;
    private float[] _slotScale;
    private float[] _hiddenScale;

    private void Awake()
    {
        _icons = new[] { _leftIcon, _centerIcon, _rightIcon };
        _sideDecors = new[] { _leftDecor, _rightDecor };

        _originX = new float[3];
        for (int i = 0; i < 3; i++)
            _originX[i] = _icons[i].transform.localPosition.x;

        _slotX = new[]
        {
            _originX[0] - _spreadOffset,
            _originX[1],
            _originX[2] + _spreadOffset
        };

        _slotScale = new[] { _sideScale, _centerScale, _sideScale };
        _hiddenScale = new[] { _smallScale, _smallScale * _centerScale / _sideScale, _smallScale };

        PlayerHelperInventoryAbility.OnLocalPlayerReady += Bind;
        SetHiddenImmediate();
    }

    private void OnDestroy()
    {
        PlayerHelperInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();

        foreach (var icon in _icons)
            icon.transform.DOKill();

        foreach (var decor in _sideDecors)
            if (decor != null) { decor.DOKill(); decor.transform.DOKill(); }

        if (_centerDecor != null) _centerDecor.DOKill();
        if (_background != null) _background.DOKill();
    }

    private void Update()
    {
        if (!_isShowing) return;

        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f)
            Hide();
    }

    private void Bind(PlayerHelperInventoryAbility ability)
    {
        Unbind();

        _ability = ability;
        _ability.OnSelectionChanged += OnInput;
        _ability.OnSummonChanged += OnSummonInput;
        Refresh();
    }

    private void Unbind()
    {
        if (_ability == null) return;

        _ability.OnSelectionChanged -= OnInput;
        _ability.OnSummonChanged -= OnSummonInput;
        _ability = null;
    }

    private void OnInput(int direction)
    {
        bool wasShowing = _isShowing;
        Show();

        if (wasShowing && direction != 0)
            Slide(direction);
        else
            Refresh();
    }

    private void OnSummonInput(int summonedIndex)
    {
        Show();
        Refresh();
    }

    private void Slide(int direction)
    {
        for (int i = 0; i < 3; i++)
            _icons[i].transform.DOKill();

        int outIdx = direction > 0 ? 0 : 2;
        int enterSlot = direction > 0 ? 2 : 0;
        HelperDataSO newData = direction > 0 ? _ability.RightData : _ability.LeftData;

        Image outIcon = _icons[outIdx];
        SetSlot(outIcon, newData);

        var pos = outIcon.transform.localPosition;
        pos.x = _slotX[enterSlot] + _offScreenOffset * direction;
        outIcon.transform.localPosition = pos;
        outIcon.transform.localScale = Vector3.one * _sideScale;

        int[] fromIdx = direction > 0 ? new[] { 1, 2 } : new[] { 0, 1 };
        int[] toSlot = direction > 0 ? new[] { 0, 1 } : new[] { 1, 2 };
        for (int i = 0; i < 2; i++)
        {
            _icons[fromIdx[i]].transform.DOLocalMoveX(_slotX[toSlot[i]], _animDuration).SetEase(Ease.OutCubic);
            _icons[fromIdx[i]].transform.DOScale(_slotScale[toSlot[i]], _animDuration).SetEase(Ease.OutCubic);
        }

        outIcon.transform.DOLocalMoveX(_slotX[enterSlot], _animDuration).SetEase(Ease.OutCubic);
        outIcon.transform.DOScale(_slotScale[enterSlot], _animDuration).SetEase(Ease.OutCubic);

        _icons = direction > 0
            ? new[] { _icons[1], _icons[2], outIcon }
            : new[] { outIcon, _icons[0], _icons[1] };

        RefreshHelperName();
        RefreshSummonedIndicator();
    }

    private void Show()
    {
        _hideTimer = _hideDelay;

        if (_isShowing) return;
        _isShowing = true;

        for (int i = 0; i < 3; i++)
        {
            _icons[i].transform.DOKill();
            _icons[i].transform.DOScale(_slotScale[i], _animDuration).SetEase(Ease.OutBack);
            _icons[i].transform.DOLocalMoveX(_slotX[i], _animDuration).SetEase(Ease.OutBack);
        }

        SetIconBgs(false);
        SetExpandObjects(true);
        AnimateDecor(true);

        if (_background != null)
        {
            _background.DOKill();
            _background.DOFade(1f, _animDuration);
        }
    }

    private void Hide()
    {
        _isShowing = false;

        for (int i = 0; i < 3; i++)
        {
            _icons[i].transform.DOKill();
            _icons[i].transform.DOScale(_hiddenScale[i], _animDuration).SetEase(Ease.InBack);
            _icons[i].transform.DOLocalMoveX(_originX[i], _animDuration).SetEase(Ease.InBack);
        }

        SetIconBgs(true);
        SetExpandObjects(false);
        AnimateDecor(false);

        if (_background != null)
        {
            _background.DOKill();
            _background.DOFade(0f, _animDuration);
        }
    }

    private void SetHiddenImmediate()
    {
        for (int i = 0; i < 3; i++)
        {
            _icons[i].transform.localScale = Vector3.one * _hiddenScale[i];
            var pos = _icons[i].transform.localPosition;
            pos.x = _originX[i];
            _icons[i].transform.localPosition = pos;
        }

        SetExpandObjects(false);
        SetIconBgs(true);
        ResetDecorImmediate();

        if (_background != null)
        {
            Color c = _background.color;
            c.a = 0f;
            _background.color = c;
        }
    }

    private void SetIconBgs(bool active)
    {
        if (_iconBgGroup != null) _iconBgGroup.SetActive(active);
    }

    private void SetExpandObjects(bool active)
    {
        if (_showOnExpand1 != null) _showOnExpand1.SetActive(active);
        if (_showOnExpand2 != null) _showOnExpand2.SetActive(active);
        RefreshHelperName();
    }

    private void RefreshHelperName()
    {
        if (_helperNameText == null) return;
        var data = _ability != null ? _ability.CenterData : null;
        _helperNameText.text = data != null ? data.HelperName : "";

        if (_ability != null)
        {
            var dataToShow = _isShowing ? data : _ability.SummonedData;
            EHelperGrade grade = _ability.GetHelperGrade(dataToShow);
            _actionInfoPanel.Show(dataToShow, grade);
        }
    }

    private void RefreshSummonedIndicator()
    {
        if (_summonedIndicator != null)
            _summonedIndicator.SetActive(_ability.IsCurrentIndexSummoned);
    }

    private void AnimateDecor(bool expanding)
    {
        foreach (var decor in _sideDecors)
        {
            if (decor == null) continue;
            decor.DOKill();
            decor.transform.DOKill();
            decor.DOColor(expanding ? _decorGrayColor : Color.white, _animDuration);
            decor.transform.DOScale(expanding ? _decorScale : 1f, _animDuration)
                .SetEase(expanding ? Ease.OutBack : Ease.InBack);
        }

        if (_centerDecor != null)
        {
            _centerDecor.DOKill();
            _centerDecor.DOFade(expanding ? 0f : 1f, _animDuration);
        }
    }

    private void ResetDecorImmediate()
    {
        foreach (var decor in _sideDecors)
        {
            if (decor == null) continue;
            decor.color = Color.white;
            decor.transform.localScale = Vector3.one;
        }

        if (_centerDecor != null)
            _centerDecor.color = Color.white;
    }

    private void Refresh()
    {
        if (_ability.Count == 0)
        {
            for (int i = 0; i < 3; i++)
                SetSlot(_icons[i], null);
            if (_summonedIndicator != null)
                _summonedIndicator.SetActive(false);
            return;
        }

        SetSlot(_icons[0], _ability.LeftData);
        SetSlot(_icons[1], _ability.CenterData);
        SetSlot(_icons[2], _ability.RightData);

        RefreshSummonedIndicator();
    }

    private void SetSlot(Image icon, HelperDataSO data)
    {
        if (data == null || data.HelperIcon == null)
        {
            icon.enabled = false;
            return;
        }

        icon.enabled = true;
        icon.sprite = data.HelperIcon;
    }
}