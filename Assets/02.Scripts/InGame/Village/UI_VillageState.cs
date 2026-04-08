using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class UI_VillageState : UIBase
{
    [SerializeField] private float _animationDuration = 0.25f;

    protected override async UniTask OnOpenAnimation()
    {
        transform.localScale = Vector3.zero;
        await transform.DOScale(Vector3.one, _animationDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
    }

    protected override async UniTask OnCloseAnimation()
    {
        await transform.DOScale(Vector3.zero, _animationDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
    }
}