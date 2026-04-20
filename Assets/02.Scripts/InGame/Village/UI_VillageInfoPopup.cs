using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UI_VillageInfoPopup : UIBase
{
    [Header("Slide Animation")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _minimumSlideDistance = 300f;
    [SerializeField] private float _hiddenPadding = 40f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    private UI_InfoSectionBase[] _sections;
    private PlayerController _playerController;
    private Vector2 _openPosition;
    private Tween _slideTween;
    private bool _hasOpenPosition;
    public bool OnAnimationComplete => _slideTween == null;
    
    
    private void Awake()
    {
        CachePanel();
        _sections = GetComponentsInChildren<UI_InfoSectionBase>(includeInactive: true);
    }

    private void OnDestroy()
    {
        KillSlideTween();
    }

    protected override void OnOpen()
    {
        if (_sections == null) return;

        foreach (UI_InfoSectionBase section in _sections)
        {
            if (section == null) continue;
            section.Subscribe(_playerController);
            section.Refresh();
        }
    }

    protected override void OnClose()
    {
        if (_sections == null) return;

        foreach (UI_InfoSectionBase section in _sections)
        {
            if (section == null) continue;
            section.Unsubscribe();
        }
    }

    protected override async UniTask OnOpenAnimation()
    {
        if (!CachePanel()) return;

        KillSlideTween();
        Canvas.ForceUpdateCanvases();

        _panel.anchoredPosition = GetHiddenPosition();
        Tween tween = _panel.DOAnchorPos(_openPosition, _slideDuration)
            .SetEase(_openEase)
            .SetLink(gameObject);
        _slideTween = tween;

        await tween.AsyncWaitForCompletion();

        if (_slideTween == tween)
        {
            _slideTween = null;
        }
    }

    protected override async UniTask OnCloseAnimation()
    {
        if (!CachePanel()) return;

        KillSlideTween();
        Canvas.ForceUpdateCanvases();

        Tween tween = _panel.DOAnchorPos(GetHiddenPosition(), _slideDuration)
            .SetEase(_closeEase)
            .SetLink(gameObject);
        _slideTween = tween;

        await tween.AsyncWaitForCompletion();

        if (_slideTween == tween)
        {
            _panel.anchoredPosition = _openPosition;
            _slideTween = null;
        }
    }

    public void SetPlayerController(PlayerController playerController)
    {
        _playerController = playerController;
    }

    private bool CachePanel()
    {
        if (_panel == null)
        {
            _panel = GetComponent<RectTransform>();
        }

        if (_panel == null) return false;

        if (!_hasOpenPosition)
        {
            _openPosition = _panel.anchoredPosition;
            _hasOpenPosition = true;
        }

        return true;
    }

    private Vector2 GetHiddenPosition()
    {
        float slideDistance = Mathf.Max(_minimumSlideDistance, _panel.rect.width + _hiddenPadding);
        return _openPosition + Vector2.left * slideDistance;
    }

    private void KillSlideTween()
    {
        if (_slideTween != null && _slideTween.IsActive())
        {
            _slideTween.Kill();
        }

        _slideTween = null;
    }
}
