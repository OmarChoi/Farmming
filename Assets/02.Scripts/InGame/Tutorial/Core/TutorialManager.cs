using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("튜토리얼 NPC")]
    [SerializeField] private NpcDataSO _tutorialNpc;

    [Header("튜토리얼 퀘스트")]
    [SerializeField] private QuestDataSO _tutorialQuestData;

    [Header("NpcQuestService")]
    [SerializeField] private NpcQuestService _npcQuestService;

    private Transform _playerTransform;
    private NpcController _tutorialNpcController;
    private bool _isTutorialStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_npcQuestService == null)
        {
            _npcQuestService = FindFirstObjectByType<NpcQuestService>();
        }
    }

    public void TryStartTutorial(Transform playerTransform)
    {
        if (_isTutorialStarted) return;
        if (playerTransform == null) return;
        if (_tutorialNpc == null)
        {
            Debug.LogWarning("튜토리얼 NPC 데이터가 없습니다.");
            return;
        }

        _playerTransform = playerTransform;
        StartTutorial();
    }

    private void StartTutorial()
    {
        _tutorialNpcController = TutorialNpcSpawner.SpawnNearPlayer(_tutorialNpc, _playerTransform);

        if (_tutorialNpcController == null)
        {
            Debug.LogWarning("튜토리얼 NPC 스폰 실패");
            return;
        }

        _isTutorialStarted = true;
        StartCoroutine(BeginTutorialInteraction());
    }

    private IEnumerator BeginTutorialInteraction()
    {
        // NPC가 완전히 스폰되고 초기화될 때까지 잠시 대기합니다.
        yield return null;

        if (_tutorialNpcController == null || _playerTransform == null) yield break;

        NpcInteractionComponent npcInteraction = _tutorialNpcController.GetComponent<NpcInteractionComponent>();
        PlayerNPCInteractionAbility playerInteraction = _playerTransform.GetComponentInChildren<PlayerNPCInteractionAbility>();
        if (npcInteraction == null || playerInteraction == null) yield break;

        // 1. 자동으로 상호작용을 시작합니다.
        playerInteraction.BeginAutoInteraction(npcInteraction);

        // 2. 대화 UI가 열린 다음 프레임에 튜토리얼 퀘스트 시작합니다.
        yield return null;

        NpcInteractionContext context = new NpcInteractionContext(
            _tutorialNpcController,
            _playerTransform,
            npcInteraction);

        _npcQuestService?.ExecuteTutorialQuestInteraction(context, _tutorialQuestData);
    }
}
