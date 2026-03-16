using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private TerrainGridManager _terrainGridManager;
    [SerializeField] private SeedDatabase _seedDatabase;

    private ISaveRepository _repository;

    private void Awake()
    {
        _repository = new LocalJsonSaveRepository();
    }

    public void Save(int slot = 0)
    {
        var data = new SaveData
        {
            Terrain = _terrainGridManager.ExportSaveData()
        };

        _repository.Save(data, slot);
        Debug.Log($"저장 완료 (슬롯 {slot})");
    }

    public void Load(int slot = 0)
    {
        SaveData data = _repository.Load(slot);
        if (data == null)
        {
            Debug.Log($"저장 데이터 없음 (슬롯 {slot})");
            return;
        }

        _terrainGridManager.ImportSaveData(data.Terrain, _seedDatabase);
        Debug.Log($"로드 완료 (슬롯 {slot})");
    }

    public bool HasSave(int slot = 0) => _repository.HasSave(slot);
}