using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraAbility : PlayerAbility
{
    [SerializeField] private Transform _cameraRoot;
    private const float MinVerticalAngle = -60f;
    private const float MaxVerticalAngle = 60f;

    private float _mx;
    private float _my;

    private CameraPreset _currentPreset;
    private Transform _presetTarget;
    private float _presetSide;
    private Vector3 _defaultLocalPos;
    private Vector3 _currentOffset;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _defaultLocalPos = _cameraRoot.localPosition;
        _currentOffset = _defaultLocalPos;

        CinemachineCamera vcam = GameObject.Find("FollowCamera").GetComponent<CinemachineCamera>();
        vcam.Follow = _cameraRoot;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }

    public void SetPreset(CameraPreset preset, Transform target = null)
    {
        _currentPreset = preset;
        _presetTarget = target;

        if (target != null)
        {
            // NPC가 카메라 기준 왼쪽인지 오른쪽인지 판별
            Vector3 toTarget = target.position - _owner.transform.position;
            toTarget.y = 0f;
            Vector3 camForward = Quaternion.Euler(0f, _mx, 0f) * Vector3.forward;
            float cross = camForward.x * toTarget.z - camForward.z * toTarget.x;
            _presetSide = cross < 0f ? 1f : -1f; // +1 = 오른쪽, -1 = 왼쪽
        }
    }

    public void ClearPreset()
    {
        _currentPreset = null;
        _presetTarget = null;
    }

    private void LateUpdate()
    {
        if (_owner.CanMove)
        {
            _mx += Input.GetAxis("Mouse X") * _owner.StatSo.MouseSensitivity * Time.deltaTime;
            _my += Input.GetAxis("Mouse Y") * _owner.StatSo.MouseSensitivity * Time.deltaTime;
            _my = Mathf.Clamp(_my, MinVerticalAngle, MaxVerticalAngle);
        }

        if (_currentPreset != null)
        {
            ApplyPreset();
        }
        else
        {
            _currentOffset = Vector3.Lerp(_currentOffset, _defaultLocalPos, 10f * Time.deltaTime);
            _cameraRoot.localPosition = _currentOffset;
            _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
        }
    }

    private void ApplyPreset()
    {
        float speed = _currentPreset.TransitionSpeed;

        if (_presetTarget != null)
        {
            // 타겟 기준 (NPC 대화 등)
            Vector3 toTarget = _presetTarget.position - _owner.transform.position;
            toTarget.y = 0f;
            float rawTargetMx = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            // Yaw: NPC 방향 + 반대쪽으로 살짝 회전 (둘 다 보이게)
            float yawOffset = _presetSide * _currentPreset.RotationOffset.y;
            float targetMx = _mx + Mathf.DeltaAngle(_mx, rawTargetMx - yawOffset);
            // Pitch: 고정 각도 (음수 = 위에서 아래)
            float targetMy = _currentPreset.RotationOffset.x;

            _mx = Mathf.Lerp(_mx, targetMx, speed * Time.deltaTime);
            _my = Mathf.Lerp(_my, targetMy, speed * Time.deltaTime);

            // 위치: NPC 쪽으로 측면 이동 + 줌
            Vector3 offset = new Vector3(_presetSide * _currentPreset.PositionOffset.x, _currentPreset.PositionOffset.y, _currentPreset.PositionOffset.z);
            Vector3 worldOffset = Quaternion.Euler(0f, _mx, 0f) * offset;
            Vector3 localOffset = _owner.transform.InverseTransformDirection(worldOffset);
            Vector3 targetLocalPos = _defaultLocalPos + localOffset;

            _currentOffset = Vector3.Lerp(_currentOffset, targetLocalPos, speed * Time.deltaTime);
            _cameraRoot.localPosition = _currentOffset;
            _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
        }
        else
        {
            // 플레이어 기준 (인벤토리 등) — 카메라 yaw 기준 월드 오프셋 → 플레이어 로컬로 변환
            Vector3 worldOffset = Quaternion.Euler(0f, _mx, 0f) * _currentPreset.PositionOffset;
            Vector3 localOffset = _owner.transform.InverseTransformDirection(worldOffset);
            Vector3 targetLocalPos = _defaultLocalPos + localOffset;
            _currentOffset = Vector3.Lerp(_currentOffset, targetLocalPos, speed * Time.deltaTime);
            _cameraRoot.localPosition = _currentOffset;

            Quaternion targetRot = Quaternion.Euler(-_my + _currentPreset.RotationOffset.x,
                _mx + _currentPreset.RotationOffset.y, _currentPreset.RotationOffset.z);
            _cameraRoot.rotation = Quaternion.Slerp(_cameraRoot.rotation, targetRot, speed * Time.deltaTime);
        }
    }
}