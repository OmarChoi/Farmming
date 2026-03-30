using UnityEngine;
using DG.Tweening;

public class UI_FriendshipBar : MonoBehaviour
{
    [Header("하트 슬롯")]
    [SerializeField] private UI_FriendshipHeartSlot[] _slots;

    [Header("친밀도 설정")]
    [SerializeField] private int _halfStepValue = 50;

    [Header("연출")]
    [SerializeField] private bool _usePunchAnimation = true;
    [SerializeField] private float _punchScale = 0.2f;
    [SerializeField] private float _punchDuration = 0.2f;

    [Header("펀치 세부 설정")]
    [SerializeField] private int _punchVibrato = 8;
    [SerializeField] private float _punchElasticity = 0.8f;

    [Header("fallback 설정")]
    [SerializeField] private int _fallbackMaxFriendship = 1000;

    [Header("하트 슬롯 설정")]
    [SerializeField] private int _halfStepsPerHeart = 2;

    private int _currentFriendship = -1;
    private string _currentNpcId;

    public void BindNpc(string npcId, bool immediate = true)
    {
        _currentNpcId = npcId;

        if (string.IsNullOrEmpty(npcId) || NpcFriendshipManager.Instance == null)
        {
            RefreshImmediate(0);
            return;
        }

        int friendship = NpcFriendshipManager.Instance.GetFriendship(npcId);

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

        int oldHalfSteps = _currentFriendship / _halfStepValue;
        int newHalfSteps = Mathf.Clamp(targetFriendship, 0, NpcFriendshipManager.Instance.MaxFriendship) / _halfStepValue;

        _currentFriendship = targetFriendship;
        ApplyToSlots(targetFriendship);

        if (_usePunchAnimation && newHalfSteps != oldHalfSteps)
        {
            PlayChangedSlotAnimation(oldHalfSteps, newHalfSteps);
        }
    }

    private void ApplyToSlots(int friendship)
    {
        if (_slots == null || _slots.Length == 0) return;

        int maxFriendship = NpcFriendshipManager.Instance != null
            ? NpcFriendshipManager.Instance.MaxFriendship
            : _fallbackMaxFriendship;

        friendship = Mathf.Clamp(friendship, 0, maxFriendship);

        int totalHalfSteps = friendship / _halfStepValue;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;

            int slotHalfStep = Mathf.Clamp(totalHalfSteps - (i * _halfStepsPerHeart), 0, _halfStepsPerHeart);

            EHeartFillState state = slotHalfStep switch
            {
                2 => EHeartFillState.Full,
                1 => EHeartFillState.Half,
                _ => EHeartFillState.Empty
            };

            _slots[i].SetState(state);
        }
    }

    private void PlayChangedSlotAnimation(int oldHalfSteps, int newHalfSteps)
    {
        if (_slots == null || _slots.Length == 0) return;

        int min = Mathf.Min(oldHalfSteps, newHalfSteps);
        int max = Mathf.Max(oldHalfSteps, newHalfSteps);

        for (int halfStep = min; halfStep < max; halfStep++)
        {
            int slotIndex = halfStep / 2;
            if (slotIndex < 0 || slotIndex >= _slots.Length) continue;
            if (_slots[slotIndex] == null) continue;

            Transform target = _slots[slotIndex].transform;
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * _punchScale, _punchDuration, _punchVibrato, _punchElasticity);
        }
    }
}
