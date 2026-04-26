using UnityEngine;

public class NpcMemoryDebugResetter : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("전체 초기화")]
    [SerializeField] private KeyCode _deleteAllMemoryKey = KeyCode.F5;

    [Header("특정 NPC 초기화")]
    [SerializeField] private KeyCode _deleteTargetMemoryKey = KeyCode.F6;
    [SerializeField] private string _targetNpcId = "Merchant";
    [SerializeField] private string _targetPlayerId = "UnknownPlayer";

    private JsonNpcMemoryRepository _repository;

    private void Awake()
    {
        _repository = new JsonNpcMemoryRepository();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_deleteAllMemoryKey))
        {
            int count = _repository.DeleteAllMemories();
            Debug.Log($"[NpcMemoryDebugResetter] 전체 NPC 기억 초기화 완료. 삭제된 파일 수: {count}");
        }

        if (Input.GetKeyDown(_deleteTargetMemoryKey))
        {
            bool deleted = _repository.DeleteMemory(_targetNpcId, _targetPlayerId);
            Debug.Log(deleted
                ? $"[NpcMemoryDebugResetter] 기억 삭제 완료: {_targetNpcId}_{_targetPlayerId}"
                : $"[NpcMemoryDebugResetter] 삭제할 기억 없음: {_targetNpcId}_{_targetPlayerId}");
        }
    }
#endif
}
