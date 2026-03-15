using UnityEngine;

public class PlayerFarmInput : MonoBehaviour
{
    public HelperPickup EquippedHelper;
    public float DetectRange = 2f;

    [Header("테스트용 씨앗")]
    public SeedConfig TestSeed;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            if(EquippedHelper!=null && !EquippedHelper.IsEquipped)
            {
                EquippedHelper.Equip();
            }
        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            if(EquippedHelper == null || !EquippedHelper.IsEquipped)
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

            if(current == EFarmTileStateType.Ground)
            {
                tile.StateMachine.FarmTransition(EFarmTileStateType.FarmDry);
            }
            else if(current == EFarmTileStateType.FarmDry && !tile.HasSeed)
            {
                tile.PlantSeed(TestSeed);
            }
            else if(current == EFarmTileStateType.FarmDry && tile.HasSeed)
            {
                tile.StateMachine.FarmTransition(EFarmTileStateType.FarmWet);
                tile.GetComponent<CropGrowth>().StartGrowth(tile.PlantedSeed);
            }
            else if(current == EFarmTileStateType.FarmWet)
            {
                tile.GetComponent<CropGrowth>().Harvest();
            }

        }
    }

    private FarmTile GetTargetTile()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if(Physics.Raycast(ray, out RaycastHit hit, DetectRange))
        {
            return hit.collider.GetComponent<FarmTile>();
        }
        return null;
    }
}
