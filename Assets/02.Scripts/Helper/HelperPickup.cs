using UnityEngine;

public class HelperPickup : MonoBehaviour
{
    public bool IsEquipped { get; private set; }

    public void Equip()
    {
        IsEquipped = true;
        Debug.Log("곡룡 장착");
    }

    public void UnEquip()
    {
        IsEquipped = false;
        Debug.Log("곡룡 해제");
    }
}
