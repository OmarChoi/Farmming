using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_VillageLevelUp : UIBase
{
    [SerializeField] private TextMeshProUGUI _levelUpText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _listParent;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _dropDistance = 800f;
    [SerializeField] private float _duration = 0.6f;

    protected override void OnOpen()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        _closeButton.onClick.AddListener(CloseUI);
    }

    protected override void OnClose()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _closeButton.onClick.RemoveListener(CloseUI);
    }

    public async UniTaskVoid SetData(int nextLevel)
    {
        if (nextLevel <= 1)
        {
            CloseUI();
            return;
        }

        for (int i = _listParent.childCount - 1; i >= 0; i--)
        {
            Destroy(_listParent.GetChild(i).gameObject);
        }

        _levelUpText.text = $"Level Up! {nextLevel - 1} -> {nextLevel}";

        var buildings = BuildingManager.Instance.AvailableBuildings;
        foreach (var building in buildings)
        {
            if (building.RequiredVillageLevel != nextLevel) continue;
            
            GameObject item = await ResourceManager.Instance.LoadAsync<GameObject>(AssetKey.Prefab.UnlockedItem);
            var go = Instantiate(item, _listParent).GetComponent<UI_UnlockedItem>();
            go.SetInfo(building);
        }
    }

    protected override async UniTask OnOpenAnimation()
    {
        _panel.DOKill();
        Vector2 targetPos = _panel.anchoredPosition;
        _panel.anchoredPosition = targetPos + Vector2.up * _dropDistance;
        await _panel.DOAnchorPos(targetPos, _duration)
                    .SetEase(Ease.OutBounce)
                    .AsyncWaitForCompletion();
    }
    
    private void CloseUI()
    {
        UIController.Instance.CloseAsync<UI_VillageLevelUp>().Forget();
    }
}
