using UnityEngine;

public class PlayerInteractionPromptAbility : PlayerAbility
{
    private UI_InteractionPrompt _promptUi;

    private PlayerNPCInteractionAbility _npcInteraction;
    private PlayerObjectInteractionAbility _objectInteraction;

    [Header("프롬프트 UI 오프셋")]
    [SerializeField] private Vector3 _npcPromptOffset = new Vector3(0f, 3f, -2f);
    [SerializeField] private Vector3 _objectPromptOffset = new Vector3(0f, 4f, -2f);

    private Transform _cachedPromptTarget;
    private Vector3 _cachedPromptOffset;

    private float _nextScanTime;
    private float _nextScanTimeSampling = 0.1f;


    protected override void Awake()
    {
        base.Awake();
        _npcInteraction = _owner.GetAbility<PlayerNPCInteractionAbility>();
        _objectInteraction = _owner.GetAbility<PlayerObjectInteractionAbility>();
    }

    private void Start()
    {
        Debug.Log($"[{name}] NPC Offset = {_npcPromptOffset}, Object Offset = {_objectPromptOffset}");
        TryBindPromptUi();
    }

    private void TryBindPromptUi()
    {
        if (_promptUi != null) return;

        _promptUi = FindFirstObjectByType<UI_InteractionPrompt>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (!_owner.IsMine) return;

        // 씬 전환에 대응하여 UI를 동적으로 찾아서 바인딩합니다.
        if (_promptUi == null)
        {
            _promptUi = UI_InteractionPrompt.Instance;
            if (_promptUi == null) return;
        }

        // UI가 열려있거나 행동이 잠긴 상태에서는 상호작용 프롬프트를 숨깁니다.
        if (_owner.IsUIOpen || _owner.IsActionLocked)
        {
            _cachedPromptTarget = null;
            _promptUi.Hide();
            return;
        }

        // 상호작용 대상 스캔은 0.1초마다 한 번씩만 계산하여 캐싱합니다.
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + _nextScanTimeSampling;
            CachePromptTarget();
        }

        // UI는 캐시값으로 즉시 반영합니다.
        if (_cachedPromptTarget != null)
        {
            _promptUi.Show(_cachedPromptTarget, _cachedPromptOffset);
        }
        else
        {
            _promptUi.Hide();
        }
    }

    private void CachePromptTarget()
    {
        IWorldInteractable objectInteraction = _objectInteraction != null
            ? _objectInteraction.GetClosestInteraction()
            : null;

        if (objectInteraction is Component objectComponent)
        {
            _cachedPromptTarget = objectComponent.transform;
            _cachedPromptOffset = _objectPromptOffset;
            Debug.Log($"Object Offset: {_objectPromptOffset}, Cached: {_cachedPromptOffset}");
            return;
        }

        IInteraction npcInteraction = _npcInteraction != null
            ? _npcInteraction.GetClosestInteraction()
            : null;

        if (npcInteraction is Component npcComponent)
        {
            _cachedPromptTarget = npcComponent.transform;
            _cachedPromptOffset = _npcPromptOffset;
            Debug.Log($"NPC Offset: {_npcPromptOffset}, Cached: {_cachedPromptOffset}");
            return;
        }

        _cachedPromptTarget = null;
        _cachedPromptOffset = Vector3.zero;
    }
}
