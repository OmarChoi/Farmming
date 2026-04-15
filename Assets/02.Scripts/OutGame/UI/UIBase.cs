using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Open 직전(데이터 주입)과 Close 직후(후처리) 시점에 끼울 훅을 한 번에 묶는다.
// UIController.OpenAsync가 이 구조체를 받아 해당 UI 인스턴스를 캡처해 UIBase에 등록한다.
public struct UILifecycleActions<T> where T : UIBase
{
    public Action<T> OnOpen;
    public Action<T> OnClose;
}

public abstract class UIBase : MonoBehaviour
{
    [SerializeField] private UIConfig _config;

    public UIConfig Config => _config;
    public bool IsOpen { get; private set; }

    // 한 번의 Open 사이클 동안만 유효한 1회성 콜백 슬롯.
    // CloseAsync가 OnClose를 발화한 뒤 두 슬롯을 모두 비워 다음 사이클로의 누수를 차단한다.
    // UIController가 유일한 등록 경로 (SetLifecycleActions).
    private Action _onOpenAction;
    private Action _onCloseAction;

    public async UniTask OpenAsync()
    {
        if (IsOpen) return;

        IsOpen = true;
        gameObject.SetActive(true);

        // 외부 주입 → 서브클래스 훅 → 애니메이션 순. OnOpen()이 주입된 데이터를 전제할 수 있도록 먼저 호출.
        _onOpenAction?.Invoke();
        OnOpen();
        await OnOpenAnimation();
    }

    public async UniTask CloseAsync()
    {
        if (!IsOpen) return;

        IsOpen = false;
        await OnCloseAnimation();
        OnClose();
        gameObject.SetActive(false);

        // 콜백 캡처 → 슬롯 클리어 → 발화 순서로 중복 호출·재등록 누수를 차단.
        Action cb = _onCloseAction;
        _onOpenAction = null;
        _onCloseAction = null;
        cb?.Invoke();
    }

    // UIController 전용 훅. 같은 UI가 재Open 되기 전까지 단일 Open/Close 사이클에만 유효.
    internal void SetLifecycleActions(Action onOpen, Action onClose)
    {
        _onOpenAction = onOpen;
        _onCloseAction = onClose;
    }

    protected virtual void OnOpen() { }

    protected virtual void OnClose() { }

    protected virtual UniTask OnOpenAnimation() => UniTask.CompletedTask;

    protected virtual UniTask OnCloseAnimation() => UniTask.CompletedTask;
}