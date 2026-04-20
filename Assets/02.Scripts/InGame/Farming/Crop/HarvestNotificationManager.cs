using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HarvestNotificationManager : MonoBehaviour
{
    public static HarvestNotificationManager Instance;

    [SerializeField] private HarvestNotification _notificationPrefab;
    [SerializeField] private Transform _notificationParent;
    [SerializeField] private float _notificationDuration = 1.8f;
    [SerializeField] private HarvestItemSO _harvestItemSO;

    [Header("여러 개 동시 표시 시 세로 간격")]
    [SerializeField] private float _stackOffset = 60f;

    private int _activeCount = 0;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }

    private void OnEnable()
    {
        _harvestItemSO.OnHarvested += Show;
    }

    private void OnDisable()
    {
        _harvestItemSO.OnHarvested -= Show;
    }

    public void Show(Sprite icon, string seedName, int amount)
    {
        HarvestNotification notification = Instantiate(_notificationPrefab, _notificationParent);

        // 여러 개 동시에 뜰 때 위로 쌓임
        RectTransform rect = notification.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, _stackOffset * _activeCount);

        notification.gameObject.SetActive(true);
        notification.Setup(icon, seedName, amount);
        _activeCount++;

        // 알림이 사라지면 카운트 감소
        StartCoroutine(DecreaseCountAfter(_notificationDuration));
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        HarvestNotification notification = Instantiate(_notificationPrefab, _notificationParent);

        RectTransform rect = notification.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, _stackOffset * _activeCount);

        notification.gameObject.SetActive(true);
        notification.SetupMessage(message);
        _activeCount++;

        StartCoroutine(DecreaseCountAfter(_notificationDuration));
    }

    private System.Collections.IEnumerator DecreaseCountAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }
}
