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
    private HelperAnimationAbility _animationAbility;
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

    public List<HelperDataSO> GetOwnedHelpers()
    {
        return new List<HelperDataSO>(_helperDataList);
    }

    public bool TryGetSavedState(string helperId, out HelperSaveData saveData)
    {
        return _savedStates.TryGetValue(helperId, out saveData);
    }

    public static event Action<PlayerHelperInventoryAbility> OnLocalPlayerReady;

    public event Action<int> OnSelectionChanged;
    public event Action<int> OnSummonChanged;

    public int GetHelperExperience(HelperDataSO data)
    {
        if (data == null) return 0;

        if (_activeMainHelper != null && _activeMainHelper.HelperId == data.HelperId)
            return _activeMainHelper.Experience.CurrentExp;

        if (_activeLightHelper != null && _activeLightHelper.HelperId == data.HelperId)
            return _activeLightHelper.Experience.CurrentExp;

        if (_savedStates.TryGetValue(data.HelperId, out var state))
            return state.Experience;

        return 0;
    }

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
            if (_helperDataList[_currentIndex].IsLightHelper)
            {
                OnSummonChanged?.Invoke(SummonedIndex);
                return;
            }
        }

        var data = _helperDataList[_currentIndex];
        bool isLightHelper = data.IsLightHelper;

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
                _activeLightHelper = helper;
                _activeLightHelper.OnGradeChanged += OnLightHelperGradeChanged;
                _helperInteractionAbility.Summon(helper);
                RestoreHelperState(helper);
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
                _activeMainHelper = helper;
                _activeMainHelper.OnGradeChanged += OnMainHelperGradeChanged;
                _helperInteractionAbility.Summon(helper);
                RestoreHelperState(helper);
                _summonedMainIndex = _currentIndex;
            }
        }

        OnSummonChanged?.Invoke(SummonedIndex);
    }

    private HelperController InstantiateHelper(HelperDataSO data)
    {
        EHelperGrade grade = GetHelperGrade(data);
        HelperController prefab = data.GetPrefabForGrade(grade);

        Vector3 spawnPos = _owner.transform.position + _owner.transform.right * 1.5f;
        if (PhotonNetwork.IsConnected)
        {
            var go = PhotonNetwork.Instantiate(prefab.name, spawnPos, Quaternion.identity);
            return go.GetComponent<HelperController>();

        }
        return Instantiate(prefab, spawnPos, Quaternion.identity);
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
                    Grade = 0,
                    Energy = data.MaxEnergy,
                    EnergySavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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

    public bool TryUpgradeHelper(HelperDataSO data)
    {
        if (data == null) return false;

        HelperController activeHelper = FindActiveHelper(data.HelperId);

        if (activeHelper != null)
        {
            if (!activeHelper.Experience.IsReadyToUpgrade)
                return false;

            activeHelper.PerformUpgrade();
            activeHelper.Energy.RecoverFull();
            SaveHelperState(activeHelper);
            return true;
        }

        if (!_savedStates.TryGetValue(data.HelperId, out var state))
            return false;

        EHelperGrade currentGrade = (EHelperGrade)state.Grade;
        if (currentGrade == EHelperGrade.Legendary)
            return false;

        int needExp = GetMaxExpByGrade(data, currentGrade);
        if (state.Experience < needExp)
            return false;

        state.Experience = 0;
        state.Grade += 1;
        state.Energy = data.MaxEnergy;

        _savedStates[data.HelperId] = state;
        return true;
    }

    public void RespawnHelper(HelperDataSO data)
    {
        if (data == null) return;

        HelperController activeHelper = FindActiveHelper(data.HelperId);
        if (activeHelper == null) return;

        if (data.IsLightHelper)
        {
            SaveHelperState(activeHelper);
            UnsubscribeLightHelper();
            _helperInteractionAbility.UnsummonBack();
            _activeLightHelper = null;

            HelperController newHelper = InstantiateHelper(data);
            _activeLightHelper = newHelper;

            HelperAnimationAbility animationAbility = newHelper.GetAbility<HelperAnimationAbility>();
            if (animationAbility != null)
            {
                animationAbility.InitAnimator();
            }
            _activeLightHelper.OnGradeChanged += OnLightHelperGradeChanged;
            _helperInteractionAbility.Summon(newHelper);
            RestoreHelperState(newHelper);
        }
        else
        {
            SaveHelperState(activeHelper);
            UnsubscribeMainHelper();
            _helperInteractionAbility.UnsummonCurrentOnly();
            _activeMainHelper = null;

            HelperController newHelper = InstantiateHelper(data);
            _activeMainHelper = newHelper;


            HelperAnimationAbility animationAbility = newHelper.GetAbility<HelperAnimationAbility>();
            if (animationAbility != null)
            {
                animationAbility.InitAnimator();
            }

            _activeMainHelper.OnGradeChanged += OnMainHelperGradeChanged;
            _helperInteractionAbility.Summon(newHelper);
            RestoreHelperState(newHelper);
        }

        OnSummonChanged?.Invoke(SummonedIndex);
    }

    private HelperController FindActiveHelper(string helperId)
    {
        if (_activeMainHelper != null && _activeMainHelper.HelperId == helperId)
            return _activeMainHelper;

        if (_activeLightHelper != null && _activeLightHelper.HelperId == helperId)
            return _activeLightHelper;

        return null;
    }

    public int GetMaxExpByGrade(HelperDataSO data, EHelperGrade grade)
    {
        if (data == null) return 0;

        switch (grade)
        {
            case EHelperGrade.Normal:
                return data.NormalMaxExp;
            case EHelperGrade.Epic:
                return data.EpicMaxExp;
            case EHelperGrade.Legendary:
                return 0;
            default:
                return 0;
        }
    }
}