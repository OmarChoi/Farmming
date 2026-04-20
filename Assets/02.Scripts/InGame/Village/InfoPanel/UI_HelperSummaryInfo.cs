using System.Text;
using UnityEngine;

public class UI_HelperSummaryInfo : UI_InfoSectionBase
{
    private readonly StringBuilder _builder = new StringBuilder(384);
    private PlayerHelperInventoryAbility _ability;
    private HelperController _mainHelper;
    private HelperController _lightHelper;
    private float _nextRefreshTime;

    public override void Subscribe(PlayerController playerController)
    {
        _ability = playerController.GetAbility<PlayerHelperInventoryAbility>();
        Refresh();
    }
    
    public override void Unsubscribe()
    {
        
    }
    
    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime) return;

        _nextRefreshTime = Time.unscaledTime + 0.25f;
        Refresh();
    }
    
    public override void Refresh()
    {
    }

}
