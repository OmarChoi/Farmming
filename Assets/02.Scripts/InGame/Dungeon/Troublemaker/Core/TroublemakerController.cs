using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.AI;

public class TroublemakerController : MonoBehaviourPunCallbacks
{
    public PhotonView PhotonView { get; private set; }

    private bool HasAuthority => !PhotonNetwork.IsConnected || (PhotonView != null && PhotonView.IsMine);

    [Header("참조 컴포넌트")]
    [SerializeField] private NpcMovement _movement;
    [SerializeField] private TroublemakerSensor _sensor;
    [SerializeField] private TroublemakerAnimatorController _anim;

    [Header("방해꾼 데이터")]
    [SerializeField] private TroublemakerDataSO _data;

    [Header("거리 유지 옵션")]
    [SerializeField] private float _minSeparationRadius = 1.8f;
    [SerializeField] private float _maxSeparationRadius = 2.2f;
    [SerializeField] private float _minSeparationWeight = 1.2f;
    [SerializeField] private float _maxSeparationWeight = 1.5f;
    [SerializeField] private LayerMask _troublemakerLayerMask = ~0;

    private ITroublemakerBehaviour _behaviour;
    private Transform _currentTarget;
    private Vector3 _homePosition;

    private float _homeArrivalDistance = 1.5f;
    private float _sampleMaxDistance = 2f;

    private bool _hasDetectedTarget;
    private bool _isDetectingTarget;

    private float _separationRadius => Mathf.Lerp(_minSeparationRadius, _maxSeparationRadius, UnityEngine.Random.value);
    private float _separationWeight => Mathf.Lerp(_minSeparationWeight, _maxSeparationWeight, UnityEngine.Random.value);

    public TroublemakerDataSO Data => _data;
    public Vector3 HomePosition => _homePosition;
    public Transform CurrentTarget => _currentTarget;
    public NpcMovement Movement => _movement;
    public TroublemakerAnimatorController Anim => _anim;


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
        _behaviour = GetComponent<TroublemakerBehaviourBase>();
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
        _movement.OnJumpStarted += OnMovementJumpStarted;
        _behaviour?.Initialize(this);
    }

    private void Update()
    {
        if (!HasAuthority || _data == null) return;

        UpdateTarget();
        UpdateState();
        _behaviour?.Tick(Time.deltaTime);
    }
    private void OnDestroy()
    {
        if (_movement != null)
        {
            _movement.OnJumpStarted -= OnMovementJumpStarted;
        }
    }

    private void OnMovementJumpStarted(Vector3 startPos, Vector3 endPos, float duration)
    {
        if (!PhotonNetwork.IsConnected || PhotonView == null) return;
        PhotonView.RPC(nameof(RPC_PlayJump), RpcTarget.Others, startPos, endPos, duration);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient && PhotonView != null && !PhotonView.IsMine)
        {
            PhotonView.RequestOwnership();
        }

        bool isOwner = HasAuthority;
        if (_movement != null) _movement.SetOwner(isOwner);
        if (!isOwner)
        {
            _currentTarget = null;
            _hasDetectedTarget = false;
            _isDetectingTarget = false;
        }
    }

    private void UpdateTarget()
    {
        if (_sensor == null) return;

        if (_currentTarget == null)
        {
            Transform newTarget = _sensor.FindNearestTarget(_data.DetectRange);

            if (newTarget != null)
            {
                _currentTarget = newTarget;

                if (!_hasDetectedTarget && !_isDetectingTarget)
                {
                    StartDetectAsync().Forget();
                }
            }

            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        if (distance > _data.LoseTargetRange)
        {
            _currentTarget = null;
            _hasDetectedTarget = false;
            _isDetectingTarget = false;
        }
    }

    private async UniTaskVoid StartDetectAsync()
    {
        _isDetectingTarget = true;
        _movement?.Stop();

        try
        {
            if (_currentTarget != null && _movement != null)
            {
                await _movement.FaceTargetAsync(_currentTarget.position);
            }

            PlayDetectAll();

            await UniTask.Delay(
                TimeSpan.FromSeconds(_anim.DetectDuration),
                cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _hasDetectedTarget = true;
        _isDetectingTarget = false;
    }

    private void UpdateState()
    {
        if (_movement != null && _movement.IsJumping) return;

        if (_isDetectingTarget)
        {
            _movement?.Stop();
            return;
        }

        if (_currentTarget == null)
        {
            HandleIdleOrReturn();
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        if (distance > _data.StopDistance)
        {
            Vector3 desiredPosition = GetApproachPosition(_currentTarget.position);
            _movement?.MoveTo(desiredPosition, _data.StopDistance, true);
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

    private void PlayDetectAll()
    {
        _anim?.PlayDetect();

        if (HasAuthority && PhotonNetwork.IsConnected && PhotonView != null)
        {
            PhotonView.RPC(nameof(RPC_PlayDetect), RpcTarget.Others);
        }
    }

    public void PlayTroubleAll()
    {
        _anim?.PlayTrouble();

        if (HasAuthority && PhotonNetwork.IsConnected && PhotonView != null)
        {
            PhotonView.RPC(nameof(RPC_PlayTrouble), RpcTarget.Others);
        }
    }

    [PunRPC]
    private void RPC_PlayDetect()
    {
        _anim?.PlayDetect();
    }

    [PunRPC]
    private void RPC_PlayTrouble()
    {
        _anim?.PlayTrouble();
    }

    [PunRPC]
    private void RPC_PlayJump(Vector3 startPos, Vector3 endPos, float duration)
    {
        _movement?.PlayRemoteJump(startPos, endPos, duration);
    }
}
