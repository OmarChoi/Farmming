
public enum EMemoryUpdatePolicy
{
    Override,       // 기존 삭제하고 새로 저장
    Accumulate,     // 계속 축적
    Refresh,        // 있으면 갱신, 없으면 추가
    Decay           // 시간이 지나면 약해짐
}
