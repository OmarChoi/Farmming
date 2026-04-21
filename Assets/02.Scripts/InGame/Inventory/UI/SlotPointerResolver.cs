using System.Collections.Generic;
using UnityEngine;

public static class SlotPointerResolver
{
    public static UI_Slot FindSlotAtScreenPosition(IEnumerable<UI_Slot> slots, Vector2 screenPosition)
    {
        if (slots == null)
            return null;

        foreach (UI_Slot slot in slots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy)
                continue;

            RectTransform rectTransform = slot.RectTransform;
            Canvas canvas = slot.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, camera))
                return slot;
        }

        return null;
    }
}
