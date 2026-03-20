using System;
using System.Collections.Generic;
using UnityEngine;

public class HelperController : MonoBehaviour
{
    [SerializeField] private HelperDataSO _data;

    public HelperDataSO Data => _data;
    public string HelperId => _data.HelperId;
    public PlayerController PlayerOwner { get; private set; }
    public EHelperState State { get; private set; } = EHelperState.Inventory;
    public Transform FollowTarget { get; private set; }

    public HelperLevel Level { get; private set; }
    public HelperGrade Grade { get; private set; }
    public HelperEnergy Energy { get; private set; }

    private readonly Dictionary<Type, HelperAbility> _abilityCache = new();

    private const float SummonOffset = 1.5f;

    private void Awake()
    {
        Level = new HelperLevel(_data);
        Grade = new HelperGrade(_data);
        Energy = new HelperEnergy(_data);

        Energy.OnExhausted += OnEnergyExhasuted;
        Energy.OnRecovered += OnEnergyRecovered;
    }

    private void OnDestroy()
    {
        Energy.OnExhausted -= OnEnergyExhasuted;
        Energy.OnRecovered -= OnEnergyRecovered;
    }

    private void OnEnergyExhasuted()
    {
        Debug.Log("에너지 소진");
    }

    private void OnEnergyRecovered()
    {
        Debug.Log("에너지 회복");
    }

    private void Update()
    {
        Energy.Recover(Time.deltaTime);
    }

    public T GetAbility<T>() where T : HelperAbility
    {
        var type = typeof(T);

        if (_abilityCache.TryGetValue(type, out var cached))
            return cached as T;

        var ability = GetComponentInChildren<T>();
        if (ability != null)
            _abilityCache[type] = ability;

        return ability;
    }

    public void Summon(PlayerController playerOwner)
    {
        PlayerOwner = playerOwner;
        FollowTarget = playerOwner.transform;
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.position = FollowTarget.position + FollowTarget.right * SummonOffset;
        gameObject.SetActive(true);
    }

    public void Equip(Transform equipSlot)
    {
        State = EHelperState.Equipped;
        transform.SetParent(equipSlot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        GetAbility<HelperAnimationAbility>()?.Play(EHelperAnim.Equipped);
    }

    public void Unequip()
    {
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.position = FollowTarget.position;

        Vector3 backDir = -FollowTarget.forward;
        GetAbility<HelperFollowAbility>()?.LaunchBack(backDir);
    }

    public void LoadState(HelperSaveData data)
    {
        Level.CurrentLevel = data.Level;
        Grade.CurrentGrade = (EHelperGrade)data.Grade;
    }

    public void InteractPrimary(TerrainCell cell)
    {
        GetAbility<HelperInteractionAbility>()?.InteractPrimary(cell);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        GetAbility<HelperInteractionAbility>()?.InteractSecondary(cell);
    }
}