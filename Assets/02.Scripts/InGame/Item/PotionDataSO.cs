using UnityEngine;

[CreateAssetMenu(fileName = "PotionDataSO", menuName = "Scriptable Objects/PotionDataSO")]
public class PotionDataSO : ItemDataSO
{
    [SerializeField] private EPotionType _potionType;

    [Header("포션 효과 지속 시간(게임 내 분 단위)")]
    [SerializeField] private int _durationGameMinutes = 60;

    public EPotionType PotionType => _potionType;
    public int DurationGameMinutes => _durationGameMinutes;
}
