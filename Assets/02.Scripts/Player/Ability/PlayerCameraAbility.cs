using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraAbility : PlayerAbility
{
    [SerializeField] private Transform _cameraRoot;

    private float _mx;
    private float _my;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

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

    private void LateUpdate()
    {
        _mx += Input.GetAxis("Mouse X") * _owner.Stat.MouseSensitivity * Time.deltaTime;
        _my += Input.GetAxis("Mouse Y") * _owner.Stat.MouseSensitivity * Time.deltaTime;

        _my = Mathf.Clamp(_my, -60f, 60f);

        _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
    }
}