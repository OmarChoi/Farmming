using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class UI_QuestCompletePopup : MonoBehaviour
{
    [Header("퀘스트 완료 팝업")]
    [SerializeField] private GameObject _completePopup;
    [SerializeField] private TextMeshProUGUI _completeRewardText;

    [Header("팝업 트윈")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted += ShowCompletePopup;
        }
        else
        {
            QuestManager.OnQuestManagerReady += SubscribeWhenReady;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= ShowCompletePopup;
        }

        QuestManager.OnQuestManagerReady -= SubscribeWhenReady;
    }

    private void SubscribeWhenReady()
    {
        QuestManager.Instance.OnQuestCompleted += ShowCompletePopup;
        QuestManager.OnQuestManagerReady -= SubscribeWhenReady;
    }

    private async void ShowCompletePopup(QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null || quest.QuestData.Reward == null) return;

        string rewardText = QuestRewardTextFormatter.BuildQuestReward(quest.QuestData.Reward);
        _completeRewardText.text = rewardText;

        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
        else if (_completePopup != null)
        {
            _completePopup.SetActive(true);
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
        }
    }
}
