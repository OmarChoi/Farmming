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

            EFarmTileStateType current = tile.StateMachine.CurrentStateType;
            CropGrowth cropGrowth = tile.GetComponent<CropGrowth>();

            if (current == EFarmTileStateType.Ground)
            {
                tile.StateMachine.FarmTransition(EFarmTileStateType.FarmDry);
            }
            else if (current == EFarmTileStateType.FarmDry && !tile.HasSeed)
            {
                tile.PlantSeed(_testSeed);
            }
            else if (current == EFarmTileStateType.FarmDry && tile.HasSeed)
            {
                tile.StateMachine.FarmTransition(EFarmTileStateType.FarmWet);

                if (!cropGrowth.HasStarted)
                {
                    tile.GetComponent<CropGrowth>().StartGrowth(tile.PlantedSeed);
                }
                else
                {
                    Debug.Log("물 줌(성장 계속)");
                }
            }
            else if(current == EFarmTileStateType.FarmWet)
            {
                if(cropGrowth.IsHarvestable)
                {
                    cropGrowth.Harvest();
                }
                else
                {
                    Debug.Log("아직 수확할 수 없음");
                }
                
            }

        }
    }

    private FarmTile GetTargetTile()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if(Physics.Raycast(ray, out RaycastHit hit, _detectRange))
        {
            return hit.collider.GetComponent<FarmTile>();
        }
        return null;
    }
}
