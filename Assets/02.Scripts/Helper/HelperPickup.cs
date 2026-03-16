using UnityEngine;

public class HelperPickup : MonoBehaviour
{
    [SerializeField] private Transform _helperSlot;
    public bool IsEquipped { get; private set; }

    public void Equip()
    {
        IsEquipped = true;

        if(_helperSlot != null)
        {
            transform.SetParent(_helperSlot);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        gameObject.SetActive(true);
        Debug.Log("곡룡 장착");
    }

    public void UnEquip()
    {
        IsEquipped = false;

        transform.SetParent(null);
        gameObject.SetActive(false);
        Debug.Log("곡룡 해제");
    }
}
