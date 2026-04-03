using UnityEngine;

/// <summary>
/// Helper 업그레이드 테스트용. 씬에 빈 오브젝트 만들고 이 컴포넌트 붙인 뒤
/// Inspector에서 PlayerHelperInteractionAbility 연결하면 됨.
/// </summary>
public class TestHelperUpgrade : MonoBehaviour
{
    [SerializeField] private PlayerHelperInteractionAbility _playerHelperInteraction;

    [Header("테스트 키")]
    [SerializeField] private KeyCode _fillExpKey = KeyCode.U;           // 경험치 최대 채우기
    [SerializeField] private KeyCode _upgradeKey = KeyCode.I;           // 업그레이드 실행
    [SerializeField] private KeyCode _addExpKey = KeyCode.J;            // 경험치 소량 추가
    [SerializeField] private KeyCode _printStatusKey = KeyCode.K;       // 현재 상태 콘솔 출력
    [SerializeField] private KeyCode _setNormalKey = KeyCode.Alpha7;    // 등급 Normal 강제 설정
    [SerializeField] private KeyCode _setEpicKey = KeyCode.Alpha8;      // 등급 Epic 강제 설정
    [SerializeField] private KeyCode _setLegendaryKey = KeyCode.Alpha9; // 등급 Legendary 강제 설정

    [Header("J키 경험치 추가량")]
    [SerializeField] private int _expAddAmount = 100;

    [Header("테스트 대상")]
    [Tooltip("true면 BackHelper(light helper) 대상, false면 CurrentHelper(일반 helper) 대상")]
    [SerializeField] private bool _targetBackHelper = false;

    private HelperController GetTarget()
    {
        if (_playerHelperInteraction == null) return null;
        return _targetBackHelper
            ? _playerHelperInteraction.BackHelper
            : _playerHelperInteraction.CurrentHelper;
    }

    private void Update()
    {
        if (Input.GetKeyDown(_fillExpKey))      FillExp();
        if (Input.GetKeyDown(_upgradeKey))      DoUpgrade();
        if (Input.GetKeyDown(_addExpKey))       AddExp();
        if (Input.GetKeyDown(_printStatusKey))  PrintStatus();
        if (Input.GetKeyDown(_setNormalKey))    SetGrade(EHelperGrade.Normal);
        if (Input.GetKeyDown(_setEpicKey))      SetGrade(EHelperGrade.Epic);
        if (Input.GetKeyDown(_setLegendaryKey)) SetGrade(EHelperGrade.Legendary);
    }

    private void FillExp()
    {
        var helper = GetTarget();
        if (helper == null) { Debug.LogWarning("[HelperUpgradeTest] 소환된 helper 없음"); return; }

        helper.Experience.Add(helper.Experience.MaxExp);
        Debug.Log($"[HelperUpgradeTest] 경험치 최대 채움 → {helper.Experience.CurrentExp}/{helper.Experience.MaxExp}  업그레이드 가능: {helper.Experience.IsReadyToUpgrade}");
    }

    private void DoUpgrade()
    {
        var helper = GetTarget();
        if (helper == null) { Debug.LogWarning("[HelperUpgradeTest] 소환된 helper 없음"); return; }

        if (!helper.Experience.IsReadyToUpgrade)
        {
            Debug.LogWarning($"[HelperUpgradeTest] 업그레이드 불가 — 경험치 부족 또는 최고 등급 ({helper.Grade.CurrentGrade})");
            return;
        }

        helper.PerformUpgrade();
        Debug.Log($"[HelperUpgradeTest] 업그레이드 완료 → 등급: {helper.Grade.CurrentGrade}  범위: {helper.Grade.GetRange()}");
    }

    private void AddExp()
    {
        var helper = GetTarget();
        if (helper == null) { Debug.LogWarning("[HelperUpgradeTest] 소환된 helper 없음"); return; }

        helper.Experience.Add(_expAddAmount);
        Debug.Log($"[HelperUpgradeTest] 경험치 +{_expAddAmount} → {helper.Experience.CurrentExp}/{helper.Experience.MaxExp}");
    }

    private void PrintStatus()
    {
        var helper = GetTarget();
        if (helper == null) { Debug.LogWarning("[HelperUpgradeTest] 소환된 helper 없음"); return; }

        Debug.Log($"[HelperUpgradeTest] === {helper.HelperId} 상태 ===\n" +
                  $"등급: {helper.Grade.CurrentGrade}  범위: {helper.Grade.GetRange()}\n" +
                  $"경험치: {helper.Experience.CurrentExp}/{helper.Experience.MaxExp}\n" +
                  $"업그레이드 가능: {helper.Experience.IsReadyToUpgrade}");
    }

    private void SetGrade(EHelperGrade grade)
    {
        var helper = GetTarget();
        if (helper == null) { Debug.LogWarning("[HelperUpgradeTest] 소환된 helper 없음"); return; }

        helper.Grade.CurrentGrade = grade;
        helper.Experience.Reset();
        Debug.Log($"[HelperUpgradeTest] 등급 강제 설정 → {grade}  범위: {helper.Grade.GetRange()}");
    }
}
