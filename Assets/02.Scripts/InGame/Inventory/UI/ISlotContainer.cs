/// UI_Slot이 소유자(인벤토리/창고)와 통신하기 위한 인터페이스.
public interface ISlotContainer
{
    void BeginDrag(UI_Slot source, bool shift);
    void EndDrag();
    void OnSlotHoverEnter(UI_Slot slot);
    void OnSlotHoverExit();
    void OnSlotClicked(UI_Slot clicked);
    void OnSlotRightClicked(UI_Slot clicked);
}
