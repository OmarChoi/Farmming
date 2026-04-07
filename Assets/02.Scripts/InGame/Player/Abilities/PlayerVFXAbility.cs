using UnityEngine;

public class PlayerVFXAbility : PlayerAbility
{
    [Header("Footstep Dust")]
    [SerializeField] private ParticleSystem _dustPrefab;
    [SerializeField] private Transform _leftFoot;
    [SerializeField] private Transform _rightFoot;
    [SerializeField] private float _walkScale = 2f;
    [SerializeField] private float _runScale = 3f;

    private static readonly int MoveHash = Animator.StringToHash("Move");

    private ParticleSystem _leftDust;
    private ParticleSystem _rightDust;
    private Animator _animator;

    private void Start()
    {
        if (_dustPrefab == null) return;

        _animator = _owner.GetComponentInChildren<Animator>();
        _leftDust = CreateDust(_leftFoot);
        _rightDust = CreateDust(_rightFoot);
    }

    public void PlayFootstepDust(bool isLeft)
    {
        if (_animator == null) return;

        float moveValue = _animator.GetFloat(MoveHash);
        if (moveValue < 0.1f) return;

        var dust = isLeft ? _leftDust : _rightDust;
        if (dust == null) return;

        float scale = moveValue > 0.75f ? _runScale : _walkScale;
        dust.transform.localScale = Vector3.one * scale;

        dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        dust.Play();
    }

    private ParticleSystem CreateDust(Transform parent)
    {
        if (parent == null) return null;
        var dust = Instantiate(_dustPrefab, parent);
        dust.transform.localPosition = Vector3.zero;
        dust.gameObject.SetActive(true);
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return dust;
    }
}