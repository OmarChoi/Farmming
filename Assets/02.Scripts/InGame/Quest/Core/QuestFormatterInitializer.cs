using UnityEngine;

public class QuestFormatterInitializer : MonoBehaviour
{
    [SerializeField] private NpcDataContainerSO _npcDataContainer;

    private void Awake()
    {
        if (_npcDataContainer == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("NpcDataContainerSO가 연결되지 않았습니다.");
#endif
            return;
        }

        QuestTextUtility.SetNpcDataContainer(_npcDataContainer);
    }
}
