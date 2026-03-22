using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // 테스트를 위해 일단 단일 구조로 생성했습니다.
    // 추후 여러 퀘스트를 동시에 진행할 때 List 혹은 Dictionary 구조로 바꿀 예정입니다.
    private QuestRuntimeData _currentQuest;

    [Header("플레이어 컴포넌트")]
    [SerializeField] private PlayerInventoryAbility _playerInventory;

    public QuestRuntimeData CurrentQuest => _currentQuest;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 테스트를 위한 단일 구조 퀘스트 형태로 만든 메서드입니다.
    // 활성화된 퀘스트가 있으면 새 퀘스트를 받지 못합니다.
    public bool HasActiveQuest()
    {
        return _currentQuest != null && _currentQuest.Status != EQuestStatus.Completed;
    }

    // 퀘스트 수락을 확인하는 메서드입니다.
    public bool AcceptQuest(QuestDataSO questData)
    {
        if (questData == null) return false;
        if (HasActiveQuest()) return false;

        _currentQuest = new QuestRuntimeData(questData);
        return true;
    }

    // 현재 퀘스트의 진행도를 확인하는 메서드입니다.
    public void AddProgress(EQuestObjectiveType objectiveType, string targetId, int amount = 1)
    {
        if (_currentQuest == null) return;
        if (_currentQuest.Status != EQuestStatus.InProgress) return;

        QuestDataSO questData = _currentQuest.QuestData;

        if (questData.ObjectiveType != objectiveType) return;
        if (questData.TargetId != targetId) return;

        _currentQuest.CurrentAmount += amount;

        if (_currentQuest.CurrentAmount >= questData.RequiredAmount)
        {
            _currentQuest.CurrentAmount = questData.RequiredAmount;
            _currentQuest.Status = EQuestStatus.CanComplete;
        }
    }

    // 퀘스트를 완료할 수 있는 지 확인하는 메서드입니다.
    public bool CanCompleteQuest()
    {
        return _currentQuest != null && _currentQuest.Status == EQuestStatus.CanComplete;
    }

    // 퀘스트가 완료되었는 지 확인하는 메서드입니다.
    public bool CompleteQuest()
    {
        if (!CanCompleteQuest()) return false;

        GiveReward(_currentQuest.QuestData.Reward);
        _currentQuest.Status = EQuestStatus.Completed;
        return true;
    }

    // 완료된 퀘스트를 목록에서 비우는 메서드입니다.
    public void ClearCompletedQuest()
    {
        if (_currentQuest != null && _currentQuest.Status == EQuestStatus.Completed)
        {
            _currentQuest = null;
        }
    }

    private void GiveReward(QuestRewardData reward)
    {
        if (reward == null) return;

        switch (reward.RewardType)
        {
            case EQuestRewardType.Gold:
                CurrencyManager.Instance.AddGold(reward.Amount);
                break;

            case EQuestRewardType.Item:
                _playerInventory.AddItem(reward.RewardItem, reward.Amount);
                break;
        }
    }
}
