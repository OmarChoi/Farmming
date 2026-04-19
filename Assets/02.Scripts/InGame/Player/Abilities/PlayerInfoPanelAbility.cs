using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerInfoPanelAbility : PlayerAbility
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;
    private bool _isOpen;

    private void Update()
    {
        if (_owner == null || !_owner.IsMine) return;

        if (Input.GetKeyDown(_toggleKey) && !_isOpen)
        {
            OpenInfoPanel();
        }
        else if ((Input.GetKeyUp(_toggleKey) || !Input.GetKey(_toggleKey)) && _isOpen)
        {
            CloseInfoPanel();
        }
    }

    private void OnDisable()
    {
        if (_isOpen) CloseInfoPanel();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _isOpen) CloseInfoPanel();
    }

    private void OpenInfoPanel()
    {
        _isOpen = true;
        _owner.EnterUIMode();
        if (UIController.Instance != null)
        {
            UIController.Instance.OpenAsync
            (
                new UILifecycleActions<UI_InfoPannel>
                {
                    OnOpen = ui => ui.SetPlayerController(_owner);
                }
            ).Forget();
        }
    }

    private void CloseInfoPanel()
    {
        _isOpen = false;
        _owner.ExitUIMode();
        if (UIController.Instance != null)
        {
            UIController.Instance.CloseAsync<UI_InfoPannel>().Forget();
        }
    }
}