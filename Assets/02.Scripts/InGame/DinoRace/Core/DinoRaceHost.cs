using System;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class DinoRaceHost : MonoBehaviour
{
    [SerializeField] private DinoRaceRunner[] _runners;
    [SerializeField] private DinoRaceSettings _settings = new DinoRaceSettings();
    [SerializeField] private Vector3 _trackForwardLocal = Vector3.forward;
    [SerializeField] private DinoRaceCountdownSignal _countdownSignal;
    [SerializeField] private GameObject _winParticle;
    [SerializeField] private float _returnDelay = 5f;

    private BaseBuilding _building;
    private DinoRaceEventPolicy _eventPolicy;
    private DinoRaceBetService _betService;
    private DinoRaceSession _session;
    private EDinoRaceState _state = EDinoRaceState.Idle;
    private float _countdownRemaining;
    private int _lastCountdownSecond = int.MinValue;
    private int _finishCount;
    private bool _npcBound;
    private Sequence _returnSequence;

    public event Action<int> CountdownTicked;
    public event Action RaceStarted;
    public event Action<DinoRaceResult> RaceFinished;
    public event Action RaceReset;

    public DinoRaceSettings Settings => _settings;
    public DinoRaceSession CurrentSession => _session;
    public EDinoRaceState State => _state;
    public bool IsBusy => _state == EDinoRaceState.Countdown || _state == EDinoRaceState.Running;

    private void Awake()
    {
        _building = GetComponent<BaseBuilding>();
        if (_runners == null || _runners.Length == 0)
            _runners = GetComponentsInChildren<DinoRaceRunner>(true);

        if (_trackForwardLocal.sqrMagnitude < 0.01f)
            _trackForwardLocal = Vector3.forward;

        _trackForwardLocal = _trackForwardLocal.normalized;
        _eventPolicy = new DinoRaceEventPolicy(_settings);
        _betService = new DinoRaceBetService(new DinoRacePayoutPolicy(_settings.Payout));
    }

    private void Start()
    {
        ResetRace(false);
    }

    private void Update()
    {
        BindNpcIfNeeded();

        switch (_state)
        {
            case EDinoRaceState.Countdown:
                UpdateCountdown();
                break;

            case EDinoRaceState.Running:
                UpdateRace();
                break;
        }
    }

    public string[] GetRunnerDisplayNames()
    {
        if (_runners == null || _runners.Length == 0)
            return Array.Empty<string>();

        string[] names = new string[_runners.Length];
        for (int i = 0; i < _runners.Length; i++)
            names[i] = _runners[i] != null ? _runners[i].DisplayName : $"Dino {i + 1}";

        return names;
    }

    public DinoRaceRunnerSnapshot[] CreateSnapshots()
    {
        if (_runners == null || _runners.Length == 0)
            return Array.Empty<DinoRaceRunnerSnapshot>();

        var snapshots = new DinoRaceRunnerSnapshot[_runners.Length];
        for (int i = 0; i < _runners.Length; i++)
        {
            snapshots[i] = _runners[i] != null
                ? _runners[i].CreateSnapshot()
                : new DinoRaceRunnerSnapshot(i, $"Dino {i + 1}", 0f, 0f, false, 0, EDinoRaceEventType.None);
        }

        return snapshots;
    }

    public float GetRemainingCountdown()
    {
        return Mathf.Max(0f, _countdownRemaining);
    }

    public bool TryBeginRace(int selectedRunnerIndex, int betAmount, out string error)
    {
        error = null;

        int validRunnerCount = GetValidRunnerCount();
        if (validRunnerCount == 0)
        {
            error = "No race runners are configured.";
            return false;
        }

        if (selectedRunnerIndex < 0 || selectedRunnerIndex >= _runners.Length || _runners[selectedRunnerIndex] == null)
        {
            error = "Selected runner is invalid.";
            return false;
        }

        if (IsBusy)
        {
            error = "Race is already in progress.";
            return false;
        }

        if (!_betService.TryPlaceBet(betAmount, out error))
            return false;

        _session = new DinoRaceSession(selectedRunnerIndex, betAmount);
        _finishCount = 0;
        _lastCountdownSecond = int.MinValue;
        _state = EDinoRaceState.Countdown;

        if (_countdownSignal != null)
        {
            _countdownRemaining = _countdownSignal.TotalDuration;
            _countdownSignal.Play(StartRaceInternal);
        }
        else
        {
            _countdownRemaining = Mathf.Max(0.1f, _settings.CountdownDuration);
        }

        float now = Time.time;
        for (int i = 0; i < _runners.Length; i++)
        {
            DinoRaceRunner runner = _runners[i];
            if (runner == null) continue;
            int capturedIndex = i;

            runner.Configure(
                i,
                _settings.BaseSpeed,
                _settings.TrackLength,
                _trackForwardLocal,
                _eventPolicy,
                () => GetSpeedUpBias(capturedIndex));
            runner.ResetForRace(now);
        }

        EmitCountdownIfChanged();
        return true;
    }

    public void ResetRace(bool notify = true)
    {
        _session = null;
        _state = EDinoRaceState.Idle;
        _countdownRemaining = 0f;
        _lastCountdownSecond = int.MinValue;
        _finishCount = 0;
        _countdownSignal?.Hide();
        _returnSequence?.Kill();
        _returnSequence = null;
        if (_winParticle != null)
            _winParticle.SetActive(false);

        float now = Time.time;
        if (_runners != null)
        {
            for (int i = 0; i < _runners.Length; i++)
            {
                DinoRaceRunner runner = _runners[i];
                if (runner == null) continue;
                int capturedIndex = i;

                runner.Configure(
                    i,
                    _settings.BaseSpeed,
                    _settings.TrackLength,
                    _trackForwardLocal,
                    _eventPolicy,
                    () => GetSpeedUpBias(capturedIndex));
                runner.ResetForRace(now);
            }
        }

        if (notify)
            RaceReset?.Invoke();
    }

    private void UpdateCountdown()
    {
        _countdownRemaining -= Time.deltaTime;
        EmitCountdownIfChanged();

        if (_countdownSignal == null && _countdownRemaining <= 0f)
            StartRaceInternal();
    }

    private void UpdateRace()
    {
        float now = Time.time;
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < _runners.Length; i++)
        {
            DinoRaceRunner runner = _runners[i];
            if (runner == null || runner.IsFinished) continue;

            runner.Tick(now, deltaTime);
            if (runner.CurrentDistance >= _settings.TrackLength)
            {
                _finishCount++;
                runner.Finish(_finishCount);
            }
        }

        if (_finishCount >= GetValidRunnerCount())
            CompleteRaceInternal();
    }

    private void StartRaceInternal()
    {
        _state = EDinoRaceState.Running;
        _countdownRemaining = 0f;

        for (int i = 0; i < _runners.Length; i++)
        {
            DinoRaceRunner runner = _runners[i];
            if (runner == null) continue;
            runner.StartRunning();
        }

        RaceStarted?.Invoke();
    }

    private void CompleteRaceInternal()
    {
        _state = EDinoRaceState.Finished;
        _countdownSignal?.Hide();

        for (int i = 0; i < _runners.Length; i++)
        {
            DinoRaceRunner runner = _runners[i];
            if (runner == null) continue;
            runner.SetWinner(runner.FinishRank == 1);
        }

        int selectedRank = 0;
        if (_session != null &&
            _session.SelectedRunnerIndex >= 0 &&
            _session.SelectedRunnerIndex < _runners.Length &&
            _runners[_session.SelectedRunnerIndex] != null)
        {
            selectedRank = _runners[_session.SelectedRunnerIndex].FinishRank;
        }

        if (selectedRank == 1 && _winParticle != null)
            _winParticle.SetActive(true);

        int payout = _session != null
            ? _betService.CalculatePayout(_session.BetAmount, selectedRank)
            : 0;

        _betService.Pay(payout);
        RaceFinished?.Invoke(new DinoRaceResult(_session, selectedRank, payout, CreateSnapshots()));

        PlayReturnSequence();
    }

    private void PlayReturnSequence()
    {
        _returnSequence?.Kill();
        _returnSequence = DOTween.Sequence();
        _returnSequence.AppendInterval(_returnDelay);

        if (_winParticle != null)
            _returnSequence.AppendCallback(() => _winParticle.SetActive(false));

        for (int i = 0; i < _runners.Length; i++)
        {
            DinoRaceRunner runner = _runners[i];
            if (runner == null) continue;

            if (i == 0)
                _returnSequence.Append(runner.ReturnToStart());
            else
                _returnSequence.Join(runner.ReturnToStart());
        }

        _returnSequence.Play();
    }

    private void EmitCountdownIfChanged()
    {
        int countdownValue = Mathf.Max(0, Mathf.CeilToInt(_countdownRemaining));
        if (countdownValue == _lastCountdownSecond) return;

        _lastCountdownSecond = countdownValue;
        CountdownTicked?.Invoke(countdownValue);
    }

    private void BindNpcIfNeeded()
    {
        if (_npcBound || _building == null || !_building.IsConstructionComplete)
            return;

        NpcController npc = _building.BuildingNpc;
        if (npc == null)
            return;

        DinoRaceNpcFeature feature = npc.GetComponent<DinoRaceNpcFeature>();
        if (feature == null)
            feature = npc.gameObject.AddComponent<DinoRaceNpcFeature>();

        feature.Initialize(this);
        _npcBound = true;
    }

    private int GetValidRunnerCount()
    {
        if (_runners == null) return 0;

        int count = 0;
        for (int i = 0; i < _runners.Length; i++)
        {
            if (_runners[i] != null)
                count++;
        }

        return count;
    }

    private float GetSpeedUpBias(int runnerIndex)
    {
        if (_runners == null ||
            runnerIndex < 0 ||
            runnerIndex >= _runners.Length ||
            _runners[runnerIndex] == null)
        {
            return 0f;
        }

        float minProgress = float.MaxValue;
        float maxProgress = float.MinValue;
        float currentProgress = 0f;
        bool hasProgressSample = false;

        for (int i = 0; i < _runners.Length; i++)
        {
            DinoRaceRunner runner = _runners[i];
            if (runner == null || runner.IsFinished) continue;

            float progress = Mathf.Clamp01(runner.CurrentDistance / Mathf.Max(0.1f, _settings.TrackLength));
            minProgress = Mathf.Min(minProgress, progress);
            maxProgress = Mathf.Max(maxProgress, progress);
            if (i == runnerIndex)
                currentProgress = progress;
            hasProgressSample = true;
        }

        if (!hasProgressSample || maxProgress - minProgress < 0.001f)
            return 0f;

        float normalizedPosition = Mathf.InverseLerp(minProgress, maxProgress, currentProgress);
        return 1f - (normalizedPosition * 2f);
    }
}
