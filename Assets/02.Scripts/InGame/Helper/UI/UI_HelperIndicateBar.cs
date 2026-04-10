using UnityEngine;

public class UI_HelperIndicateBar : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;

    private PlayerHelperInventoryAbility _ability;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        SetVisible(false);
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
        _ability.OnSummonChanged += OnSummonChanged;
        Refresh();
    }

    private void Unbind()
    {
        if (_ability == null)
        {
            return;
        }

        _ability.OnSummonChanged -= OnSummonChanged;
        _ability = null;
        SetVisible(false);
    }

    private void OnSummonChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        SetVisible(_ability != null && _ability.HasActiveMainHelper);
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
