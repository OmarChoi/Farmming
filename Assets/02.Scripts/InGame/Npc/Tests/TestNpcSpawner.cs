using UnityEngine;

public class TestNpcSpawner : MonoBehaviour
{
    [SerializeField] private NpcDataContainerSO npcDatabase;
    [SerializeField] private string npcId;
    [SerializeField] private GameObject npcPrefab;

    private void Start()
    {
        var data = npcDatabase.GetNpc(npcId);

        var npc = Instantiate(npcPrefab, transform.position, Quaternion.identity);
        npc.GetComponent<NpcController>().Initialize(data);
    }
}
