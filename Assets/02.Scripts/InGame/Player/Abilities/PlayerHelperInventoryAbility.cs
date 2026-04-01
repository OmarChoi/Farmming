using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerHelperInventoryAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private KeyCode _summonKey = KeyCode.E;
    [SerializeField] private KeyCode _rotateLeftKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _rotateRightKey = KeyCode.Alpha3;
    [SerializeField] private HelperDatabase _helperDatabase;
    [SerializeField] private List<HelperDataSO> _helperDataList = new();

    private PlayerHelperInteractionAbility _helperInteractionAbility;
    private HelperController _activeHelper;
    private readonly Dictionary<string, HelperSaveData> _savedStates = new();
    private int _currentIndex;
    private int _summonedIndex = -1;

    public int CurrentIndex => _currentIndex;
    public int Count => _helperDataList.Count;
    public int SummonedIndex => _summonedIndex;
    public HelperController ActiveHelper => _activeHelper;

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
    }

    private void Start()
    {
        if (!_owner.IsMine) return;
        OnLocalPlayerReady?.Invoke(this);
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
            SaveActiveHelperState();
            _helperInteractionAbility.Unsummon();
            _activeHelper = null;
            _summonedIndex = -1;
        }
        else
        {
            if (_activeHelper != null)
            {
                SaveActiveHelperState();
                _helperInteractionAbility.Unsummon();
                _activeHelper = null;
            }

            var data = _helperDataList[_currentIndex];
            Vector3 spawnPos = _owner.transform.position + _owner.transform.right * 1.5f;

            HelperController helper;
            if (PhotonNetwork.IsConnected)
            {
                var go = PhotonNetwork.Instantiate(data.Prefab.name, spawnPos, Quaternion.identity);
                helper = go.GetComponent<HelperController>();
            }
            else
            {
                helper = Instantiate(data.Prefab, spawnPos, Quaternion.identity);
            }

            RestoreHelperState(helper);
            _activeHelper = helper;
            _helperInteractionAbility.Summon(helper);
            _summonedIndex = _currentIndex;
        }
        OnSummonChanged?.Invoke(_summonedIndex);
    }

    private void SaveActiveHelperState()
    {
        if (_activeHelper == null) return;
        _savedStates[_activeHelper.HelperId] = new HelperSaveData
        {
            HelperId = _activeHelper.HelperId,
            Level = _activeHelper.Level.CurrentLevel,
            Grade = (int)_activeHelper.Grade.CurrentGrade,
            Experience = _activeHelper.Experience.CurrentExp,
            Energy = _activeHelper.Energy.Current,
            EnergySavedAt = UnityEngine.Time.realtimeSinceStartup
        };
    }

    private void RestoreHelperState(HelperController helper)
    {
        if (_savedStates.TryGetValue(helper.HelperId, out var state))
            helper.LoadState(state);
    }

    public void AddHelper(HelperDataSO data)
    {
        _helperDataList.Add(data);
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        SaveActiveHelperState();

        saveData.Helpers = new List<HelperSaveData>();
        foreach (var data in _helperDataList)
        {
            if (data == null) continue;

            if (_savedStates.TryGetValue(data.HelperId, out var state))
            {
                saveData.Helpers.Add(state);
            }
            else
            {
                saveData.Helpers.Add(new HelperSaveData
                {
                    HelperId = data.HelperId,
                    Level = 1,
                    Grade = 0
                });
            }
        }
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        foreach (var data in saveData.Helpers)
        {
            HelperDataSO so = _helperDatabase.GetById(data.HelperId);
            if (so == null) continue;

            _savedStates[data.HelperId] = data;

            if (_helperDataList.Find(d => d.HelperId == data.HelperId) == null)
                _helperDataList.Add(so);
        }

        if (_activeHelper != null)
            RestoreHelperState(_activeHelper);

        OnSelectionChanged?.Invoke(0);
    }
}