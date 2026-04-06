using TMPro;
using UnityEngine;

public class UI_DungeonTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = Color.red;
    [SerializeField] private float _warningThreshold = 30f;

    private void Update()
    {
        if (DungeonTimer.Instance == null || !DungeonTimer.Instance.IsRunning)
        {
            _timerText.gameObject.SetActive(false);
            return;
        }

        _timerText.gameObject.SetActive(true);

        float remaining = DungeonTimer.Instance.RemainingSeconds;
        int minutes = (int)(remaining / 60f);
        int seconds = (int)(remaining % 60f);

        _timerText.text = $"{minutes:00}:{seconds:00}";
        _timerText.color = remaining <= _warningThreshold ? _warningColor : _normalColor;
    }
}