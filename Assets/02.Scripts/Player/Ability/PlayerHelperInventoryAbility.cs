using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHelperInventoryAbility : PlayerAbility
{
    [SerializeField] private KeyCode _summonKey = KeyCode.E;
    [SerializeField] private KeyCode _rotateLeftKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _rotateRightKey = KeyCode.Alpha3;
    [SerializeField] private List<HelperDataSO> _helperDataList = new();

    private PlayerHelperInteractionAbility _helperInteractionAbility;
    private readonly List<HelperController> _helperInstances = new();
    private int _currentIndex;
    private int _summonedIndex = -1;

    public int CurrentIndex => _currentIndex;
    public int Count => _helperDataList.Count;
    public int SummonedIndex => _summonedIndex;

    public static event Action<PlayerHelperInventoryAbility> OnLocalPlayerReady;

    public event Action OnSelectionChanged;
    public event Action<int> OnSummonChanged;

    public HelperDataSO GetData(int index)
    {
        if (_helperDataList.Count == 0) return null;
        int i = ((index % _helperDataList.Count) + _helperDataList.Count) % _helperDataList.Count;
        return _helperDataList[i];
    }

    public HelperDataSO LeftData => GetData(_currentIndex - 1);
    public HelperDataSO CenterData => GetData(_currentIndex);
    public HelperDataSO RightData => GetData(_currentIndex + 1);

    protected override void Awake()
    {
        base.Awake();
        _helperInteractionAbility = _owner.GetAbility<PlayerHelperInteractionAbility>();
    }

    private void Start()
    {
        InitHelperInstances();
        OnLocalPlayerReady?.Invoke(this);
    }

    private void InitHelperInstances()
    {
        foreach (var data in _helperDataList)
        {
            if (data == null || data.Prefab == null) continue;

            var instance = Instantiate(data.Prefab);
            instance.gameObject.SetActive(false);
            _helperInstances.Add(instance);
        }
    }

    private void Update()
    {
        if (_owner.IsUIOpen) return;
        if (_helperDataList.Count == 0) return;

        if (Input.GetKeyDown(_rotateLeftKey))
            Rotate(-1);
        if (Input.GetKeyDown(_rotateRightKey))
            Rotate(1);
        if (Input.GetKeyDown(_summonKey))
            ToggleSummon();
    }

    private void Rotate(int direction)
    {
        int count = _helperDataList.Count;
        _currentIndex = ((_currentIndex + direction) % count + count) % count;
        OnSelectionChanged?.Invoke();
    }

    private void ToggleSummon()
    {
        if (_summonedIndex == _currentIndex)
        {
            _helperInteractionAbility.Unsummon();
            _summonedIndex = -1;
        }
        else
        {
            _helperInteractionAbility.Summon(_helperInstances[_currentIndex]);
            _summonedIndex = _currentIndex;
        }
        OnSummonChanged?.Invoke(_summonedIndex);
    }

    public void AddHelper(HelperDataSO data)
    {
        _helperDataList.Add(data);
        var instance = Instantiate(data.Prefab);
        instance.gameObject.SetActive(false);
        _helperInstances.Add(instance);
    }
}