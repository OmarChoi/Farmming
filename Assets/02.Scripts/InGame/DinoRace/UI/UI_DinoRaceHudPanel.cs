using TMPro;
using UnityEngine;

public class UI_DinoRaceHudPanel : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private TextMeshProUGUI[] _laneTexts;

    public void Show()
    {
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

    public void Refresh(
        DinoRaceRunnerSnapshot[] snapshots,
        int selectedRunnerIndex,
        EDinoRaceState state,
        float countdownRemaining)
    {
        if (_statusText != null)
        {
            _statusText.text = state switch
            {
                EDinoRaceState.Countdown => "Preparing",
                EDinoRaceState.Running => "Racing",
                EDinoRaceState.Finished => "Finished",
                _ => string.Empty
            };
        }

        if (_countdownText != null)
        {
            _countdownText.text = state == EDinoRaceState.Countdown
                ? Mathf.CeilToInt(Mathf.Max(0f, countdownRemaining)).ToString()
                : string.Empty;
        }

        int laneTextCount = _laneTexts != null ? _laneTexts.Length : 0;
        for (int i = 0; i < laneTextCount; i++)
        {
            if (_laneTexts[i] == null) continue;

            if (snapshots == null || i >= snapshots.Length)
            {
                _laneTexts[i].text = string.Empty;
                continue;
            }

            DinoRaceRunnerSnapshot snapshot = snapshots[i];
            int currentRank = GetCurrentRank(snapshots, i);
            string selectedPrefix = i == selectedRunnerIndex ? ">" : string.Empty;
            string eventText = snapshot.EventType switch
            {
                EDinoRaceEventType.Stun => "Stunned",
                EDinoRaceEventType.SpeedUp => "Boost",
                EDinoRaceEventType.SlowDown => "Slow",
                _ => "Normal"
            };

            string progressText = snapshot.IsFinished
                ? $"Finish #{snapshot.FinishRank}"
                : $"{snapshot.Distance:F1}m";

            _laneTexts[i].text = $"{selectedPrefix}{snapshot.DisplayName} | Rank {currentRank} | {progressText} | {eventText}";
        }
    }

    private static int GetCurrentRank(DinoRaceRunnerSnapshot[] snapshots, int targetIndex)
    {
        int rank = 1;
        DinoRaceRunnerSnapshot target = snapshots[targetIndex];

        for (int i = 0; i < snapshots.Length; i++)
        {
            if (i == targetIndex) continue;

            DinoRaceRunnerSnapshot other = snapshots[i];
            if (other.IsFinished && target.IsFinished)
            {
                if (other.FinishRank > 0 && other.FinishRank < target.FinishRank)
                    rank++;
                continue;
            }

            if (other.IsFinished && !target.IsFinished)
            {
                rank++;
                continue;
            }

            if (!other.IsFinished && !target.IsFinished && other.Distance > target.Distance)
                rank++;
        }

        return rank;
    }
}
