using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "HelperEvolutionProfile", menuName = "Scriptable Objects/Helper/HelperEvolutionProfile")]
public class HelperEvolutionProfileSO : ScriptableObject
{
    [Header("Timeline")]
    public PlayableAsset TimelineAsset;

    [Header("Background")]
    public Sprite BackgroundSprite;
    public Color FlashColor = Color.white;

    [Header("Studio Placement")]
    public float PivotHeight = 1.2f;
    public Vector3 BeforeLocalEuler;
    public Vector3 AfterLocalEuler;

    [Header("Fallback Sequence")]
    public float IntroDuration = 2f;
    public float ZoomDuration = 0.8f;
    public float RevealDuration = 1.2f;
    public float OutroDuration = 1.0f;
    public float IntroSpinSpeed = 90f;

    [Header("Fallback Camera")]
    public Vector3 IntroCameraLocalPosition = new Vector3(0f, 1.8f, -6f);
    public Vector3 ZoomCameraLocalPosition = new Vector3(0f, 1.35f, -3.4f);
    public Vector3 RevealCameraLocalPosition = new Vector3(0f, 0.45f, -2.5f);
    public Vector3 FinalCameraLocalPosition = new Vector3(0f, 1.5f, -5.5f);
}
