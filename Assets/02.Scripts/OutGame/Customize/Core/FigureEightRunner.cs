using UnityEngine;

/// <summary>
/// 커스터마이즈 씬 연출용. 대상 오브젝트를 Lemniscate(∞) 경로로 순회시킨다.
/// Leader 모드는 독자 경로, Chaser 모드는 Leader의 과거 위치를 쫓아가며 상하 플랩을 더한다.
/// </summary>
public class FigureEightRunner : MonoBehaviour
{
    public enum EMode { Leader, Chaser }

    [Header("공통")]
    [SerializeField] private EMode _mode = EMode.Leader;
    [SerializeField] private Transform _center;
    [SerializeField] private float _speed = 1.2f;
    [SerializeField] private float _turnSpeed = 540f;

    [Header("Leader 경로")]
    [SerializeField] private Vector2 _size = new Vector2(6f, 3f); // X, Z 반경

    [Header("Chaser 추격")]
    [SerializeField] private FigureEightRunner _leader;
    [SerializeField] private float _chaseDelay = 0.4f;
    [SerializeField] private float _flyHeight = 1.4f;
    [SerializeField] private float _flapAmplitude = 0.25f;
    [SerializeField] private float _flapSpeed = 6f;

    private float _elapsed;
    private Vector3 _origin;

    // Chaser가 Leader의 과거 위치를 샘플링하기 위한 링버퍼
    private const int HistoryCapacity = 240;
    private readonly Vector3[] _history = new Vector3[HistoryCapacity];
    private readonly float[] _historyTime = new float[HistoryCapacity];
    private int _historyHead;
    private int _historyCount;

    private void Awake()
    {
        _origin = _center != null ? _center.position : transform.position;
    }

    // Animator의 본 평가는 Update 이후 LateUpdate 이전에 수행된다.
    // Update에서 transform을 수정하면 같은 프레임에 Animator가 루트를 덮어써 회전이 고정될 수 있어
    // 이동·회전 적용은 LateUpdate에서 수행한다.
    private void LateUpdate()
    {
        _elapsed += Time.deltaTime * _speed;

        if (_mode == EMode.Leader)
        {
            UpdateLeader();
            RecordHistory();
        }
        else
        {
            UpdateChaser();
        }
    }

    private void UpdateLeader()
    {
        Vector3 next = SampleLemniscate(_elapsed);
        ApplyMove(next);
    }

    private Vector3 SampleLemniscate(float t)
    {
        // x = A * sin(t), z = (B/2) * sin(2t)  →  원점 교차 ∞ 곡선
        float x = _size.x * Mathf.Sin(t);
        float z = (_size.y * 0.5f) * Mathf.Sin(2f * t);
        return _origin + new Vector3(x, 0f, z);
    }

    private void ApplyMove(Vector3 nextPos)
    {
        Vector3 dir = nextPos - transform.position;
        transform.position = nextPos;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * Time.deltaTime);
        }
    }

    private void RecordHistory()
    {
        _history[_historyHead] = transform.position;
        _historyTime[_historyHead] = Time.time;
        _historyHead = (_historyHead + 1) % HistoryCapacity;
        if (_historyCount < HistoryCapacity) _historyCount++;
    }

    private void UpdateChaser()
    {
        if (_leader == null) return;

        Vector3 targetGround = _leader.SamplePastPosition(_chaseDelay);
        float flap = Mathf.Sin(Time.time * _flapSpeed) * _flapAmplitude;
        Vector3 target = targetGround + new Vector3(0f, _flyHeight + flap, 0f);

        ApplyMove(target);
    }

    /// <summary>
    /// delay초 전의 위치를 반환. 데이터 부족 시 현재 위치로 폴백.
    /// </summary>
    public Vector3 SamplePastPosition(float delay)
    {
        if (_historyCount == 0) return transform.position;

        float targetTime = Time.time - delay;
        for (int i = 1; i <= _historyCount; i++)
        {
            int idx = (_historyHead - i + HistoryCapacity) % HistoryCapacity;
            if (_historyTime[idx] <= targetTime) return _history[idx];
        }
        int oldest = (_historyHead - _historyCount + HistoryCapacity) % HistoryCapacity;
        return _history[oldest];
    }

    private void OnDrawGizmosSelected()
    {
        if (_mode != EMode.Leader) return;

        Vector3 c = _center != null ? _center.position : (Application.isPlaying ? _origin : transform.position);
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        const int segments = 96;
        Vector3 prev = c + new Vector3(0f, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(_size.x * Mathf.Sin(t), 0f, (_size.y * 0.5f) * Mathf.Sin(2f * t));
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}