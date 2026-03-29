using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHelperInventoryAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private KeyCode _summonKey = KeyCode.E;
    [SerializeField] private KeyCode _rotateLeftKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _rotateRightKey = KeyCode.Alpha3;
    [SerializeField] private HelperDatabase _helperDatabase;
    [SerializeField] private List<HelperDataSO> _helperDataList = new();

    private PlayerHelperInteractionAbility _helperInteractionAbility;
    private readonly List<HelperController> _helperInstances = new();
    private int _currentIndex;
    private int _summonedIndex = -1;

    public int CurrentIndex => _currentIndex;
    public int Count => _helperDataList.Count;
    public int SummonedIndex => _summonedIndex;

    public HelperDataSO SummonedData => SummonedIndex >= 0 && SummonedIndex < _helperDataList.Count ? _helperDataList[SummonedIndex]: null;

    public static event Action<PlayerHelperInventoryAbility> OnLocalPlayerReady;

    public event Action<int> OnSelectionChanged;
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
        InitHelperInstances();
    }

    private void Start()
    {
        if (!_owner.IsMine) return;
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
        if (!_owner.IsMine) return;
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
        OnSelectionChanged?.Invoke(direction);
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

    public HelperController AddHelper(HelperDataSO data)
    {
        _helperDataList.Add(data);
        var instance = Instantiate(data.Prefab);
        instance.gameObject.SetActive(false);
        _helperInstances.Add(instance);
        return instance;
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        saveData.Helpers = new List<HelperSaveData>();
        for (int i = 0; i < _helperInstances.Count; i++)
        {
            var helper = _helperInstances[i];
            saveData.Helpers.Add(new HelperSaveData
            {
                HelperId = helper.HelperId,
                Level = helper.Level.CurrentLevel,
                Grade = (int)helper.Grade.CurrentGrade
            });
        }
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        foreach (var data in saveData.Helpers)
        {
            HelperDataSO so = _helperDatabase.GetById(data.HelperId);
            if (so == null) continue;

            // 이미 보유 중이면 스탯만 복원
            var existing = _helperInstances.Find(h => h.Data.HelperId == data.HelperId);
            if (existing != null)
            {
                existing.LoadState(data);
                continue;
            }

            // 없으면 새로 추가 후 스탯 복원
            var newHelper = AddHelper(so);
            newHelper.LoadState(data);
        }

        OnSelectionChanged?.Invoke(0);
    }
}