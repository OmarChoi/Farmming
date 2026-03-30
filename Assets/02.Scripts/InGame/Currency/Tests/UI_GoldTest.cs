using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GoldTest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private Button _addButton;
    [SerializeField] private Button _spendButton;
    [SerializeField] private double _addAmount = 100;
    [SerializeField] private double _spendAmount = 50;

    private void Start()
    {
        UpdateGoldText(CurrencyManager.Instance.GetGold());

        CurrencyManager.Instance.OnGoldChanged += UpdateGoldText;

    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnGoldChanged -= UpdateGoldText;
        }
    }

    private void UpdateGoldText(Currency gold)
    {
        _goldText.text = gold.ToString();
    }
}
