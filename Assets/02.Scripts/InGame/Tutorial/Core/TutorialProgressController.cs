using UnityEngine;
using System.Collections.Generic;

public class TutorialProgressController : MonoBehaviour
{
    [Header("튜토리얼 시작 퀘스트")]
    [SerializeField] private QuestDataSO _entryTutorialQuest;

    [Header("튜토리얼 퀘스트 목록")]
    [SerializeField] private List<QuestDataSO> _tutorialQuestSequence = new();

    [Header("NpcQuestService")]
    [SerializeField] private NpcQuestService _npcQuestService;

    private NpcController _tutorialNpcController;
    private PlayerController _currentPlayer;
    private Transform _playerTransform;

    private void Awake()
    {
        if (_npcQuestService == null)
        {
            _npcQuestService = FindFirstObjectByType<NpcQuestService>();
        }
    }

    private void OnEnable()
    {
        QuestManager.OnQuestManagerReady += HandleQuestManagerReady;
        SubscribeQuestEvents();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestManagerReady -= HandleQuestManagerReady;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleQuestManagerReady()
    {
        SubscribeQuestEvents();
    }

    private void SubscribeQuestEvents()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
    }

    public bool BeginTutorial(PlayerController player, NpcController tutorialNpc)
    {
        if (player == null || tutorialNpc == null) return false;

        _currentPlayer = player;
        _playerTransform = player.transform;
        _tutorialNpcController = tutorialNpc;

        if (_currentPlayer.TutorialState == ETutorialState.None)
        {
            _currentPlayer.SetTutorialState(ETutorialState.InProgress);
        }

        return true;
    }

    public bool TryGetCurrentTutorialQuest(out QuestDataSO currentQuest)
    {
        currentQuest = null;

        if (_tutorialQuestSequence == null || _tutorialQuestSequence.Count == 0) return false;
        if (QuestManager.Instance == null) return false;

        foreach (QuestDataSO questData in _tutorialQuestSequence)
        {
            if (questData == null) continue;

            bool isCompleted = QuestManager.Instance.IsQuestCompleted(questData.QuestId);
            bool hasQuest = QuestManager.Instance.HasQuest(questData.QuestId);
            bool canAccept = QuestManager.Instance.CanAcceptQuest(questData);
            bool canAcceptByFlow = CanAcceptByTutorialFlow(questData);

            // 이미 완료했으면 다음으로 넘어갑니다.
            if (isCompleted)
            {
                continue;
            }
            // 이미 진행 중인 상태라면, 그 퀘스트를 현재 퀘스트로 간주합니다.
            if (hasQuest)
            {
                currentQuest = questData;
                return true;
            }
            // 아직 시작하지 않았고, 지금 수락 가능한 첫 퀘스트면 이걸 반환합니다.
            if (canAccept && canAcceptByFlow)
            {
                currentQuest = questData;
                return true;
            }

            return false;
        }
        return false;
    }

    private bool CanAcceptByTutorialFlow(QuestDataSO questData)
    {
        if (questData == null) return false;
        if (questData.PrerequisiteQuests == null || questData.PrerequisiteQuests.Count == 0) return true;

        foreach (QuestDataSO prerequisite in questData.PrerequisiteQuests)
        {
            if (prerequisite == null || string.IsNullOrEmpty(prerequisite.QuestId)) continue;

            if (!QuestManager.Instance.IsQuestCompleted(prerequisite.QuestId))
            {
                return false;
            }
        }
        return true;
    }

    private void HandleQuestCompleted(QuestRuntimeData questRuntime)
    {
        if (questRuntime == null || questRuntime.QuestData == null) return;

        QuestDataSO completedQuest = questRuntime.QuestData;
        if (!completedQuest.IsTutorial) return;

        if (TryGetNextTutorialQuest(completedQuest, out QuestDataSO nextQuest))
        {
            TryAcceptTutorialQuest(nextQuest);
            return;
        }

        CompleteTutorial();
    }

    private bool TryGetNextTutorialQuest(QuestDataSO completedQuest, out QuestDataSO nextQuest)
    {
        nextQuest = null;

        if (completedQuest == null) return false;
        if (_tutorialQuestSequence == null || _tutorialQuestSequence.Count == 0) return false;

        for (int i = 0; i < _tutorialQuestSequence.Count; i++)
        {
            QuestDataSO current = _tutorialQuestSequence[i];
            if (current == null) continue;
            if (current.QuestId != completedQuest.QuestId) continue;

            int nextIndex = i + 1;
            if (nextIndex >= _tutorialQuestSequence.Count) return false;

            nextQuest = _tutorialQuestSequence[nextIndex];
            return nextQuest != null;
        }

        return false;
    }

    private void TryAcceptTutorialQuest(QuestDataSO questData)
    {
        if (questData == null || QuestManager.Instance == null) return;
        if (_tutorialNpcController == null || _playerTransform == null || _npcQuestService == null) return;

        if (QuestManager.Instance.IsQuestCompleted(questData.QuestId)) return;
        if (QuestManager.Instance.HasQuest(questData.QuestId)) return;
        if (!QuestManager.Instance.CanAcceptQuest(questData)) return;
        if (!CanAcceptByTutorialFlow(questData)) return;

        NpcInteractionComponent interaction = _tutorialNpcController.GetComponent<NpcInteractionComponent>();
        if (interaction == null) return;

        NpcInteractionContext context = new NpcInteractionContext(
            _tutorialNpcController,
            _playerTransform,
            interaction);

        _npcQuestService.ExecuteTutorialQuestInteraction(context, questData);
    }

    private void CompleteTutorial()
    {
        TutorialManager.Instance?.CompleteTutorial();
    }

    // 현재 상호작용하는 NPC가 튜토리얼 NPC인지 확인하는 메서드입니다.
    public bool IsTutorialNpc(NpcController npc)
    {
        if (npc == null) return false;

        NpcQuest provider = npc.Quest;
        if (provider == null || provider.Quests == null) return false;

        foreach (QuestDataSO questData in provider.Quests)
        {
            if (questData == null) continue;
            if (questData.IsTutorial) return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (_tutorialQuestSequence == null || _tutorialQuestSequence.Count == 0) return;
        if (_entryTutorialQuest == null) return;

        if (_tutorialQuestSequence[0] != _entryTutorialQuest)
        {
            Debug.LogWarning("튜토리얼 시작 퀘스트는 튜토리얼 퀘스트 목록의 첫 번째와 같아야 합니다.");
        }
    }
}
