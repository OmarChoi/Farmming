using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraAbility : PlayerAbility
{
    [SerializeField] private Transform _cameraRoot;
    private const float MinVerticalAngle = -60f;
    private const float MaxVerticalAngle = 60f;
    private const float DefaultReturnSpeed = 10f;

    private float _mx;
    private float _my;

    private PresetState _preset;
    private Vector3 _defaultLocalPos;
    private Vector3 _currentOffset;

    private struct PresetState
    {
        public CameraPreset data;
        public Transform target;
        public float side; // +1 = 오른쪽, -1 = 왼쪽

        public bool IsActive => data != null;
        public bool HasTarget => target != null;
    }

    private void Start()
    {
        _defaultLocalPos = _cameraRoot.localPosition;
        _currentOffset = _defaultLocalPos;

        CinemachineCamera vcam = GameObject.Find("FollowCamera").GetComponent<CinemachineCamera>();
        vcam.Follow = _cameraRoot;
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
            _cameraRoot.localPosition = _currentOffset;
            _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
        }
    }

    // NPC 대화 등 — 타겟 방향으로 회전 + 측면 이동 + 줌
    private void ApplyTargetPreset()
    {
        float speed = _preset.data.TransitionSpeed;

        Vector3 toTarget = _preset.target.position - _owner.transform.position;
        toTarget.y = 0f;
        float rawTargetMx = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

        // Yaw: NPC 방향 + 반대쪽으로 살짝 회전 (둘 다 보이게)
        float yawOffset = _preset.side * _preset.data.RotationOffset.y;
        float targetMx = _mx + Mathf.DeltaAngle(_mx, rawTargetMx - yawOffset);
        // Pitch: 고정 각도 (음수 = 위에서 아래)
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

    // 인벤토리 등 — 위치 오프셋 + 회전 오프셋
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

    // 지정된 yaw 기준 오프셋을 플레이어 로컬 공간으로 변환하여 적용
    private void ApplyPositionOffset(Vector3 offset, float speed, float yaw)
    {
        Vector3 worldOffset = Quaternion.Euler(0f, yaw, 0f) * offset;
        Vector3 localOffset = _owner.transform.InverseTransformDirection(worldOffset);
        Vector3 targetLocalPos = _defaultLocalPos + localOffset;
        _currentOffset = Vector3.Lerp(_currentOffset, targetLocalPos, speed * Time.deltaTime);
        _cameraRoot.localPosition = _currentOffset;
    }
}