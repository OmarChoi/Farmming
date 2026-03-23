using UnityEngine;
using UnityEngine.UI;

public class UI_Stamina : MonoBehaviour
{
    [SerializeField] private Image _fillImage;

    private PlayerStamina _stamina;

    private void Awake()
    {
        PlayerStaminaAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        PlayerStaminaAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Bind(PlayerStaminaAbility ability)
    {
        Unbind();

        _stamina = ability.Stamina;
        _stamina.OnChanged += UpdateFill;
        _stamina.OnMaxChanged += _ => UpdateFill(_stamina.Current);
        UpdateFill(_stamina.Current);
    }

    private void Unbind()
    {
        if (_stamina == null) return;

        _stamina.OnChanged -= UpdateFill;
        _stamina = null;
    }

    private void UpdateFill(float current)
    {
        _fillImage.fillAmount = current / _stamina.Max;
    }
}