using UnityEngine;

[CreateAssetMenu(fileName = "DungeonEnvironmentProfile", menuName = "Map/Dungeon Environment Profile")]
public class DungeonEnvironmentProfile : ScriptableObject
{
    [Header("Ambient")]
    public Color AmbientColor = new Color(0.20f, 0.22f, 0.28f, 1f);
    [Range(0f, 8f)]
    public float AmbientIntensity = 0.6f;

    [Header("Directional Light")]
    public Color DirectionalLightColor = Color.white;
    [Range(0f, 8f)]
    public float DirectionalLightIntensity = 0.35f;

    [Header("Fog")]
    public bool UseFog = true;
    public Color FogColor = new Color(0.10f, 0.12f, 0.16f, 1f);
    [Range(0f, 1f)]
    public float FogDensity = 0.03f;

    [Header("Skybox")]
    public Material SkyboxMaterial;

    [Header("Post Processing")]
    [Range(-5f, 5f)]
    public float PostExposure = -0.8f;
    [Range(0f, 1f)]
    public float VignetteIntensity = 0.28f;

    [Header("Depth of Field (Gaussian)")]
    public bool UseDepthOfField = true;
    [Tooltip("블러가 시작되는 거리(m)")]
    [Min(0f)]
    public float DofGaussianStart = 30f;
    [Tooltip("블러가 최대치가 되는 거리(m)")]
    [Min(0f)]
    public float DofGaussianEnd = 85f;
    [Range(0.5f, 1.5f)]
    public float DofGaussianMaxRadius = 0.7f;
}