using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;

public class UI_Gold : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _value;

    private void Start()
    {
        CurrencyManager.Instance.OnGoldChanged += UpdateValue;
        UpdateValue(CurrencyManager.Instance.GetGold());
    }
    
    private void UpdateValue(Currency currency)
    {
        _value.text = currency.ToString();
    }
}