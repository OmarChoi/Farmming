using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "HelperEvolutionProfile", menuName = "Scriptable Objects/Helper/HelperEvolutionProfile")]
public class HelperEvolutionProfileSO : ScriptableObject
{
    [Header("Timeline")]
    public PlayableAsset TimelineAsset;

    [Header("Background")]
    public Sprite BackgroundSprite;
    public VideoClip BackgroundVideo;
    public Color FlashColor = Color.white;

    [Header("Studio Placement")]
    public float PivotHeight = 1.2f;
    public Vector3 BeforeLocalEuler;
    public Vector3 AfterLocalEuler;

    [Header("Fallback Sequence")]
    public float IntroDuration = 2f;
    public float ImpactZoomDuration = 0.35f;
    public float AfterPullbackDuration = 0.45f;
    public float AfterScanDuration = 2.4f;
    [Range(0.5f, 3f)] public float AfterScanVerticalCurvePower = 1.25f;
    public float RevealDuration = 1.2f;
    public float OutroDuration = 1.0f;
    public float IntroSpinSpeed = 90f;

    [Header("Timeline Rotation")]
    public float ModelSwapTime = 4.65f;
    public float BeforeSpinAccelerationDuration = 4.1f;
    public float TimelineSpinMaxSpeed = 240f;

    [Header("Timeline Camera")]
    public float BeforeLookHeight = 1.2f;
    public float AfterFootLookHeight = 0.35f;
    public float AfterHeadLookHeight = 1.8f;
    public Vector3 BeforeIntroCameraLocalPosition = new Vector3(0f, 1.6f, -6f);
    public Vector3 BeforeImpactZoomCameraLocalPosition = new Vector3(0f, 1.05f, -1.45f);
    public Vector3 AfterCloseCameraLocalPosition = new Vector3(0f, 0.45f, -1.35f);
    public Vector3 AfterPullbackCameraLocalPosition = new Vector3(0f, 0.55f, -2.05f);
    public Vector3 AfterHeadCameraLocalPosition = new Vector3(0f, 1.95f, -2.15f);
    public float FinalShowcaseDuration = 2f;
    public float AfterFullShotLookHeight = 1.05f;
    public Vector3 AfterFullShotLookLocalPosition = new Vector3(0f, 1.05f, 0f);
    public Vector3 AfterFullShotCameraLocalPosition = new Vector3(0f, 1.25f, -3.25f);

    [Header("Timeline Finish")]
    public float FinalRotationAlignDuration = 2f;
    public float FinalFrontYawOffset = 0f;
    public float FinalRotationExtraTurns;

    [Header("Before Energy Rise")]
    public float BeforeEnergyRiseEndTime = 4.55f;
    public float BeforeEnergyRiseDuration = 2.5f;
    public Color BeforeEnergyRiseColor = Color.white;
    public float BeforeEnergyRiseBandWidth = 0.35f;
    [Range(0f, 1f)] public float BeforeEnergyRiseFillAlpha = 0.45f;
    [Range(0f, 2f)] public float BeforeEnergyRiseGlowAlpha = 0.85f;
    public float BeforeEnergyRiseOutlineWidth = 0.045f;
    public float BeforeEnergyRiseVerticalPadding = 0.1f;
    [Range(0f, 1f)] public float BeforeEnergyRiseRainbowStrength = 1f;
    [Range(0f, 2f)] public float BeforeEnergyRiseRainbowSpeed = 0.25f;
    public float BeforeEnergyRiseSurfaceOffset = 0.018f;

    [Header("Legacy Fallback Camera")]
    public Vector3 IntroCameraLocalPosition = new Vector3(0f, 1.8f, -6f);
    public Vector3 ZoomCameraLocalPosition = new Vector3(0f, 1.35f, -3.4f);
    public Vector3 RevealCameraLocalPosition = new Vector3(0f, 0.45f, -2.5f);
    public Vector3 FinalCameraLocalPosition = new Vector3(0f, 1.5f, -5.5f);
}
