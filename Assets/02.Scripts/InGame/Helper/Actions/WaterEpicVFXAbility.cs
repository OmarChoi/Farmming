using System;
using System.Collections;
using UnityEngine;

public class WaterEpicVFXAbility : HelperAbility, IWaterGradeVFX
{
    [Header("에픽 등급 물효과")]
    [SerializeField] private GameObject _helperVfxPrefab; 
    [SerializeField] private GameObject _landVfxPrefab; 
    [SerializeField] private float _helperVfxDuration = 1f;
    [SerializeField] private float _landVfxDuration = 2.5f;
    [SerializeField] private float _actionCompletionDelay = 1.0f;
    [SerializeField] private float _landDelay = 0.1f;
    [SerializeField] private float _waterOpenDelay = 0.2f;
    [SerializeField] private float _jumpInterval = 0.6f;

    private HelperAnimationAbility _animAbility;
    private Coroutine _jumpCoroutine;
    private Coroutine _completionCoroutine;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public void BeginAction(Action onWaterOpen, Action onComplete)
    {
        _animAbility?.Play(EHelperAnim.Jump);

        if (_jumpCoroutine != null) StopCoroutine(_jumpCoroutine);
        _jumpCoroutine = StartCoroutine(LoopJumpAnimation());

        if (_completionCoroutine != null) StopCoroutine(_completionCoroutine);
        _completionCoroutine = StartCoroutine(EpicActionFlow(onWaterOpen, onComplete));
    }

    private IEnumerator EpicActionFlow(Action onWaterOpen, Action onComplete)
    {
        yield return new WaitForSeconds(_waterOpenDelay);
        onWaterOpen?.Invoke();

        yield return new WaitForSeconds(_landDelay + _actionCompletionDelay);

        _completionCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator LoopJumpAnimation()
    {
        while (true)
        {
            _animAbility?.Play(EHelperAnim.Jump);
            yield return new WaitForSeconds(_jumpInterval);
        }
    }

    public void SpawnHelperVFX()
    {
        if (_helperVfxPrefab == null) return;

        GameObject vfx = Instantiate(_helperVfxPrefab, _owner.transform.position, Quaternion.identity);
        vfx.transform.SetParent(_owner.transform);
        Destroy(vfx, _helperVfxDuration);
    }

    public void SpawnCellVFX(TerrainCell cell, Vector3 targetPos, bool isCenter,
        Vector3 spawnPos, Action<TerrainCell, bool> onCellLand)
    {
        StartCoroutine(DelayedLandEffect(cell, targetPos, isCenter, onCellLand));
    }

    private IEnumerator DelayedLandEffect(TerrainCell cell, Vector3 targetPos,
        bool isCenter, Action<TerrainCell, bool> onCellLand)
    {
        yield return new WaitForSeconds(0.1f);
        SpawnLandVFX(targetPos, cell, isCenter, onCellLand);
    }

    private void SpawnLandVFX(Vector3 position, TerrainCell cell, bool isCenter,
        Action<TerrainCell, bool> onCellLand)
    {
        if (_landVfxPrefab == null) return;

        GameObject landVfx = Instantiate(_landVfxPrefab, position, Quaternion.identity);
        EpicWaterLandEffect landEffect = landVfx.GetComponentInChildren<EpicWaterLandEffect>();
        landEffect?.Initialize(cell, isCenter, onCellLand);
        Destroy(landVfx, _landVfxDuration);
    }

    public void Cancel()
    {
        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = null;
        }

        if (_completionCoroutine != null)
        {
            StopCoroutine(_completionCoroutine);
            _completionCoroutine = null;
        }
    }
}
