using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class InfoPageController : MonoBehaviour
{
    [SerializeField] private UI_InfoPage _uiInfoPage;

    public static event Action<InfoPageController> OnInfoPageReady;

    private void Start()
    {
        OnInfoPageReady?.Invoke(this);
    }

    public async UniTask ShowAsync(InfoPageSetSO pageSet, Action onCompleted = null)
    {
        if (pageSet == null || !pageSet.HasPages())
        {
            onCompleted?.Invoke();
            return;
        }

        List<InfoPageData> pageList = new(pageSet.Pages);

        void Cleanup()
        {
            _uiInfoPage.OnCompleteRequested -= HandleComplete;
            _uiInfoPage.OnCloseRequested -= HandleClose;
        }
        void HandleComplete()
        {
            Cleanup();
            CloseInternal(onCompleted).Forget();
        }
        void HandleClose()
        {
            Cleanup();
            CloseInternal(onCompleted).Forget();
        }

        Cleanup();
        _uiInfoPage.OnCompleteRequested += HandleComplete;
        _uiInfoPage.OnCloseRequested += HandleClose;

        await _uiInfoPage.OpenAsync(pageList);
    }

    private async UniTaskVoid CloseInternal(Action onCompleted)
    {
        if (_uiInfoPage != null)
        {
            await _uiInfoPage.CloseAsync();
        }

        onCompleted?.Invoke();
    }
}
