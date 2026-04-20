using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerInfoPanelAbility : PlayerAbility
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;
    private bool _isOpen;

    private void Update()
    {
        if (_owner == null || !_owner.IsMine) return;

        if (Input.GetKey(_toggleKey))
        {
            if (_isOpen) return;
            OpenInfoPanel();
        }
        else if (_isOpen)
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
        if (UIController.Instance != null)
        {
            UIController.Instance.OpenAsync<UI_VillageInfoPopup>
            (
                new UILifecycleActions<UI_VillageInfoPopup>
                {
                    OnOpen = ui => ui.SetPlayerController(_owner)
                }
            ).Forget();
        }
        _isOpen = true;
    }

    private void CloseInfoPanel()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.CloseAsync<UI_VillageInfoPopup>().Forget();
        }
        _isOpen = false;
    }
}