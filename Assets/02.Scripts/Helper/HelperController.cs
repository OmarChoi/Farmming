using System;
using System.Collections.Generic;
using UnityEngine;

public class HelperController : MonoBehaviour
{
    [SerializeField] private HelperDataSO _data;

    public EHelperState State { get; private set; } = EHelperState.Summoned;
    public Transform FollowTarget { get; private set; }

    public HelperLevel Level { get; private set; }
    public HelperGrade Grade { get; private set; }
    public HelperEnergy Energy { get; private set; }

    private IHelperAction _helperAction;

    private readonly Dictionary<Type, HelperAbility> _abilityCache = new();

    private void Awake()
    {
        Level = new HelperLevel(_data);
        Grade = new HelperGrade(_data);
        Energy = new HelperEnergy(_data);

        _helperAction = GetComponentInChildren<IHelperAction>();

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

    public void Summon(Transform followTarget)
    {
        FollowTarget = followTarget;
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.position = followTarget.position + followTarget.right * 1.5f;
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

    public void Interact(TerrainCell cell)
    {

        if(Energy.IsExhausted)
        {
            Debug.Log("에너지 소진상태");
            return;
        }

        if (_helperAction != null)
        {
            _helperAction.Interact(cell);
            Energy.TryConsume(Level.GetEnergyCost());
        }
    }
}