using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DungeonEnvironmentController : MonoBehaviour
{
    [Header("Optional Scene References")]
    [SerializeField] private Light _directionalLight;
    [SerializeField] private Volume _globalVolume;

    private bool _hasCachedState;
    private AmbientMode _ambientMode;
    private Color _ambientLight;
    private float _ambientIntensity;
    private bool _fogEnabled;
    private Color _fogColor;
    private float _fogDensity;
    private Material _skyboxMaterial;
    private Color _directionalLightColor;
    private float _directionalLightIntensity;
    private bool _hasDirectionalLightState;
    private bool _hasColorAdjustmentsState;
    private float _postExposure;
    private bool _postExposureOverrideState;
    private bool _hasVignetteState;
    private float _vignetteIntensity;
    private bool _vignetteOverrideState;
    private bool _hasDepthOfFieldState;
    private DepthOfFieldMode _dofMode;
    private bool _dofModeOverrideState;
    private float _dofGaussianStart;
    private bool _dofGaussianStartOverrideState;
    private float _dofGaussianEnd;
    private bool _dofGaussianEndOverrideState;
    private float _dofGaussianMaxRadius;
    private bool _dofGaussianMaxRadiusOverrideState;
    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private DepthOfField _depthOfField;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        Restore();
    }

    public void Apply(DungeonMapConfig config)
    {
        if (config == null || config.EnvironmentProfile == null)
            return;

        ResolveReferences();
        CacheState();
        ApplyProfile(config.EnvironmentProfile);
    }

    private void ResolveReferences()
    {
        if (_directionalLight == null)
        {
            _directionalLight = RenderSettings.sun;

            if (_directionalLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (Light sceneLight in lights)
                {
                    if (sceneLight.type != LightType.Directional)
                        continue;

                    _directionalLight = sceneLight;
                    break;
                }
            }
        }

        if (_globalVolume == null)
            _globalVolume = FindFirstObjectByType<Volume>();
    }

    private void CacheState()
    {
        if (_hasCachedState)
            return;

        _ambientLight = RenderSettings.ambientLight;
        _ambientIntensity = RenderSettings.ambientIntensity;
        _ambientMode = RenderSettings.ambientMode;
        _fogEnabled = RenderSettings.fog;
        _fogColor = RenderSettings.fogColor;
        _fogDensity = RenderSettings.fogDensity;
        _skyboxMaterial = RenderSettings.skybox;

        if (_directionalLight != null)
        {
            _directionalLightColor = _directionalLight.color;
            _directionalLightIntensity = _directionalLight.intensity;
            _hasDirectionalLightState = true;
        }

        if (TryGetVolumeComponent(out _colorAdjustments))
        {
            _postExposure = _colorAdjustments.postExposure.value;
            _postExposureOverrideState = _colorAdjustments.postExposure.overrideState;
            _hasColorAdjustmentsState = true;
        }

        if (TryGetVolumeComponent(out _vignette))
        {
            _vignetteIntensity = _vignette.intensity.value;
            _vignetteOverrideState = _vignette.intensity.overrideState;
            _hasVignetteState = true;
        }

        if (TryGetVolumeComponent(out _depthOfField))
        {
            _dofMode = _depthOfField.mode.value;
            _dofModeOverrideState = _depthOfField.mode.overrideState;
            _dofGaussianStart = _depthOfField.gaussianStart.value;
            _dofGaussianStartOverrideState = _depthOfField.gaussianStart.overrideState;
            _dofGaussianEnd = _depthOfField.gaussianEnd.value;
            _dofGaussianEndOverrideState = _depthOfField.gaussianEnd.overrideState;
            _dofGaussianMaxRadius = _depthOfField.gaussianMaxRadius.value;
            _dofGaussianMaxRadiusOverrideState = _depthOfField.gaussianMaxRadius.overrideState;
            _hasDepthOfFieldState = true;
        }

        _hasCachedState = true;
    }

    private void ApplyProfile(DungeonEnvironmentProfile profile)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = profile.AmbientColor;
        RenderSettings.ambientIntensity = profile.AmbientIntensity;
        RenderSettings.fog = profile.UseFog;
        RenderSettings.fogColor = profile.FogColor;
        RenderSettings.fogDensity = profile.FogDensity;

        if (profile.SkyboxMaterial != null)
            RenderSettings.skybox = profile.SkyboxMaterial;

        if (_directionalLight != null)
        {
            _directionalLight.color = profile.DirectionalLightColor;
            _directionalLight.intensity = profile.DirectionalLightIntensity;
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = profile.PostExposure;
        }

        if (_vignette != null)
        {
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = profile.VignetteIntensity;
        }

        if (_depthOfField != null)
        {
            _depthOfField.mode.overrideState = true;
            _depthOfField.mode.value = profile.UseDepthOfField ? DepthOfFieldMode.Gaussian : DepthOfFieldMode.Off;

            if (profile.UseDepthOfField)
            {
                _depthOfField.gaussianStart.overrideState = true;
                _depthOfField.gaussianStart.value = profile.DofGaussianStart;
                _depthOfField.gaussianEnd.overrideState = true;
                _depthOfField.gaussianEnd.value = profile.DofGaussianEnd;
                _depthOfField.gaussianMaxRadius.overrideState = true;
                _depthOfField.gaussianMaxRadius.value = profile.DofGaussianMaxRadius;
            }
        }
    }

    private bool TryGetVolumeComponent<T>(out T component) where T : VolumeComponent
    {
        component = null;

        if (_globalVolume == null)
            return false;

        VolumeProfile profile = _globalVolume.profile != null ? _globalVolume.profile : _globalVolume.sharedProfile;
        return profile != null && profile.TryGet(out component);
    }

    private void Restore()
    {
        if (!_hasCachedState)
            return;

        RenderSettings.ambientMode = _ambientMode;
        RenderSettings.ambientLight = _ambientLight;
        RenderSettings.ambientIntensity = _ambientIntensity;
        RenderSettings.fog = _fogEnabled;
        RenderSettings.fogColor = _fogColor;
        RenderSettings.fogDensity = _fogDensity;
        RenderSettings.skybox = _skyboxMaterial;

        if (_hasDirectionalLightState && _directionalLight != null)
        {
            _directionalLight.color = _directionalLightColor;
            _directionalLight.intensity = _directionalLightIntensity;
        }

        if (_hasColorAdjustmentsState && _colorAdjustments != null)
        {
            _colorAdjustments.postExposure.overrideState = _postExposureOverrideState;
            _colorAdjustments.postExposure.value = _postExposure;
        }

        if (_hasVignetteState && _vignette != null)
        {
            _vignette.intensity.overrideState = _vignetteOverrideState;
            _vignette.intensity.value = _vignetteIntensity;
        }

        if (_hasDepthOfFieldState && _depthOfField != null)
        {
            _depthOfField.mode.overrideState = _dofModeOverrideState;
            _depthOfField.mode.value = _dofMode;
            _depthOfField.gaussianStart.overrideState = _dofGaussianStartOverrideState;
            _depthOfField.gaussianStart.value = _dofGaussianStart;
            _depthOfField.gaussianEnd.overrideState = _dofGaussianEndOverrideState;
            _depthOfField.gaussianEnd.value = _dofGaussianEnd;
            _depthOfField.gaussianMaxRadius.overrideState = _dofGaussianMaxRadiusOverrideState;
            _depthOfField.gaussianMaxRadius.value = _dofGaussianMaxRadius;
        }

        _hasCachedState = false;
    }
}