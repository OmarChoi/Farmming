using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class UI_InfoPage : MonoBehaviour
{
    [SerializeField] private GameObject _pageRoot;
    [SerializeField] private UI_InfoPageView _pageView;

    [Header("버튼")]
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _completeButton;

    [Header("PopupDoTween")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    private readonly List<InfoPageData> _pages = new();
    private int _currentPageIndex = 0;

    public Action OnCompleteRequested;
    public Action OnCloseRequested;

    private void Awake()
    {
        if (_prevButton != null)
        {
            _prevButton.onClick.AddListener(ShowPrevPage);
        }
        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(ShowNextPage);
        }
        if (_completeButton != null)
        {
            _completeButton.onClick.AddListener(OnClickComplete);
        }
    }

    private void Start()
    {
        if (_pageRoot != null)
        {
            _pageRoot.SetActive(false);
        }
    }

    public async UniTask OpenAsync(List<InfoPageData> pages)
    {
        _pages.Clear();

        if (pages != null)
        {
            _pages.AddRange(pages);
        }

        _currentPageIndex = 0;

        if (_pageRoot != null)
        {
            _pageRoot.SetActive(true);
        }

        RefreshCurrentPage();

        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
    }

    public async UniTask CloseAsync()
    {
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayCloseAsync();
        }
        else if (_pageRoot != null)
        {
            _pageRoot.SetActive(false);
        }

        if (_pageView != null)
        {
            _pageView.Refresh(null);
        }

        _pages.Clear();
        _currentPageIndex = 0;
    }

    private void RefreshCurrentPage()
    {
        if (_pages.Count == 0)
        {
            _pageView.Refresh(null);
            UpdateButtons();
            return;
        }

        _currentPageIndex = Mathf.Clamp(_currentPageIndex, 0, _pages.Count - 1);
        _pageView.Refresh(_pages[_currentPageIndex]);

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasPages = _pages.Count > 0;
        bool isFirst = _currentPageIndex == 0;
        bool isLast = hasPages && _currentPageIndex == _pages.Count - 1;

        if (_prevButton != null)
        {
            _prevButton.gameObject.SetActive(hasPages && !isFirst);
        }
        if (_nextButton != null)
        {
            _nextButton.gameObject.SetActive(hasPages && !isLast);
        }
        if (_completeButton != null)
        {
            _completeButton.gameObject.SetActive(hasPages && isLast);
        }
    }

    private void ShowPrevPage()
    {
        if (_pages.Count == 0) return;
        if (_currentPageIndex <= 0) return;

        _currentPageIndex--;
        RefreshCurrentPage();
    }

    private void ShowNextPage()
    {
        if (_pages.Count == 0) return;
        if (_currentPageIndex >= _pages.Count - 1) return;

        _currentPageIndex++;
        RefreshCurrentPage();
    }

    private void OnClickComplete()
    {
        OnCompleteRequested?.Invoke();
    }
}
