using UnityEngine;

[CreateAssetMenu(fileName = "CustomizeData", menuName = "Farmming/CustomizeData")]
public class CustomizeData : ScriptableObject
{
    public int BodyIndex;
    public int HairIndex;
    public int HatIndex;
    public int EyeIndex;
    public int MouthIndex;
    public int EyebrowIndex;
    public int CheekIndex;

    public void CopyFrom(CharacterPartSwapper swapper)
    {
        BodyIndex = swapper.currentBodyIndex;
        HairIndex = swapper.currentHairIndex;
        HatIndex = swapper.currentHatIndex;
        EyeIndex = swapper.currentEyeIndex;
        MouthIndex = swapper.currentMouthIndex;
        EyebrowIndex = swapper.currentEyebrowIndex;
        CheekIndex = swapper.currentCheekIndex;
    }

    public void ApplyTo(CharacterPartSwapper swapper)
    {
        swapper.SetBody(BodyIndex);
        swapper.SetHair(HairIndex);
        swapper.SetHat(HatIndex);
        swapper.SetEye(EyeIndex);
        swapper.SetMouth(MouthIndex);
        swapper.SetEyebrow(EyebrowIndex);
        swapper.SetCheek(CheekIndex);
    }
}