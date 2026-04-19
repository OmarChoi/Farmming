public class UI_InfoPannel : UIBase
{
    private UI_InfoSectionBase[] _sections;
    private PlayerController _playerController;
    private void Awake()
    {
        _sections = GetComponentsInChildren<UI_InfoSectionBase>(includeInactive: true);
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

    public void SetPlayerController(PlayerController playerController)
    {
        _playerController = playerController;
    }
}
