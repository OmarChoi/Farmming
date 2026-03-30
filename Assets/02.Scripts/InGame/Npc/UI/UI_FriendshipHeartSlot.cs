using UnityEngine;
using UnityEngine.UI;

public class UI_FriendshipHeartSlot : MonoBehaviour
{
    [Header("이미지")]
    [SerializeField] private Image _heartImage;

    [Header("상태별 스프라이트")]
    [SerializeField] private Sprite _emptySprite;
    [SerializeField] private Sprite _halfSprite;
    [SerializeField] private Sprite _fullSprite;

    public void SetState(EHeartFillState state)
    {
        if (_heartImage == null) return;

        switch (state)
        {
            case EHeartFillState.Full:
                _heartImage.sprite = _fullSprite;
                break;

            case EHeartFillState.Half:
                _heartImage.sprite = _halfSprite;
                break;

            default:
                _heartImage.sprite = _emptySprite;
                break;
        }
    }
}
