using UnityEngine;

[CreateAssetMenu(fileName = "SeedDataItemSO", menuName = "Scriptable Objects/SeedDataItemSO")]
public class SeedItemDataSO : ItemDataSO
{
    [SerializeField] private SeedConfig _seedConfig;

    public SeedConfig SeedConfig => _seedConfig;
}
