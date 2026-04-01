using System;

[Serializable]
public class HelperSaveData
{
    public string HelperId;
    public int Level;
    public int Grade;
    public int Experience;
    public float Energy = -1f;       
    public long EnergySavedAt;
}