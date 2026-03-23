using UnityEngine;

public class SowAnimationEventReceiver : MonoBehaviour
{
    private SowActionAbility _sowAbility;

    private void Awake()
    {
        _sowAbility = transform.root.GetComponentInChildren<SowActionAbility>(true);
        Debug.Log($"SowAbility 찾음 : {_sowAbility != null}");
    }

    public void SowOpen()
    {
        _sowAbility?.SowOpen();
        Debug.Log("이벤트발동");
    }

    public void SowClose()
    {
        _sowAbility?.SowClose();
    }
}
