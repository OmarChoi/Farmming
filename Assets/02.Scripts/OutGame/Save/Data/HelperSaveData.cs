using System;

[Serializable]
public class HelperSaveData
{
    public string HelperId;
    public int Level;
    public int Grade;
    public int Experience;
    public float Energy = -1f;       // -1: 저장 안 됨 → 최대치로 시작
    public float EnergySavedAt;      // Time.realtimeSinceStartup 기준 저장 시각
}