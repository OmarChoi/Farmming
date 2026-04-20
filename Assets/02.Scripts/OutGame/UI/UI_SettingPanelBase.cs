using UnityEngine;

public abstract class UI_SettingPanelBase : MonoBehaviour
{
    // UI 값 초기화와 리스너 구독을 수행.
    public virtual void OnShow() { }

    // 리스너 해제와 정리를 수행.
    public virtual void OnHide() { }
}
