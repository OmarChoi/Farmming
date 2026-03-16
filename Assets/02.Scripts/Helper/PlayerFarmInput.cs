using UnityEngine;

public class PlayerFarmInput : MonoBehaviour
{
    [SerializeField] private HelperPickup _equippedHelper;
    [SerializeField] private PlayerTerrainAbility _terrainAbility;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_equippedHelper != null && !_equippedHelper.IsEquipped)
                _equippedHelper.Equip();
            else if (_equippedHelper != null && _equippedHelper.IsEquipped)
                _equippedHelper.UnEquip();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_equippedHelper == null || !_equippedHelper.IsEquipped)
            {
                Debug.Log("곡룡 들고 있지 않음");
                return;
            }

            TerrainCell cell = _terrainAbility.GetFrontCell();
            if (cell == null)
            {
                Debug.Log("앞에 셀 없음");
                return;
            }

            var action = _equippedHelper.GetComponent<IHelperAction>();
            if (action != null)
                action.Interact(cell);
            else
                Debug.Log("이 곡룡은 행동이 없음");
        }
    }
}