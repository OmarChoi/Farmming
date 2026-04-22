using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class UI_QuestCompletePopup : MonoBehaviour
{
    [SerializeField] private QuestManager _questManager;

    private IQuestProgressService _questProgressService;

    [Header("퀘스트 완료 팝업")]
    [SerializeField] private GameObject _completePopup;
    [SerializeField] private TextMeshProUGUI _completeRewardText;
    [SerializeField] private GameObject _blockPanel;
    
    [Header("팝업 트윈")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    private void Awake()
    {
        if (_questManager == null)
        {
            _questManager = FindFirstObjectByType<QuestManager>();
        }
        _questProgressService = _questManager;
    }

    private void OnEnable()
    {
        if (_questProgressService != null)
        {
            _questProgressService.OnQuestCompleted += ShowCompletePopup;
        }
        else
        {
            QuestManager.OnQuestManagerReady += SubscribeWhenReady;
        }
    }

    private void OnDisable()
    {
        if (_questProgressService != null)
        {
            _questProgressService.OnQuestCompleted -= ShowCompletePopup;
        }

        QuestManager.OnQuestManagerReady -= SubscribeWhenReady;
    }

    private void SubscribeWhenReady()
    {
        _questProgressService.OnQuestCompleted += ShowCompletePopup;
        QuestManager.OnQuestManagerReady -= SubscribeWhenReady;
    }

    private async void ShowCompletePopup(QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null || quest.QuestData.Reward == null) return;
        if (quest.QuestData.IsTutorial == true) return;

        string rewardText = QuestRewardTextFormatter.BuildQuestReward(quest.QuestData.Reward);
        _completeRewardText.text = rewardText;

        transform.SetAsLastSibling();
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
        else if (_completePopup != null)
        {
            _completePopup.SetActive(true);
            if (_blockPanel == null) return;
            _blockPanel.SetActive(true);
        }
    }

    public void OnClickPopup()
    {
        HideCompletePopupAsync().Forget();
    }

    public async UniTask HideCompletePopupAsync()
    {
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayCloseAsync();
        }
        else if (_completePopup != null)
        {
            _completePopup.SetActive(false);
            if (_blockPanel == null) return;
            _blockPanel.SetActive(false);
        }
    }
}
