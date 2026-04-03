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
    private HelperController _activeMainHelper;
    private HelperController _activeLightHelper;
    private readonly Dictionary<string, HelperSaveData> _savedStates = new();
    private int _currentIndex;
    private int _summonedMainIndex = -1;
    private int _summonedLightIndex = -1;

    private void OnMainHelperGradeChanged() => OnSummonChanged?.Invoke(SummonedIndex);
    private void OnLightHelperGradeChanged() => OnSummonChanged?.Invoke(SummonedIndex);

    public int CurrentIndex => _currentIndex;
    public int Count => _helperDataList.Count;
    public int SummonedIndex => _summonedMainIndex >= 0 ? _summonedMainIndex : _summonedLightIndex;
    public bool IsCurrentIndexSummoned => _currentIndex == _summonedMainIndex || _currentIndex == _summonedLightIndex;
    public HelperController ActiveHelper => _activeMainHelper != null ? _activeMainHelper : _activeLightHelper;

    public HelperDataSO SummonedData
    {
        get
        {
            int idx = SummonedIndex;
            return idx >= 0 && idx < _helperDataList.Count ? _helperDataList[idx] : null;
        }
    }

    public static event Action<PlayerHelperInventoryAbility> OnLocalPlayerReady;

    public event Action<int> OnSelectionChanged;
    public event Action<int> OnSummonChanged;

    public EHelperGrade GetHelperGrade(HelperDataSO data)
    {
        if (data == null) return EHelperGrade.Normal;

        if (_activeMainHelper != null && _activeMainHelper.HelperId == data.HelperId)
            return _activeMainHelper.Grade.CurrentGrade;
        if (_activeLightHelper != null && _activeLightHelper.HelperId == data.HelperId)
            return _activeLightHelper.Grade.CurrentGrade;

        if (_savedStates.TryGetValue(data.HelperId, out var state))
            return (EHelperGrade)state.Grade;

        return EHelperGrade.Normal;
    }

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
        // 빛곡룡이 Summoned(등에서 해제) 상태이고 일반 helper가 없으면
        // 빛곡룡을 먼저 소환 해제하고, 현재 인덱스가 같으면 여기서 종료
        if (_activeLightHelper != null
            && _activeLightHelper.State == EHelperState.Summoned
            && _activeMainHelper == null)
        {
            SaveHelperState(_activeLightHelper);
            _helperInteractionAbility.UnsummonBack();
            _activeLightHelper = null;
            _summonedLightIndex = -1;

            // 현재 인덱스가 방금 해제한 빛곡룡이였으면 소환 해제만
            if (_helperDataList[_currentIndex].Prefab.GetComponent<LightActionAbility>() != null)
            {
                OnSummonChanged?.Invoke(SummonedIndex);
                return;
            }
        }

        var data = _helperDataList[_currentIndex];
        bool isLightHelper = data.Prefab.GetComponent<LightActionAbility>() != null;

        if (isLightHelper)
        {
            if (_summonedLightIndex == _currentIndex)
            {
                // 빛곡룡 소환 해제: 일반 helper가 소환 중이면 불가
                if (_activeMainHelper != null) return;

                SaveHelperState(_activeLightHelper);
                _helperInteractionAbility.UnsummonBack();
                _activeLightHelper = null;
                _summonedLightIndex = -1;
            }
            else
            {
                // 일반 helper가 소환 중이면 먼저 해제 후 빛곡룡 소환
                if (_activeMainHelper != null)
                {
                    SaveHelperState(_activeMainHelper);
                    _helperInteractionAbility.UnsummonCurrentOnly();
                    _activeMainHelper = null;
                    _summonedMainIndex = -1;
                }

                // 기존 빛곡룡이 있으면 먼저 해제
                if (_activeLightHelper != null)
                {
                    SaveHelperState(_activeLightHelper);
                    UnsubscribeLightHelper();
                    _helperInteractionAbility.UnsummonBack();
                    _activeLightHelper = null;
                }

                HelperController helper = InstantiateHelper(data);
                RestoreHelperState(helper);
                _activeLightHelper = helper;
                _activeLightHelper.OnGradeChanged += OnLightHelperGradeChanged;
                _helperInteractionAbility.Summon(helper);
                _summonedLightIndex = _currentIndex;
            }
        }
        else
        {
            if (_summonedMainIndex == _currentIndex)
            {
                // 일반 helper 소환 해제
                SaveHelperState(_activeMainHelper);
                UnsubscribeMainHelper();
                _helperInteractionAbility.UnsummonCurrentOnly();
                _activeMainHelper = null;
                _summonedMainIndex = -1;
            }
            else
            {
                // 기존 일반 helper가 있으면 먼저 해제 (빛곡룡은 건드리지 않음)
                if (_activeMainHelper != null)
                {
                    SaveHelperState(_activeMainHelper);
                    _helperInteractionAbility.UnsummonCurrentOnly();
                    _activeMainHelper = null;
                }

                HelperController helper = InstantiateHelper(data);
                RestoreHelperState(helper);
                _activeMainHelper = helper;
                _activeMainHelper.OnGradeChanged += OnMainHelperGradeChanged;
                _helperInteractionAbility.Summon(helper);
                _summonedMainIndex = _currentIndex;
            }
        }

        OnSummonChanged?.Invoke(SummonedIndex);
    }

    private HelperController InstantiateHelper(HelperDataSO data)
    {
        Vector3 spawnPos = _owner.transform.position + _owner.transform.right * 1.5f;
        if (PhotonNetwork.IsConnected)
        {
            var go = PhotonNetwork.Instantiate(data.Prefab.name, spawnPos, Quaternion.identity);
            return go.GetComponent<HelperController>();
        }
        return Instantiate(data.Prefab, spawnPos, Quaternion.identity);
    }

    private void SaveHelperState(HelperController helper)
    {
        if (helper == null) return;
        _savedStates[helper.HelperId] = new HelperSaveData
        {
            HelperId = helper.HelperId,
            Level = helper.Level.CurrentLevel,
            Grade = (int)helper.Grade.CurrentGrade,
            Experience = helper.Experience.CurrentExp,
            Energy = helper.Energy.Current,
            EnergySavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    private void UnsubscribeMainHelper()
    {
        if (_activeMainHelper == null)
        {
            return;
        }
        _activeMainHelper.OnGradeChanged -= OnMainHelperGradeChanged;
    }

    private void UnsubscribeLightHelper()
    {
        if (_activeLightHelper == null)
        {
            return;
        }
        _activeLightHelper.OnGradeChanged -= OnLightHelperGradeChanged;
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
        SaveHelperState(_activeMainHelper);
        SaveHelperState(_activeLightHelper);

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

        if (_activeMainHelper != null)
            RestoreHelperState(_activeMainHelper);
        if (_activeLightHelper != null)
            RestoreHelperState(_activeLightHelper);

        OnSelectionChanged?.Invoke(0);
    }
}