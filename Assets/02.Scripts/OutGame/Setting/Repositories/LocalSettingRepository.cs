using UnityEngine;

/// <summary>
/// PlayerPrefs + JsonUtility를 이용한 로컬 설정 저장소.
/// VolumeSettings는 readonly struct이므로 JsonUtility가 직접 직렬화하지 못해
/// 평탄화된 DTO(SerializableData)를 거쳐 읽고 쓴다.
/// </summary>
public class LocalSettingRepository : ISettingRepository
{
    // PlayerPrefs 키. 기존 SoundManager가 쓰던 "SoundSettings" 키와 분리해
    // 향후 설정 항목이 늘어나도 단일 키로 통합 관리되도록 한다.
    private const string Key = "GameSettings";

    // 저장된 설정을 불러온다. 키가 없으면 기본값 GameSettings를 반환한다.
    public GameSetting Load()
    {
        if (!PlayerPrefs.HasKey(Key)) return new GameSetting();

        string json = PlayerPrefs.GetString(Key);
        SerializableData data = JsonUtility.FromJson<SerializableData>(json);
        return data.ToGameSettings();
    }

    // 현재 설정을 JSON으로 직렬화해 PlayerPrefs에 기록한다.
    public void Save(GameSetting gameSettings)
    {
        SerializableData data = SerializableData.From(gameSettings);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    // JsonUtility가 다룰 수 있도록 VolumeSettings(struct)를 평탄화한 DTO.
    // 저장 포맷이 바뀌어도 이 내부 구조만 수정하면 외부 API는 영향을 받지 않는다.
    [System.Serializable]
    private struct SerializableData
    {
        public float Master;
        public float Music;
        public float Effect;

        public static SerializableData From(GameSetting gameSettings)
        {
            VolumeSettings v = gameSettings.Volume;
            return new SerializableData
            {
                Master = v.Master,
                Music = v.Music,
                Effect = v.Effect,
            };
        }

        public GameSetting ToGameSettings()
        {
            return new GameSetting(new VolumeSettings(Master, Music, Effect));
        }
    }
}
