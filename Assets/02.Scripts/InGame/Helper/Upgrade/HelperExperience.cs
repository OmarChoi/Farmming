using System;

// 현재 경험치 저장
// 현재 등급에 맞는 최대 경험치 계산
// 업그레이드 가능한 상태인지 판단
// 경험치 추가
// 저장된 경험치 불러오기
// 업그레이드 후 경험치 초기화
// 경험치 변경 / 업그레이드 가능 상태를 이벤트 알림
public class HelperExperience
{
    private readonly HelperDataSO _data;
    private readonly HelperGrade _grade;

    public int CurrentExp { get; private set; }
    public int MaxExp => MaxExpForGrade(_grade.CurrentGrade);
    public bool IsReadyToUpgrade => _grade.CurrentGrade != EHelperGrade.Legendary && CurrentExp >= MaxExp;

    public event Action<int, int> OnExpChanged;
    public event Action OnReadyToUpgrade; // 업그레이드 가능 연출 재생 가능

    public HelperExperience(HelperDataSO data, HelperGrade grade)
    {
        _data = data;
        _grade = grade;
    }

    public void Add(int amount)
    {
        if (_grade.CurrentGrade == EHelperGrade.Legendary)
        {
            return;
        }
        if (IsReadyToUpgrade)
        {
            return;
        }

        CurrentExp = Math.Min(CurrentExp + amount, MaxExp);
        OnExpChanged?.Invoke(CurrentExp, MaxExp);

        if (IsReadyToUpgrade)
        { 
            OnReadyToUpgrade?.Invoke();
            // UI에 "업그레이드 가능" 표시 띄울 예정
        }
    }

    public void Load(int savedExp)
    {
        CurrentExp = Math.Min(savedExp, MaxExp);
    }

    // 대장간에서 등급업 완료 후 호출
    public void Reset()
    {
        CurrentExp = 0;
        OnExpChanged?.Invoke(CurrentExp, MaxExp);
    }

    private int MaxExpForGrade(EHelperGrade grade) => grade switch
    {
        EHelperGrade.Normal => _data.NormalMaxExp,
        EHelperGrade.Epic   => _data.EpicMaxExp,
        _                   => int.MaxValue
    };
}
