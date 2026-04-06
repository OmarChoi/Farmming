using TMPro;
using UnityEngine;

public class UI_DungeonTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _warningColor = Color.red;
    [SerializeField] private float _warningThreshold = 30f;

    private void OnEnable()
    {
        if (DungeonTimer.Instance != null)
            DungeonTimer.Instance.OnSecondChanged += UpdateDisplay;
    }

    private void OnDisable()
    {
        if (DungeonTimer.Instance != null)
            DungeonTimer.Instance.OnSecondChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int totalSeconds)
    {
        if (!_timerText.gameObject.activeSelf)
            _timerText.gameObject.SetActive(true);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        _timerText.text = $"{minutes:00}:{seconds:00}";
        _timerText.color = totalSeconds <= _warningThreshold ? _warningColor : _normalColor;
    }
}