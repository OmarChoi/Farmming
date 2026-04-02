using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 처음 경험치 바는 0 사이즈
// 플레이어의 현재 소환된 헬퍼를 찾고
// 경험치 추가 알림이 들어오면 게이지 차오름
// 게이지 부드럽게 갱신
// 소환 해제 시 0으로 초기화
public class UI_HelperExperience : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private float _tweenDuration = 0.5f;
    [SerializeField] private GameObject _upgradeReadyIndicator;

    private PlayerHelperInventoryAbility _ability;
    private HelperExperience _experience;
    private Tweener _fillTween;

    private void Awake()
    {
        _fillImage.fillAmount = 0f;

        if (_upgradeReadyIndicator != null)
        { 
            _upgradeReadyIndicator.SetActive(false);
        }

        PlayerHelperInventoryAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        _fillTween?.Kill();
        PlayerHelperInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Bind(PlayerHelperInventoryAbility ability)
    {
        Unbind();

        _ability = ability;
        _ability.OnSummonChanged += OnSummonChanged;

        if (_ability.ActiveHelper != null)
        {
            BindExperience(_ability.ActiveHelper.Experience);
        }
    }

    private void Unbind()
    {
        if (_ability == null)
        {
            return;
        }

        _ability.OnSummonChanged -= OnSummonChanged;
        _ability = null;
        UnbindExperience();
    }

    private void OnSummonChanged(int summonedIndex)
    {
        UnbindExperience();

        if (summonedIndex >= 0 && _ability?.ActiveHelper != null)
        {
            BindExperience(_ability.ActiveHelper.Experience);
        }
        else
        {
            // 소환 해제 시 경험치 바 초기화
            AnimateFill(0f);

            if (_upgradeReadyIndicator != null)
                _upgradeReadyIndicator.SetActive(false);
        }
    }

    private void BindExperience(HelperExperience experience)
    {
        _experience = experience;
        _experience.OnExpChanged += UpdateFill;
        _experience.OnReadyToUpgrade += OnReadyToUpgrade;

        // 현재 경험치 즉시 반영
        float ratio = _experience.MaxExp > 0 ? (float)_experience.CurrentExp / _experience.MaxExp : 0f;
        _fillImage.fillAmount = ratio;
    }

    private void UnbindExperience()
    {
        if (_experience == null)
        {
            return;
        }

        _experience.OnExpChanged -= UpdateFill;
        _experience.OnReadyToUpgrade -= OnReadyToUpgrade;
        _experience = null;
    }

    private void UpdateFill(int current, int max)
    {
        if (max <= 0)
        {
            AnimateFill(0f);
            return;
        }

        float ratio = (float)current / max;
        AnimateFill(ratio);
    }

    private void OnReadyToUpgrade()
    {
        if (_upgradeReadyIndicator != null)
        { 
            _upgradeReadyIndicator.SetActive(true);
        }
    }

    private void AnimateFill(float targetRatio)
    {
        _fillTween?.Kill();
        _fillTween = _fillImage.DOFillAmount(targetRatio, _tweenDuration).SetEase(Ease.OutCubic);
    }
}
