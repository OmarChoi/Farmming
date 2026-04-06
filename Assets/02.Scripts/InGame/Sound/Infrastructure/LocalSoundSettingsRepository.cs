using UnityEngine;

/// <summary>
/// PlayerPrefs를 이용한 볼륨 설정 저장소.
/// 디바이스별 사용자 설정을 저장하며, 세이브 슬롯과 무관하다.
/// </summary>
public class LocalSoundSettingsRepository : ISoundSettingsRepository
{
    private const string Key = "SoundSettings";

    // 현재 볼륨 설정을 JSON으로 직렬화하여 저장한다
    public void Save(VolumeSettings settings)
    {
        string json = JsonUtility.ToJson(new Data(settings));
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    // 저장된 볼륨 설정을 불러온다 (없으면 기본값 반환)
    public VolumeSettings Load()
    {
        if (!PlayerPrefs.HasKey(Key)) return VolumeSettings.Default;

        Data data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key));
        return new VolumeSettings(data.Master, data.Music, data.Effect);
    }

    [System.Serializable]
    private struct Data
    {
        public float Master;
        public float Music;
        public float Effect;

        public Data(VolumeSettings s)
        {
            Master = s.Master;
            Music = s.Music;
            Effect = s.Effect;
        }
    }
}
