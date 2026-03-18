using UnityEngine;
using UnityEngine.UI;

public class UI_HelperInventory : MonoBehaviour
{
    [Header("슬롯 아이콘")]
    [SerializeField] private Image _leftIcon;
    [SerializeField] private Image _centerIcon;
    [SerializeField] private Image _rightIcon;

    [Header("선택 강조")]
    [SerializeField] private GameObject _summonedIndicator;

    private PlayerHelperInventoryAbility _ability;

    private void Awake()
    {
        PlayerHelperInventoryAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        PlayerHelperInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Bind(PlayerHelperInventoryAbility ability)
    {
        Unbind();

        _ability = ability;
        _ability.OnSelectionChanged += Refresh;
        _ability.OnSummonChanged += OnSummonChanged;
        Refresh();
    }

    private void Unbind()
    {
        if (_ability == null) return;

        _ability.OnSelectionChanged -= Refresh;
        _ability.OnSummonChanged -= OnSummonChanged;
        _ability = null;
    }

    private void OnSummonChanged(int summonedIndex)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_ability.Count == 0)
        {
            SetSlot(_leftIcon, null);
            SetSlot(_centerIcon, null);
            SetSlot(_rightIcon, null);
            if (_summonedIndicator != null)
                _summonedIndicator.SetActive(false);
            return;
        }

        SetSlot(_leftIcon, _ability.LeftData);
        SetSlot(_centerIcon, _ability.CenterData);
        SetSlot(_rightIcon, _ability.RightData);

        if (_summonedIndicator != null)
            _summonedIndicator.SetActive(_ability.SummonedIndex == _ability.CurrentIndex);
    }

    private void SetSlot(Image icon, HelperDataSO data)
    {
        if (data == null || data.HelperIcon == null)
        {
            icon.enabled = false;
            return;
        }

        icon.enabled = true;
        icon.sprite = data.HelperIcon;
    }
}