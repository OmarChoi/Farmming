using Cysharp.Threading.Tasks;
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

    public async UniTask SaveAsync(int slot = 0)
    {
        var data = new SaveData
        {
            Terrain = _terrainGridManager.ExportSaveData()
        };

        await _repository.SaveAsync(data, slot);
        Debug.Log($"저장 완료 (슬롯 {slot})");
    }

    public async UniTask LoadAsync(int slot = 0)
    {
        SaveData data = await _repository.LoadAsync(slot);
        if (data == null)
        {
            Debug.Log($"저장 데이터 없음 (슬롯 {slot})");
            return;
        }

        _terrainGridManager.ImportSaveData(data.Terrain, _seedDatabase);
        Debug.Log($"로드 완료 (슬롯 {slot})");
    }

    public UniTask<bool> HasSaveAsync(int slot = 0) => _repository.HasSaveAsync(slot);
}