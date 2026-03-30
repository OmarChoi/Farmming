using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BuildingListSlot : MonoBehaviour
{
    [SerializeField] private Image _sliceImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _label;
    
    private RectTransform _rectTransform;

    public struct LayoutData
    {
        public float Rotation;
        public float FillAmount;
        public float IconAngle;
        public float IconDist;
        public float IconSize;
        public float IconYOffset;
    }

    public void SetData(Sprite icon, string name)
    {
        if (_iconImage != null) _iconImage.sprite = icon;
        if (_label != null) _label.text = name;
    }

    public void SetColor(Color color)
    {
        if (_sliceImage != null) _sliceImage.color = color;
    }

    // 슬라이스 회전/채움 + 아이콘 위치/크기 설정
    public void SetLayout(LayoutData data)
    {
        if (_sliceImage != null)
        {
            var sliceRt = _sliceImage.GetComponent<RectTransform>();
            sliceRt.localRotation = Quaternion.Euler(0, 0, -data.Rotation);

            _sliceImage.type = Image.Type.Filled;
            _sliceImage.fillMethod = Image.FillMethod.Radial360;
            _sliceImage.fillOrigin = 2;
            _sliceImage.fillClockwise = true;
            _sliceImage.fillAmount = data.FillAmount;
        }

        if (_iconImage != null)
        {
            float rad = data.IconAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 pos = dir * data.IconDist + Vector2.up * data.IconYOffset;

            var iconRt = _iconImage.GetComponent<RectTransform>();
            iconRt.anchoredPosition = pos;
            iconRt.sizeDelta = Vector2.one * data.IconSize;
        }
    }
}
