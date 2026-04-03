using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_QuestBoardSlot : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private TypewriterWithWrap _wrapper;

    [Header("퀘스트 슬롯 텍스트")]
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _questTargetText;
    [SerializeField] private TextMeshProUGUI _questRewardText;
    private string _rewardText;

    [Header("퀘스트 수락 버튼")]
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Image _acceptButtonImage;
    [SerializeField] private TextMeshProUGUI _acceptButtonText;

    private UI_QuestBoard _uiQuestBoard;
    private int _slotIndex;
    private QuestDataSO _questData;

    public int SlotIndex => _slotIndex;
    public QuestDataSO QuestData => _questData;

    private void Awake()
    {
        if (_wrapper == null)
        {
            _wrapper = FindFirstObjectByType<TypewriterWithWrap>();
        }
    }

    public void Init(UI_QuestBoard uiQuestBoard, int index)
    {
        _uiQuestBoard = uiQuestBoard;
        _slotIndex = index;

        if (_acceptButton != null)
        {
            _acceptButton.onClick.RemoveAllListeners();
            _acceptButton.onClick.AddListener(OnClickAcceptButton);
        }
    }

    public void Refresh(QuestDataSO quest)
    {
        _questData = quest;

        if (quest == null)
        {
            _questNameText.text = "";
            _descriptionText.text = "";
            _questTargetText.text = "";
            _questRewardText.text = "";

            if (_acceptButton != null)
            {
                _acceptButton.interactable = false;
            }
            if (_acceptButtonText != null)
            {
                _acceptButtonText.text = "";
            }
            return;
        }

        _questNameText.text = quest.QuestName;
        _descriptionText.text = _wrapper.WrapText(quest.Description, _descriptionText);
        SetWrappedText(QuestObjectiveTextFormatter.BuildTargetText(quest), _questTargetText);
        SetWrappedText(QuestRewardTextFormatter.BuildQuestReward(quest.Reward), _questRewardText);

        RefreshButtonState();
    }

    private void SetWrappedText(string text, TextMeshProUGUI target)
    {
        target.text = string.IsNullOrEmpty(text)
            ? ""
            : _wrapper.WrapText(text, target);
    }

    private void RefreshButtonState()
    {
        if (_acceptButton == null || _acceptButtonText == null) return;

        if (_questData == null || QuestManager.Instance == null)
        {
            _acceptButton.interactable = false;
            _acceptButtonText.text = "";
            if (_acceptButtonImage != null)
            {
                _acceptButtonImage.color = Color.white;
            }
            return;
        }

        QuestManager questManager = QuestManager.Instance;

        bool hasQuest = questManager.HasQuest(_questData.QuestId);
        bool canComplete = questManager.CanCompleteQuest(_questData.QuestId);
        bool canAccept = questManager.CanAcceptQuest(_questData);

        if (hasQuest)
        {
            if (canComplete)
            {
                _acceptButton.interactable = true;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.yellow;
                }

                _acceptButtonText.text = "완료 가능!";
            }
            else
            {
                _acceptButton.interactable = false;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.red;
                }

                _acceptButtonText.text = "진행 중...";
            }
        }
        else
        {
            if (canAccept)
            {
                _acceptButton.interactable = true;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.green;
                }

                _acceptButtonText.text = "퀘스트 수락";
            }
            else
            {
                _acceptButton.interactable = false;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.gray;
                }

                _acceptButtonText.text = "퀘스트 완료!";
            }
        }
    }

    public void OnClickAcceptButton()
    {
        _uiQuestBoard.OnQuestSlotClicked(_questData);
    }
}
