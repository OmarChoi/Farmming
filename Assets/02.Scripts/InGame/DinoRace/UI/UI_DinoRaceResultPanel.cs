using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DinoRaceResultPanel : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _summaryText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Button _closeButton;

    private System.Action _onClose;

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => _onClose?.Invoke());

        Hide();
    }

    public void Show(DinoRaceResult result, System.Action onClose)
    {
        _onClose = onClose;

        if (_titleText != null)
        {
            _titleText.text = result.SelectedRunnerFinishRank switch
            {
                1 => "First Place",
                2 => "Second Place",
                3 => "Third Place",
                _ => "Race Finished"
            };
        }

        if (_summaryText != null)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Selected runner rank: {result.SelectedRunnerFinishRank}");

            for (int i = 0; i < result.FinalSnapshots.Length; i++)
            {
                DinoRaceRunnerSnapshot snapshot = result.FinalSnapshots[i];
                builder.AppendLine($"{snapshot.FinishRank}. {snapshot.DisplayName}");
            }

            _summaryText.text = builder.ToString().TrimEnd();
        }

        if (_rewardText != null)
            _rewardText.text = $"Gold Won: {result.PayoutAmount}G";

        if (_root != null)
            _root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
