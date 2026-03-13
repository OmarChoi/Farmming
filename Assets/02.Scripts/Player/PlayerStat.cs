using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStat", menuName = "Data/PlayerStat")]
public class PlayerStat : ScriptableObject
{
    [Header("체력")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("스태미너")]
    [SerializeField] private float _maxStamina = 100f;

    [Header("이동")]
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _runSpeed = 6f;
    [SerializeField] private float _jumpPower = 5f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("카메라")]
    [SerializeField] private float _mouseSensitivity = 200f;

    public float MaxHealth => _maxHealth;
    public float MaxStamina => _maxStamina;
    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;
    public float JumpPower => _jumpPower;
    public float RotationSpeed => _rotationSpeed;
    public float MouseSensitivity => _mouseSensitivity;
}