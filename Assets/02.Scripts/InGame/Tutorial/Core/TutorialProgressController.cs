using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class TutorialProgressController : MonoBehaviour
{
    [Header("튜토리얼 시작 퀘스트")]
    [SerializeField] private QuestDataSO _entryTutorialQuest;

    [Header("튜토리얼 퀘스트 목록")]
    [SerializeField] private List<QuestDataSO> _tutorialQuestSequence = new();

    [Header("NpcQuestService")]
    [SerializeField] private NpcQuestService _npcQuestService;

    private NpcController _tutorialNpcController;
    private Transform _playerTransform;

    private QuestDataSO _pendingNextTutorialQuest;
    private bool _isWaitingForFinalTutorialComplete;

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

        PlayerQuestAbility questAbility = player.GetAbility<PlayerQuestAbility>();
        if (questAbility == null) return false;

        _playerTransform = player.transform;
        _tutorialNpcController = tutorialNpc;

        if (questAbility.TutorialState == ETutorialState.None)
        {
            questAbility.SetTutorialState(ETutorialState.InProgress);
        }

        return true;
    }

    public bool TryGetCurrentTutorialQuest(out QuestDataSO currentQuest)
    {
        currentQuest = null;

        if (!TryGetTutorialProgress(out QuestDataSO activeQuest, out QuestDataSO nextQuest, out bool isCompleted))
        {
            return false;
        }
        if (isCompleted) return false;

        currentQuest = activeQuest ?? nextQuest;
        return currentQuest != null;
    }

    public bool TryGetTutorialProgress(out QuestDataSO currentQuest, out QuestDataSO nextQuest, out bool isCompleted)
    {
        currentQuest = null;
        nextQuest = null;
        isCompleted = false;

        if (_tutorialQuestSequence == null || _tutorialQuestSequence.Count == 0) return false;
        if (QuestManager.Instance == null) return false;

        foreach (QuestDataSO questData in _tutorialQuestSequence)
        {
            if (questData == null) continue;

            if (QuestManager.Instance.HasQuest(questData.QuestId))
            {
                currentQuest = questData;
                return true;
            }

            if (QuestManager.Instance.IsQuestCompleted(questData.QuestId)) continue;
            if (!CanAcceptByTutorialFlow(questData)) return false;

            nextQuest = questData;
            return true;
        }

        isCompleted = true;
        return true;
    }

    private bool CanAcceptByTutorialFlow(QuestDataSO questData)
    {
        if (questData == null) return false;
        if (questData.PrerequisiteQuests == null || questData.PrerequisiteQuests.Count == 0) return true;

        foreach (QuestDataSO prerequisite in questData.PrerequisiteQuests)
        {
            if (prerequisite == null || string.IsNullOrEmpty(prerequisite.QuestId)) continue;
            if (!QuestManager.Instance.IsQuestCompleted(prerequisite.QuestId)) return false;
        }
        return true;
    }

    private void HandleQuestCompleted(QuestRuntimeData questRuntime)
    {
        if (questRuntime == null || questRuntime.QuestData == null) return;
        if (!questRuntime.QuestData.IsTutorial) return;
        if (!TryGetTutorialProgress(out _, out _, out bool isCompleted)) return;

        if (isCompleted)
        {
            CompleteTutorial();
        }
    }

    public bool HandleTutorialQuestDialogueEnded()
    {
        if (!TryGetTutorialProgress(out QuestDataSO currentQuest, out QuestDataSO nextQuest, out bool isCompleted))
        {
            return false;
        }

        if (isCompleted)
        {
            CompleteTutorial();
            return false;
        }

        if (currentQuest != null) return false;

        if (nextQuest != null)
        {
            AcceptNextTutorialQuestDeferred(nextQuest).Forget();
            return true;
        }

        return false;
    }

    private async UniTaskVoid AcceptNextTutorialQuestDeferred(QuestDataSO questData)
    {
        await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        TryAcceptTutorialQuest(questData);
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

    public void ResetRuntimeState()
    {
        _tutorialNpcController = null;
        _playerTransform = null;
        _pendingNextTutorialQuest = null;
        _isWaitingForFinalTutorialComplete = false;
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
