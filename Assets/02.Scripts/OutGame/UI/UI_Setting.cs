using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_Setting : UIBase
{
    private UI_PopupDoTween _popupDoTween;

    [Header("Buttons")]
    [SerializeField] private Button _closeButton;

    [Header("Tabs")]
    [SerializeField] private Button[] _tabButtons;
    [SerializeField] private UI_SettingPanelBase[] _panels;

    [Header("Tab Lift Animation")]
    [SerializeField] private float _liftOffset = 20f;
    [SerializeField] private float _liftDuration = 0.2f;
    [SerializeField] private Ease _liftEase = Ease.OutCubic;

    private Vector2[] _tabBasePositions;
    private int _currentIndex = -1;
    private UnityAction[] _tabHandlers;

    private void Awake()
    {
        _popupDoTween = GetComponent<UI_PopupDoTween>();
        CacheTabBasePositions();
        DeactivateAllPanels();
    }

    // 인스펙터에서 주입된 탭 버튼들의 기준 위치를 캐싱한다. (Tab 애니메이션에 사용)
    private void CacheTabBasePositions()
    {
        if (_tabButtons == null) return;

        _tabBasePositions = new Vector2[_tabButtons.Length];
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            if (_tabButtons[i] == null) continue;

            RectTransform rt = (RectTransform)_tabButtons[i].transform;
            _tabBasePositions[i] = rt.anchoredPosition;
        }
    }

    private void DeactivateAllPanels()
    {
        if (_panels == null) return;
        foreach (UI_SettingPanelBase panel in _panels)
        {
            if (panel == null) continue; 
            panel.gameObject.SetActive(false);
        }
    }

    protected override void OnOpen()
    {
        BindTabButtons();

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        // 최초 오픈에서는 첫 번째 탭을 선택해 초기 리프트를 재생한다.
        // 재오픈이면 이전에 선택됐던 탭/패널 상태가 그대로 남아 있으므로 현재 패널의 리스너만 다시 등록한다.
        if (_currentIndex < 0 || _currentIndex >= _panels.Length || _panels[_currentIndex] == null)
        {
            SelectTab(0);
        }
        else
        {
            PlayTabLiftTransition(-1, _currentIndex);
            _panels[_currentIndex].OnShow();
        }
    }

    protected override void OnClose()
    {
        UnbindTabButtons();

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        if (_currentIndex >= 0 && _currentIndex < _panels.Length && _panels[_currentIndex] != null)
        {
            _panels[_currentIndex].OnHide();
        }

        if (SettingManager.Instance != null)
        {
            SettingManager.Instance.Save();
        }
    }

    protected override UniTask OnOpenAnimation()
    {
        if (_popupDoTween != null) _ = _popupDoTween.PlayOpenAsync();
        return base.OnOpenAnimation();
    }

    protected override UniTask OnCloseAnimation()
    {
        if (_popupDoTween != null) _ = _popupDoTween.PlayCloseAsync();
        return base.OnCloseAnimation();
    }

    #region Tab Binding

    private void BindTabButtons()
    {
        if (_tabButtons == null) return;

        _tabHandlers = new UnityAction[_tabButtons.Length];
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            if (_tabButtons[i] == null) continue;

            int capturedIndex = i;
            UnityAction handler = () => SelectTab(capturedIndex);
            _tabHandlers[i] = handler;
            _tabButtons[i].onClick.AddListener(handler);
        }
    }

    private void UnbindTabButtons()
    {
        if (_tabButtons == null || _tabHandlers == null) return;

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            if (_tabButtons[i] != null && _tabHandlers[i] != null)
            {
                _tabButtons[i].onClick.RemoveListener(_tabHandlers[i]);
            }
        }
        _tabHandlers = null;
    }

    #endregion

    #region Tab Selection

    /// <summary>
    /// 지정된 인덱스의 탭으로 전환한다. 이전 패널은 OnHide + 비활성화, 새 패널은 활성화 + OnShow.
    /// 탭 버튼 자체에는 이전 탭이 내려오고 새 탭이 올라가는 Y축 리프트 애니메이션이 재생된다.
    /// </summary>
    public void SelectTab(int index)
    {
        if (_panels == null || _tabButtons == null) return;
        if (index < 0 || index >= _panels.Length) return;
        if (index == _currentIndex) return;
        if (_panels[index] == null) return;

        int previousIndex = _currentIndex;

        // 이전 패널 정리. OnHide로 리스너 해제 후 GameObject를 비활성화해 다른 탭이 겹치지 않도록 한다.
        if (previousIndex >= 0 && previousIndex < _panels.Length && _panels[previousIndex] != null)
        {
            _panels[previousIndex].OnHide();
            _panels[previousIndex].gameObject.SetActive(false);
        }

        _currentIndex = index;

        UI_SettingPanelBase panel = _panels[index];
        panel.gameObject.SetActive(true);
        panel.OnShow();

        PlayTabLiftTransition(previousIndex, index);
    }

    // 이전에 올라가 있던 탭은 기준 위치로 내리고, 새로 선택된 탭은 _liftOffset만큼 올리는 이중 애니메이션.
    // previousIndex가 -1이면 내릴 대상이 없으므로 올리는 쪽만 재생한다.
    private void PlayTabLiftTransition(int previousIndex, int newIndex)
    {
        if (previousIndex >= 0 && previousIndex < _tabButtons.Length && _tabButtons[previousIndex] != null)
        {
            AnimateTabOffset(previousIndex, 0f);
        }

        AnimateTabOffset(newIndex, _liftOffset);
    }

    // 기준 위치에 offsetY만큼 더한 좌표로 탭 버튼을 이동시킨다. duration이 0 이하이면 즉시 스냅한다.
    private void AnimateTabOffset(int tabIndex, float offsetY)
    {
        if (_tabBasePositions == null || tabIndex >= _tabBasePositions.Length) return;
        if (_tabButtons[tabIndex] == null) return;

        RectTransform rt = (RectTransform)_tabButtons[tabIndex].transform;
        Vector2 basePos = _tabBasePositions[tabIndex];
        float targetY = basePos.y + offsetY;

        rt.DOKill();

        if (_liftDuration <= 0f)
        {
            rt.anchoredPosition = new Vector2(basePos.x, targetY);
            return;
        }

        rt.DOAnchorPosY(targetY, _liftDuration).SetEase(_liftEase);
    }

    #endregion

    // 닫기 버튼. UIController가 있으면 스택에서 제거하고, 없으면 자체 CloseAsync로 폴백한다.
    private void OnCloseClicked()
    {
        if (UIController.Instance == null)
        {
            CloseAsync().Forget();
            return;
        }
        UIController.Instance.CloseAsync<UI_Setting>().Forget();
    }

    // 외부 UnityEvent에서 이 UI를 직접 여는 용도. UIController를 거치지 않고 UIBase.OpenAsync를 호출한다.
    public void OpenSettingPanel()
    {
        OpenAsync().Forget();
    }
}
