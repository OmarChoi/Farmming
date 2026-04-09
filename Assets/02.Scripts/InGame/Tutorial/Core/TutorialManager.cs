using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("튜토리얼 NPC")]
    [SerializeField] private NpcDataSO _tutorialNpc;

    [Header("튜토리얼 시작 퀘스트")]
    [SerializeField] private QuestDataSO _entryTutorialQuest;

    [Header("튜토리얼 퀘스트 목록")]
    [SerializeField] private List<QuestDataSO> _tutorialQuestSequence = new();

    [Header("NpcQuestService")]
    [SerializeField] private NpcQuestService _npcQuestService;

    private PlayerController _currentPlayer;
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

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    public void TryStartTutorial(PlayerController player)
    {
        if (_isTutorialStarted || player == null || _tutorialNpc == null) return;
        if (player.TutorialState == ETutorialState.Completed) return;

        _currentPlayer = player;
        _playerTransform = player.transform;

        if (_currentPlayer.TutorialState == ETutorialState.None)
        {
            _currentPlayer.SetTutorialState(ETutorialState.InProgress);
        }

        StartTutorial();
    }

    private void StartTutorial()
    {
        if (_playerTransform == null) return;

        DespawnTutorialNpc();

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

        if (_tutorialNpcController == null || _playerTransform == null)
        {
            FailTutorialStart();
            yield break;
        }

        NpcInteractionComponent npcInteraction = _tutorialNpcController.GetComponent<NpcInteractionComponent>();
        PlayerNPCInteractionAbility playerInteraction = _playerTransform.GetComponentInChildren<PlayerNPCInteractionAbility>();

        if (npcInteraction == null || playerInteraction == null)
        {
            FailTutorialStart();
            yield break;
        }

        // 1. 자동으로 상호작용을 시작합니다.
        playerInteraction.BeginAutoInteraction(npcInteraction);

        // 2. 대화 UI가 열린 다음 프레임에 튜토리얼 퀘스트 시작합니다.
        yield return null;

        NpcInteractionContext context = new NpcInteractionContext(
            _tutorialNpcController,
            _playerTransform,
            npcInteraction);

        _npcQuestService?.ExecuteTutorialQuestInteraction(context, _entryTutorialQuest);
    }

    private void HandleQuestCompleted(QuestRuntimeData questRuntime)
    {
        if (questRuntime == null || questRuntime.QuestData == null) return;

        if (!questRuntime.QuestData.IsTutorial || !AreAllTutorialQuestsCompleted()) return;

        CompleteTutorial();
    }

    private bool AreAllTutorialQuestsCompleted()
    {
        if (_tutorialQuestSequence == null || _tutorialQuestSequence.Count == 0) return false;

        if (QuestManager.Instance == null) return false;

        foreach (QuestDataSO quest in _tutorialQuestSequence)
        {
            if (quest == null) continue;
            if (!QuestManager.Instance.IsQuestCompleted(quest.QuestId)) return false;
        }

        return true;
    }

    public void CompleteTutorial()
    {
        if (_currentPlayer != null)
        {
            _currentPlayer.SetTutorialState(ETutorialState.Completed);
        }

        DespawnTutorialNpc();
        _isTutorialStarted = false;
        _currentPlayer = null;
        _playerTransform = null;
    }

    private void FailTutorialStart()
    {
        DespawnTutorialNpc();
        _isTutorialStarted = false;
        _currentPlayer = null;
        _playerTransform = null;
    }

    // 기존에 존재하는 튜토리얼 NPC가 있다면 제거합니다.
    private void DespawnTutorialNpc()
    {
        if (_tutorialNpcController == null) return;

        if (NpcSpawnManager.Instance != null)
        {
            NpcSpawnManager.Instance.Despawn(_tutorialNpcController);
        }
        else
        {
            Destroy(_tutorialNpcController.gameObject);
        }

        _tutorialNpcController = null;
    }
}
