using UnityEngine;

public class PlayerFarmInput : MonoBehaviour
{
    [SerializeField] private HelperPickup _equippedHelper;
    [SerializeField] private float _detectRange = 2f;

    [Header("테스트용 씨앗")]
    [SerializeField] private SeedConfig _testSeed;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            if(_equippedHelper!=null && !_equippedHelper.IsEquipped)
            {
                _equippedHelper.Equip();
            }
            else if (_equippedHelper != null && _equippedHelper.IsEquipped)
            {
                _equippedHelper.UnEquip();
            }
        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            if(_equippedHelper == null || !_equippedHelper.IsEquipped)
            {
                Debug.Log("곡룡 들고 있지 않음");
                return;
            }

            FarmTile tile = GetTargetTile();
            if (tile == null)
            {
                return;
            }

            tile.Interact(_testSeed);

        }
    }

    private FarmTile GetTargetTile()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        // 정면을 알기위해 빨간 선
        Debug.DrawRay(transform.position, transform.forward * _detectRange, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, _detectRange))
        {
            return hit.collider.GetComponent<FarmTile>();
        }
        return null;
    }
}
