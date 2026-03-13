using UnityEngine;

public class PlayerRotateAbility : PlayerAbility
{
    [SerializeField] private Transform _cameraRoot;

    private float _mx;
    private float _my;

    private void LateUpdate()
    {
        _mx += Input.GetAxis("Mouse X") * _owner.Stat.RotationSpeed * Time.deltaTime;
        _my += Input.GetAxis("Mouse Y") * _owner.Stat.RotationSpeed * Time.deltaTime;

        _my = Mathf.Clamp(_my, -60f, 60f);

        _cameraRoot.rotation = Quaternion.Euler(-_my, _mx, 0f);
    }
}