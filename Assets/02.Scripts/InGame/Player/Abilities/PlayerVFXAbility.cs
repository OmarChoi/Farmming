using UnityEngine;

public class PlayerVFXAbility : PlayerAbility
{
    [Header("Footstep Dust")]
    [SerializeField] private ParticleSystem _dustPrefab;
    [SerializeField] private Transform _leftFoot;
    [SerializeField] private Transform _rightFoot;
    [SerializeField] private float _walkScale = 2f;
    [SerializeField] private float _runScale = 3f;

    private ParticleSystem _leftDust;
    private ParticleSystem _rightDust;
    private PlayerMoveAbility _move;

    private void Start()
    {
        if (_dustPrefab == null) return;

        _move = _owner.GetAbility<PlayerMoveAbility>();
        _leftDust = CreateDust(_leftFoot);
        _rightDust = CreateDust(_rightFoot);
    }

    public void PlayFootstepDust(bool isLeft)
    {
        if (_move == null || !_move.IsMoving) return;

        var dust = isLeft ? _leftDust : _rightDust;
        if (dust == null) return;

        float scale = _move.IsSprinting ? _runScale : _walkScale;
        dust.transform.localScale = new Vector3(scale, scale, scale);

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