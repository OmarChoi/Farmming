using UnityEngine;

// TimeSystem 기준으로 Directional Light의 색/강도/회전을 조절한다
// - 색/강도 : SkyDatabase의 4개 키프레임(Night/Dawn/Day/Dusk)을 세그먼트 블렌딩
// - 회전   : 24시간을 360°로 매핑하여 정오를 X축 90°(수직) 기준으로 연속 회전
// 시각 전용이므로 모든 클라이언트에서 로컬로 동작한다 (RPC 불필요)
public class DirectionalLightController : MonoBehaviour
{
    private const int NoonMinutes = 12 * GameTime.MinutesPerHour;
    
    [Header("References")]
    [SerializeField] private Light _directionalLight;
    [SerializeField] private TimeSettingSO _timeSettings;
    [SerializeField] private SkyDatabase _skyDatabase;

    [Header("Rotation")]
    [Tooltip("태양의 방위각(남북 방향). 라이트의 Y 회전 기본값")]
    [SerializeField] private float _baseYaw = 30f;
    [Tooltip("라이트의 Z 회전(roll)")]
    [SerializeField] private float _baseRoll = 0f;


    private bool _originalCached;
    private Color _originalColor;
    private float _originalIntensity;
    private Quaternion _originalRotation;

    #region Lifecycle

    // 컴포넌트가 활성화될 때 원본 라이트 상태를 캐싱하고 TimeSystem 이벤트를 구독한다
    private void OnEnable()
    {
        // 원본 상태 캐싱에 실패하면(참조 누락 등) 이후 로직 진입을 차단
        if (!CacheOriginalState()) return;

        // 색/강도는 매 게임 분, 회전은 Update에서 프레임 단위로 처리
        TimeEvents.OnMinuteChanged += UpdateLightColor;
        RefreshNow();
    }

    // 컴포넌트 비활성화 시 구독 해제 및 원본 라이트 상태 복원
    private void OnDisable()
    {
        TimeEvents.OnMinuteChanged -= UpdateLightColor;
        RestoreOriginalState();
    }

    // 회전은 분 내부 진행도까지 반영해 매 프레임 연속으로 갱신한다
    private void Update()
    {
        if (_directionalLight == null || _timeSettings == null) return;
        GameTime currentTime = TimeEvents.CurrentDay > 0 ? TimeEvents.CurrentTime : _timeSettings.DefaultTime;
        UpdateLightRotation(currentTime);
    }

    // 외부에서 강제로 현재 시간 기준 라이트 상태를 재적용할 때 사용
    // TimeSystem이 아직 tick 되지 않았다면(CurrentDay == 0) 기본 시간으로 대체
    private void RefreshNow()
    {
        if (_timeSettings == null) return;
        GameTime currentTime = TimeEvents.CurrentDay > 0 ? TimeEvents.CurrentTime : _timeSettings.DefaultTime;
        UpdateLightColor(currentTime);
        UpdateLightRotation(currentTime);
    }

    #endregion

    #region Light Update

    // 색/강도 보간 : SkyboxController와 동일한 커브로 from→to 키프레임 사이를 블렌딩
    // OnMinuteChanged 이벤트로 매 게임 분마다 호출된다
    private void UpdateLightColor(GameTime time)
    {
        // 참조/세그먼트 유효성 확인 : 하나라도 없으면 갱신을 건너뛴다
        if (!SkySegmentResolver.TryResolve(time, _timeSettings, _skyDatabase,
                                           out SkyKeyframeSO from, out SkyKeyframeSO to, out float segmentProgress)) return;

        float t = SkySegmentResolver.EvaluateCurve(from.BlendCurve, segmentProgress);
        _directionalLight.color = Color.Lerp(from.LightColor, to.LightColor, t);
        _directionalLight.intensity = Mathf.Lerp(from.LightIntensity, to.LightIntensity, t);
    }

    // 회전 계산 : 정오(12:00)를 X=90°(수직 아래)로 두고 24시간 동안 연속 1회전
    // 06:00 ≈ 0°(지평선), 12:00 ≈ 90°(정오), 18:00 ≈ 180°(반대 지평선), 00:00 ≈ 270°(지평선 아래)
    // TimeSystem의 분 내부 진행도(0~1)를 더해 분 경계에서의 이산 점프를 제거한다
    private void UpdateLightRotation(GameTime time)
    {
        float minuteFraction = TimeSystem.Instance != null ? TimeSystem.Instance.CurrentMinuteProgress : 0f;
        float totalMinutes = time.TotalMinutes + minuteFraction;
        float pitch = ((totalMinutes - NoonMinutes) / GameTime.MinutesPerDay) * 360f + 90f;
        _directionalLight.transform.rotation = Quaternion.Euler(pitch, _baseYaw, _baseRoll);
    }

    #endregion

    #region State Backup

    // 첫 활성화 시점의 라이트 상태를 1회만 캐싱한다
    // 플레이 종료/컴포넌트 비활성화 시 원본 복원에 사용되므로 에디터에서 값이 튀는 것을 방지한다
    private bool CacheOriginalState()
    {
        if (_originalCached) return true;
        if (_directionalLight == null)
        {
            Debug.LogWarning($"[{nameof(DirectionalLightController)}] Directional Light 참조가 비어 있습니다.", this);
            return false;
        }

        _originalColor = _directionalLight.color;
        _originalIntensity = _directionalLight.intensity;
        _originalRotation = _directionalLight.transform.rotation;
        _originalCached = true;
        return true;
    }

    // 캐싱된 원본 상태로 라이트를 되돌린다
    // 캐싱 전이거나 라이트 참조가 사라진 경우 no-op
    private void RestoreOriginalState()
    {
        if (!_originalCached || _directionalLight == null) return;

        _directionalLight.color = _originalColor;
        _directionalLight.intensity = _originalIntensity;
        _directionalLight.transform.rotation = _originalRotation;
    }

    #endregion
}
