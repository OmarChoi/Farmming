using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    [SerializeField] private UIConfig _config;

    public UIConfig Config => _config;
    public bool IsOpen { get; private set; }

    public async UniTask OpenAsync()
    {
        if (IsOpen) return;

        IsOpen = true;
        gameObject.SetActive(true);
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
    }

    protected virtual void OnOpen() { }

    protected virtual void OnClose() { }

    protected virtual UniTask OnOpenAnimation() => UniTask.CompletedTask;

    protected virtual UniTask OnCloseAnimation() => UniTask.CompletedTask;
}