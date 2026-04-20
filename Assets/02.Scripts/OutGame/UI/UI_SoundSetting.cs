using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master/Music/Effect 볼륨 슬라이더를 SettingManager에 실시간 바인딩한다.
/// 디스크 저장은 루트 UI_Setting에서 수행되므로, 이 패널은 값 변경만 발행한다.
/// </summary>
public class UI_SoundSetting : UI_SettingPanelBase
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _effectSlider;

    public override void OnShow()
    {
        VolumeSettings volume = SettingManager.Instance.Volume;

        InitializeSlider(_masterSlider, volume.Master, OnMasterChanged);
        InitializeSlider(_musicSlider, volume.Music, OnMusicChanged);
        InitializeSlider(_effectSlider, volume.Effect, OnEffectChanged);
    }

    public override void OnHide()
    {
        if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (_effectSlider != null) _effectSlider.onValueChanged.RemoveListener(OnEffectChanged);
    }

    // 슬라이더의 초기값을 주입하면서 onValueChanged 콜백이 재발화되지 않도록 SetValueWithoutNotify를 사용한다.
    private void InitializeSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> listener)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(listener);
    }

    private void OnMasterChanged(float value) => SettingManager.Instance.SetVolume(EAudioChannel.Master, value);
    private void OnMusicChanged(float value) => SettingManager.Instance.SetVolume(EAudioChannel.Music, value);
    private void OnEffectChanged(float value) => SettingManager.Instance.SetVolume(EAudioChannel.Effect, value);
}
