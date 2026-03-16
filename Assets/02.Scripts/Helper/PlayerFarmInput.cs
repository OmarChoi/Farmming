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

            TerrainCell cell = GetTargetCell();
            if (cell == null) return;

            // FarmTile이 이미 활성화되어 있으면 FarmTile과 상호작용
            FarmTile farmTile = cell.GetComponentInChildren<FarmTile>();
            if (farmTile != null && farmTile.gameObject.activeSelf)
            {
                farmTile.Interact(_testSeed);
            }
            else
            {
                // 아직 경작지가 아니면 흙→경작지 전환
                cell.TryConvertToFarm();
            }
        }
    }

    private TerrainCell GetTargetCell()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        Debug.DrawRay(transform.position, transform.forward * _detectRange, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, _detectRange))
        {
            return hit.collider.GetComponentInParent<TerrainCell>();
        }
        return null;
    }
}
