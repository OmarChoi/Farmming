using UnityEngine;
using System;
using System.Collections.Generic;

public class NpcFriendshipManager : MonoBehaviour
{
    public static NpcFriendshipManager Instance { get; private set; }

    [SerializeField] private NpcFriendshipSettings _friendshipSettings;

    private const float DISPLAY_SCALE = 10f;

    private readonly Dictionary<string, int> _friendshipByNpcId = new();
    private readonly HashSet<string> _greetedNpcIdsToday = new();

    public event Action<string, int, int, ENpcFriendshipReason> OnFriendshipChanged;

    public int MaxFriendship => _friendshipSettings.MaxFriendship;
    public int DefaultFriendship => _friendshipSettings.DefaultFriendship;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        TimeEvents.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        TimeEvents.OnDayChanged -= HandleDayChanged;
    }

    public ENpcFriendshipStep GetFriendshipStep(int friendship)
    {
        friendship = ClampFriendship(friendship);

        if (friendship >= _friendshipSettings.BestThreshold)
        {
            return ENpcFriendshipStep.Best;
        }
        if (friendship >= _friendshipSettings.TrustedThreshold)
        {
            return ENpcFriendshipStep.Trusted;
        }
        if (friendship >= _friendshipSettings.FriendlyThreshold)
        {
            return ENpcFriendshipStep.Friendly;
        }
        if (friendship >= _friendshipSettings.InterestedThreshold)
        {
            return ENpcFriendshipStep.Interested;
        }
        if (friendship >= _friendshipSettings.AcquaintedThreshold)
        {
            return ENpcFriendshipStep.Acquainted;
        }
        return ENpcFriendshipStep.Awkward;
    }

    // 내부 데이터 값인 int를 외부 표시 또는 입력 값인 float로 변환해주는 메서드입니다.
    public float ToDisplayValue(int friendship)
    {
        friendship = ClampFriendship(friendship);
        return friendship / DISPLAY_SCALE;
    }

    // 외부 표시 또는 입력 값인 float를 내부 데이터 값인 int로 변환해주는 메서드입니다.
    public int ToInternalValue(float displayValue)
    {
        return Mathf.RoundToInt(displayValue * DISPLAY_SCALE);
    }

    public int GetFriendship(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return DefaultFriendship;

        return _friendshipByNpcId.TryGetValue(npcId, out int value)
            ? value
            : DefaultFriendship;
    }

    public void AddFriendship(string npcId, int amount, ENpcFriendshipReason reason = ENpcFriendshipReason.Event)
    {
        if (string.IsNullOrEmpty(npcId) || amount == 0) return;

        int oldValue = GetFriendship(npcId);
        int newValue = ClampFriendship(oldValue + amount);

        _friendshipByNpcId[npcId] = newValue;
        OnFriendshipChanged?.Invoke(npcId, oldValue, newValue, reason);

#if UNITY_EDITOR
        Debug.Log($"NPC {npcId} 친밀도 변화 {amount} / {oldValue} -> {newValue} / reason = {reason}");
#endif
    }

    public bool TryRewardDailyGreeting(string npcId)
    {
        if (string.IsNullOrEmpty(npcId) || _greetedNpcIdsToday.Contains(npcId)) return false;

        _greetedNpcIdsToday.Add(npcId);
        AddFriendship(npcId, _friendshipSettings.DailyGreetingReward, ENpcFriendshipReason.Greeting);
        return true;
    }

    private void HandleDayChanged(int day)
    {
        ResetDailyGreetings();
#if UNITY_EDITOR
        Debug.Log($"NPC 일일 인사 기록 초기화 - Day {day}");
#endif
    }

    public void ResetDailyGreetings()
    {
        _greetedNpcIdsToday.Clear();
    }

    // 외부에서 비정상적인 값이 들어왔을 경우를 대비한 방어 코드입니다.
    private int ClampFriendship(int friendship)
    {
        return Mathf.Clamp(friendship, 0, _friendshipSettings.MaxFriendship);
    }

    private void OnValidate()
    {
        _friendshipSettings?.Validate();
    }
}
