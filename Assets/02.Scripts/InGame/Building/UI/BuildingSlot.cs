using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BuildingSlot : MonoBehaviour
{
    public Image SliceImage;
    public Image IconImage;
    public TextMeshProUGUI Label;

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
        if (IconImage != null) IconImage.sprite = icon;
        if (Label != null) Label.text = name;
    }

    public void SetColor(Color color)
    {
        if (SliceImage != null) SliceImage.color = color;
    }

    // 슬라이스 회전/채움 + 아이콘 위치/크기 설정
    public void SetLayout(LayoutData data)
    {
        if (SliceImage != null)
        {
            var sliceRt = SliceImage.GetComponent<RectTransform>();
            sliceRt.localRotation = Quaternion.Euler(0, 0, -data.Rotation);

            SliceImage.type = Image.Type.Filled;
            SliceImage.fillMethod = Image.FillMethod.Radial360;
            SliceImage.fillOrigin = 2;
            SliceImage.fillClockwise = true;
            SliceImage.fillAmount = data.FillAmount;
        }

        if (IconImage != null)
        {
            float rad = data.IconAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 pos = dir * data.IconDist + Vector2.up * data.IconYOffset;

            var iconRt = IconImage.GetComponent<RectTransform>();
            iconRt.anchoredPosition = pos;
            iconRt.sizeDelta = Vector2.one * data.IconSize;
        }
    }
}
