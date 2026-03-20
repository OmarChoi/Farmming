using TMPro;
using UnityEngine;

public class UI_GatheringTest : MonoBehaviour
{
    [Header("테스트 대상")]
    [SerializeField] private GatheringObject _target;
    [SerializeField] private int _testDamage = 10;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _dropText;

    private int _totalDropCount;

    private void OnEnable()
    {
        GatheringObject.OnGatheringCompleted += HandleGatheringCompleted;
    }

    private void OnDisable()
    {
        GatheringObject.OnGatheringCompleted -= HandleGatheringCompleted;
    }

    private void HandleGatheringCompleted(GatheringObject obj)
    {
        if (obj != _target) return;

        foreach (var drop in obj.GatheringData.Drops)
        {
            _totalDropCount += drop.GetRandomQuantity();
        }

        if (_dropText != null) _dropText.text = $"Drop: {_totalDropCount}";
    }
}
