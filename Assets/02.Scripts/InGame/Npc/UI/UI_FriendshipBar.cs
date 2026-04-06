using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class UI_FriendshipBar : MonoBehaviour
{
    [SerializeField] private NpcFriendshipManager _friendshipManager;
    private IFriendshipService _friendshipService;
    private IFriendshipHeartCalculator _friendshipHeartCalculator;

    [Header("하트 슬롯")]
    [SerializeField] private UI_FriendshipHeartSlot[] _slots;

    [Header("연출")]
    [SerializeField] private bool _usePunchAnimation = true;
    [SerializeField] private float _punchScale = 0.2f;
    [SerializeField] private float _punchDuration = 0.2f;

    [Header("펀치 세부 설정")]
    [SerializeField] private int _punchVibrato = 8;
    [SerializeField] private float _punchElasticity = 0.8f;

    [Header("fallback 설정")]
    [SerializeField] private int _fallbackMaxFriendship = 1000;

    private int _currentFriendship = -1;
    private string _currentNpcId;

    private void Awake()
    {
        if (_friendshipManager == null)
        {
            _friendshipManager = FindFirstObjectByType<NpcFriendshipManager>();
        }
        _friendshipService = _friendshipManager;
        _friendshipHeartCalculator = _friendshipManager;
    }
    public void BindNpc(string npcId, bool immediate = true)
    {
        _currentNpcId = npcId;

        if (string.IsNullOrEmpty(npcId) || _friendshipService == null)
        {
            RefreshImmediate(0);
            return;
        }

        int friendship = _friendshipService.GetFriendship(npcId);

        if (immediate)
        {
            RefreshImmediate(friendship);
        }
        else
        {
            RefreshAnimated(friendship);
        }
    }

    public void RefreshImmediate(int friendship)
    {
        _currentFriendship = friendship;
        ApplyToSlots(friendship);
    }

    public void Clear()
    {
        _currentNpcId = null;
        _currentFriendship = -1;
        ApplyToSlots(0);
    }

    public void RefreshAnimated(int targetFriendship)
    {
        if (_currentFriendship < 0)
        {
            _currentFriendship = targetFriendship;
            ApplyToSlots(targetFriendship);
            return;
        }

        int oldValue = _currentFriendship;
        int newValue = targetFriendship;

        _currentFriendship = targetFriendship;
        ApplyToSlots(targetFriendship);

        if (_usePunchAnimation && _friendshipHeartCalculator != null)
        {
            var changedSlots = _friendshipHeartCalculator.GetChangedHeartSlots(oldValue, newValue, _slots.Length);

            PlayChangedSlotAnimation(changedSlots);
        }
    }

    private void ApplyToSlots(int friendship)
    {
        if (_slots == null || _slots.Length == 0) return;
        if (_friendshipHeartCalculator == null) return;

        var steps = _friendshipHeartCalculator.CalculateHeartSteps(friendship, _slots.Length);

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;

            EHeartFillState state = steps[i] switch
            {
                2 => EHeartFillState.Full,
                1 => EHeartFillState.Half,
                _ => EHeartFillState.Empty
            };

            _slots[i].SetState(state);
        }
    }

    private void PlayChangedSlotAnimation(List<int> slotIndices)
    {
        if (_slots == null || _slots.Length == 0) return;

        foreach (var slotIndex in slotIndices)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) continue;
            if (_slots[slotIndex] == null) continue;

            Transform target = _slots[slotIndex].transform;
            target.DOKill();
            target.localScale = Vector3.one;

            target.DOPunchScale(Vector3.one * _punchScale, _punchDuration, _punchVibrato, _punchElasticity);
        }
    }
}
