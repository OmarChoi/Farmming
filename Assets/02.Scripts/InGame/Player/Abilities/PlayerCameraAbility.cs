using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCameraAbility : PlayerAbility
{
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private float _impactShakeDuration = 0.18f;
    [SerializeField] private float _impactShakeStrength = 0.15f;
    [SerializeField] private int _impactShakeVibrato = 18;
    private const float MinVerticalAngle = -60f;
    private const float MaxVerticalAngle = 10f;
    private const float DefaultReturnSpeed = 10f;

    private float _mx;
    private float _my;

    private PresetState _preset;
    private Vector3 _defaultLocalPos;
    private Vector3 _currentOffset;
    private Vector3 _shakeOffset;
    private Tween _shakeTween;

    private struct PresetState
    {
        public CameraPreset data;
        public Transform target;
        public float side;

        public bool IsActive => data != null;
        public bool HasTarget => target != null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _shakeTween?.Kill();
        _shakeOffset = Vector3.zero;
    }

    private void Start()
    {
        _defaultLocalPos = _cameraRoot.localPosition;
        _currentOffset = _defaultLocalPos;
        BindFollowCamera();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindFollowCamera();
    }

    private void BindFollowCamera()
    {
        if (!_owner.IsMine) return;
        if (_cameraRoot == null) return;

        var followCamObj = GameObject.Find("FollowCamera");
        if (followCamObj == null) return;

        var cinemachineCamera = followCamObj.GetComponent<CinemachineCamera>();
        if (cinemachineCamera != null)
            cinemachineCamera.Follow = _cameraRoot;
    }

    public void RebindFollowCamera()
    {
        if (!_owner.IsMine) return;
        if (_cameraRoot == null) return;

        var followCamObj = GameObject.Find("FollowCamera");
        if (followCamObj == null) return;

        var cinemachineCamera = followCamObj.GetComponent<CinemachineCamera>();
        if (cinemachineCamera == null) return;

        cinemachineCamera.Follow = null;
        cinemachineCamera.Follow = _cameraRoot;
        cinemachineCamera.PreviousStateIsValid = false;
    }

    public void SuspendFollowCamera()
    {
        if (!_owner.IsMine) return;

        var followCamObj = GameObject.Find("FollowCamera");
        if (followCamObj == null) return;

        var cinemachineCamera = followCamObj.GetComponent<CinemachineCamera>();
        if (cinemachineCamera == null) return;

        cinemachineCamera.Follow = null;
        cinemachineCamera.PreviousStateIsValid = false;
    }

    public void SetPreset(CameraPreset preset, Transform target = null)
    {
        _preset.data = preset;
        _preset.target = target;

        if (target != null)
        {
            Vector3 toTarget = target.position - _owner.transform.position;
            toTarget.y = 0f;
            Vector3 camForward = Quaternion.Euler(0f, _mx, 0f) * Vector3.forward;
            float cross = camForward.x * toTarget.z - camForward.z * toTarget.x;
            _preset.side = cross < 0f ? 1f : -1f;
        }
    }

    public void ClearPreset()
    {
        _preset = default;
    }

    private void LateUpdate()
    {
        if (!_owner.IsMine) return;

        if (_owner.CanRotateCamera)
        {
            _mx += Input.GetAxis("Mouse X") * _owner.StatSo.MouseSensitivity * Time.deltaTime;
            _my += Input.GetAxis("Mouse Y") * _owner.StatSo.MouseSensitivity * Time.deltaTime;
            _my = Mathf.Clamp(_my, MinVerticalAngle, MaxVerticalAngle);
        }

        if (_preset.IsActive)
        {
            if (_preset.HasTarget)
                ApplyTargetPreset();
            else
                ApplyLocalPreset();
        }
        else
        {
            _currentOffset = Vector3.Lerp(_currentOffset, _defaultLocalPos, DefaultReturnSpeed * Time.deltaTime);
            _cameraRoot.localPosition = _currentOffset + _shakeOffset;
            _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
        }
    }

    private void ApplyTargetPreset()
    {
        float speed = _preset.data.TransitionSpeed;

        Vector3 toTarget = _preset.target.position - _owner.transform.position;
        toTarget.y = 0f;
        float rawTargetMx = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

        float yawOffset = _preset.side * _preset.data.RotationOffset.y;
        float targetMx = _mx + Mathf.DeltaAngle(_mx, rawTargetMx - yawOffset);
        float targetMy = _preset.data.RotationOffset.x;

        _mx = Mathf.Lerp(_mx, targetMx, speed * Time.deltaTime);
        _my = Mathf.Lerp(_my, targetMy, speed * Time.deltaTime);

        Vector3 offset = new Vector3(
            _preset.side * _preset.data.PositionOffset.x,
            _preset.data.PositionOffset.y,
            _preset.data.PositionOffset.z);
        ApplyPositionOffset(offset, speed, _mx);
        _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
    }

    private void ApplyLocalPreset()
    {
        float speed = _preset.data.TransitionSpeed;

        ApplyPositionOffset(_preset.data.PositionOffset, speed, _mx);

        Quaternion targetRot = Quaternion.Euler(
            -_my + _preset.data.RotationOffset.x,
            _mx + _preset.data.RotationOffset.y,
            _preset.data.RotationOffset.z);
        _cameraRoot.rotation = Quaternion.Slerp(_cameraRoot.rotation, targetRot, speed * Time.deltaTime);
    }

    private void ApplyPositionOffset(Vector3 offset, float speed, float yaw)
    {
        Vector3 worldOffset = Quaternion.Euler(0f, yaw, 0f) * offset;
        Vector3 localOffset = _owner.transform.InverseTransformDirection(worldOffset);
        Vector3 targetLocalPos = _defaultLocalPos + localOffset;
        _currentOffset = Vector3.Lerp(_currentOffset, targetLocalPos, speed * Time.deltaTime);
        _cameraRoot.localPosition = _currentOffset + _shakeOffset;
    }

    public void SnapYawToPlayerFrontView()
    {
        if (!_owner.IsMine) return;
        _mx = Mathf.Repeat(_owner.transform.eulerAngles.y + 180f, 360f);
    }

    public void PlayImpactShake(float strengthMultiplier = 1f)
    {
        if (!_owner.IsMine) return;
        if (_cameraRoot == null) return;

        _shakeTween?.Kill();
        _shakeOffset = Vector3.zero;

        Vector3 strength = Vector3.one * (_impactShakeStrength * strengthMultiplier);
        _shakeTween = DOTween.Shake(
                () => _shakeOffset,
                value => _shakeOffset = value,
                _impactShakeDuration,
                strength,
                _impactShakeVibrato,
                90f,
                false,
                ShakeRandomnessMode.Harmonic)
            .SetUpdate(UpdateType.Normal)
            .OnKill(() => _shakeOffset = Vector3.zero);
    }
}
