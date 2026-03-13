using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedConfig", menuName = "Scriptable Objects/SeedConfig")]
public class SeedConfig : ScriptableObject
{
    [Header("씨앗 기본 정보")]
    public string SeedId;   // 씨앗 고유 ID
    public string SeedName; // 씨앗 이름
    public Sprite SeedIcon; // 인벤토리 아이콘

    [Header("성장 단계")]
    public List<SeedGrowthStageData> SeedGrowthStage;

    [Header("판매 가격")]
    public int BaseSellingPrice; // 기본 판매 가격

    [Header("성장 등급")]
    public SeedGrade SeedGrade; // 씨앗 등급
}
