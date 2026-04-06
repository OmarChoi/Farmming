using UnityEngine;

public class DefaultBuilding : BaseBuilding
{
    private const float ProgressTweenDuration = 1.5f;
    private const float RevealFeather = 0.08f;

    private readonly BuildingConstructionVisual _constructionVisual = new BuildingConstructionVisual();

    private float _displayProgress = 1f;
    private float _targetProgress = 1f;
    private bool _targetComplete;
    private bool _hasInitialVisualState;

    private void Update()
    {
        if (!_constructionVisual.IsInitialized) return;
        if (Mathf.Approximately(_displayProgress, _targetProgress)) return;

        _displayProgress = Mathf.MoveTowards(
            _displayProgress,
            _targetProgress,
            Time.deltaTime / ProgressTweenDuration);

        bool finalize = Mathf.Approximately(_displayProgress, _targetProgress) && _targetComplete;
        _constructionVisual.ApplyProgress(_displayProgress, finalize);
    }

    protected override void OnBuildingInitialized()
    {
        _hasInitialVisualState = false;

        if (BuildingData == null || BuildingData.ConstructionDays <= 0)
        {
            _constructionVisual.Dispose();
            return;
        }

        if (!ConstructionContext.HasRequiredShaders)
        {
            Debug.LogWarning($"{nameof(DefaultBuilding)} skipped construction visuals because the required construction shaders are missing.", this);
            _constructionVisual.Dispose();
            return;
        }

        _constructionVisual.Initialize(
            gameObject,
            ConstructionContext.RevealLitShader,
            ConstructionContext.GhostRevealShader,
            ConstructionContext.GhostMaterial,
            RevealFeather);
    }

    protected override void OnConstructionStateChanged(float progress, bool isComplete)
    {
        if (!_constructionVisual.IsInitialized)
        {
            _displayProgress = progress;
            _targetProgress = progress;
            _targetComplete = isComplete;
            return;
        }

        if (!_hasInitialVisualState)
        {
            _displayProgress = progress;
            _targetProgress = progress;
            _targetComplete = isComplete;
            _constructionVisual.ApplyProgress(_displayProgress, isComplete);
            _hasInitialVisualState = true;
            return;
        }

        _targetProgress = progress;
        _targetComplete = isComplete;

        if (Mathf.Approximately(_displayProgress, _targetProgress))
        {
            _constructionVisual.ApplyProgress(_displayProgress, _targetComplete);
        }
    }

    protected override void OnConstructionCompleted()
    {
    }

    protected override void OnDestroy()
    {
        _constructionVisual.Dispose();
        base.OnDestroy();
    }
}
