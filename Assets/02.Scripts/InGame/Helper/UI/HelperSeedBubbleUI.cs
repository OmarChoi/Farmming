using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelperSeedBubbleUI : MonoBehaviour
{
    [SerializeField] private GameObject _bubbleRoot;
    [SerializeField] private Image _seedIcon;
    [SerializeField] private TextMeshProUGUI _seedNameText;

    private SeedSelectAbility _seedSelectAbility;

    private void Start()
    {
        _seedSelectAbility = GetComponentInParent<SeedSelectAbility>();

        if (_seedSelectAbility != null)
        { 
            _seedSelectAbility.OnSeedSelected += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        if (_seedSelectAbility != null)
        {
            _seedSelectAbility.OnSeedSelected -= RefreshUI;
        }
    }

    private void RefreshUI(SeedItemDataSO seed)
    {
        if (seed == null)
        {
            _bubbleRoot.SetActive(false);
            return;
        }

        _bubbleRoot.SetActive(true);
        _seedIcon.sprite = seed.Icon;
        _seedNameText.text = seed.DisplayName;
    }
}
