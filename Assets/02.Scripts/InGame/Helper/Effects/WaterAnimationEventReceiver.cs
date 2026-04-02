using UnityEngine;

public class WaterAnimationEventReceiver : MonoBehaviour
{
    private WaterActionAbility _waterAbility;

    private void Awake()
    {
        _waterAbility = transform.root.GetComponentInChildren<WaterActionAbility>(true);

        Debug.Log($"WaterAbility 찾음 : {_waterAbility != null}");
    }

    public void WaterOpen()
    {
        _waterAbility?.WaterOpen();
    }
}
