using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

public class TroublemakerController : MonoBehaviourPunCallbacks
{
    public PhotonView PhotonView { get; private set; }

    private bool HasAuthority => !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

    [Header("참조 컴포넌트")]
    [SerializeField] private NpcMovement _movement;
    [SerializeField] private TroublemakerSensor _sensor;
    [SerializeField] private TroublemakerAnimatorController _anim;
    [SerializeField] private MonoBehaviour _behaviourComponent;

    [Header("방해꾼 데이터")]
    [SerializeField] private TroublemakerDataSO _data;

    [Header("거리 유지 옵션")]
    [SerializeField] private float _separationMinRadius = 1.8f;
    [SerializeField] private float _separationMaxRadius = 2.2f;
    [SerializeField] private float _separationMinWeight = 1.2f;
    [SerializeField] private float _separationMaxWeight = 1.5f;
    [SerializeField] private LayerMask _troublemakerLayerMask = ~0;

    private ITroublemakerBehaviour _behaviour;
    private Transform _currentTarget;
    private Vector3 _homePosition;

    private float _homeArrivalDistance = 1.5f;
    private float _sampleMaxDistance = 2f;

    private float _separationRadius => Mathf.Lerp(_separationMinRadius, _separationMaxRadius, Random.value);
    private float _separationWeight => Mathf.Lerp(_separationMinWeight, _separationMaxWeight, Random.value);

    public TroublemakerDataSO Data => _data;
    public Vector3 HomePosition => _homePosition;
    public Transform CurrentTarget => _currentTarget;


    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();

        if (_movement == null)
        {
            _movement = GetComponent<NpcMovement>();
        }
        if (_sensor == null)
        {
            _sensor = GetComponent<TroublemakerSensor>();
        }
        if (_anim == null)
        {
            _anim = GetComponent<TroublemakerAnimatorController>();
        }

        _behaviour = _behaviourComponent as ITroublemakerBehaviour;
    }

    public void Initialize(TroublemakerDataSO data, Vector3 homePosition)
    {
        _data = data;
        _homePosition = homePosition;

        IMovementAnimator movementAnimator = _anim;

        if (_movement != null && _data != null)
        {
            _movement.SetOwner(HasAuthority);
            _movement.Initialize(
                movementAnimator,
                _data.WalkSpeed,
                _data.RunSpeed,
                _data.JumpDuration,
                _data.JumpHeight);
        }

        _behaviour?.Initialize(this);
    }

    private void Update()
    {
        if (!HasAuthority || _data == null) return;

        UpdateTarget();
        UpdateState();
        _behaviour?.Tick(Time.deltaTime);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (_movement != null)
        {
            _movement.SetOwner(HasAuthority);
        }

        if (!HasAuthority)
        {
            _currentTarget = null;
        }
    }

    private void UpdateTarget()
    {
        if (_sensor == null) return;

        if (_currentTarget == null)
        {
            _currentTarget = _sensor.FindNearestTarget(_data.DetectRange);
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        if (distance > _data.LoseTargetRange)
        {
            _currentTarget = null;
        }
    }

    private void UpdateState()
    {
        if (_currentTarget == null)
        {
            HandleIdleOrReturn();
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        if (distance > _data.StopDistance)
        {
            Vector3 desiredPosition = GetApproachPosition(_currentTarget.position);
            _movement?.MoveTo(desiredPosition, _data.StopDistance);
            _behaviour?.OnChase(_currentTarget);
        }
        else
        {
            _movement?.Stop();
            _behaviour?.OnReachTarget(_currentTarget);
        }
    }

    private void HandleIdleOrReturn()
    {
        float distanceToHome = Vector3.Distance(transform.position, _homePosition);

        if (distanceToHome > _homeArrivalDistance)
        {
            _movement.MoveTo(_homePosition, 0f);
            _behaviour?.OnReturnToHome();
        }
        else
        {
            _movement.Stop();
            _behaviour?.OnIdle();
        }
    }

    private Vector3 GetApproachPosition(Vector3 targetPosition)
    {
        Vector3 separation = CalculateSeparationOffset();
        Vector3 desired = targetPosition + separation;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, _sampleMaxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPosition;
    }

    private Vector3 CalculateSeparationOffset()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _separationRadius,
            _troublemakerLayerMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        int count = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            if (hits[i].gameObject == gameObject) continue;

            TroublemakerController other = hits[i].GetComponentInParent<TroublemakerController>();
            if (other == null || other == this) continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;

            float distance = away.magnitude;
            if (distance <= 0.001f) continue;

            float strength = 1f - Mathf.Clamp01(distance / _separationRadius);
            separation += away.normalized * strength;
            count++;
        }

        if (count == 0) return Vector3.zero;

        separation /= count;
        return separation * _separationWeight;
    }
}
