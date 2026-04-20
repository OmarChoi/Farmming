using System;
using System.Collections;
using UnityEngine;

public class WaterLegendaryVFXAbility : HelperAbility, IWaterGradeVFX
{
    [Header("레전더리 등급 물효과")]
    [SerializeField] private GameObject _helperVfxPrefab;   
    [SerializeField] private GameObject _landVfxPrefab;     
    [SerializeField] private float _helperVfxDuration = 1f;
    [SerializeField] private float _landVfxDuration = 2.5f;
    [SerializeField] private float _rotationDuration = 2.5f;
    [SerializeField] private float _rotationSpeed = 720f;   // 숫자가 클수록 회전 속도 빨라짐
    [SerializeField] private float _waterOpenDelay = 0.2f;

    private HelperAnimationAbility _animAbility;
    private Coroutine _rotationCoroutine;
    private bool _landImpactSfxPlayed;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public void BeginAction(Action onWaterOpen, Action onComplete)
    {
        _landImpactSfxPlayed = false;
        _animAbility?.Play(EHelperAnim.Happy);
        _animAbility?.Play(EHelperAnim.Jump);

        if (_rotationCoroutine != null) StopCoroutine(_rotationCoroutine);
        _rotationCoroutine = StartCoroutine(RotateLegendaryAndComplete(onWaterOpen, onComplete));
    }

    private IEnumerator RotateLegendaryAndComplete(Action onWaterOpen, Action onComplete)
    {
        float elapsed = 0f;
        bool waterOpened = false;
        Quaternion startRotation = _owner.transform.rotation;

        while (elapsed < _rotationDuration)
        {
            elapsed += Time.deltaTime;

            if (!waterOpened && elapsed >= _waterOpenDelay)
            {
                waterOpened = true;
                onWaterOpen?.Invoke();
            }

            _owner.transform.rotation = startRotation * Quaternion.Euler(0, elapsed * _rotationSpeed, 0);
            yield return null;
        }

        _owner.transform.rotation = startRotation;
        _rotationCoroutine = null;
        onComplete?.Invoke();
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
        SpawnLandVFX(targetPos);
        onCellLand?.Invoke(cell, isCenter);
    }

    private void SpawnLandVFX(Vector3 position)
    {
        if (_landVfxPrefab == null) return;

        PlayWaterImpactSfx(position);
        GameObject landVfx = Instantiate(_landVfxPrefab, position, Quaternion.identity);
        Destroy(landVfx, _landVfxDuration);
    }

    public void Cancel()
    {
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
            _rotationCoroutine = null;
        }
    }

    private void PlayWaterImpactSfx(Vector3 targetPos)
    {
        if (_landImpactSfxPlayed || SoundManager.Instance == null)
            return;

        _landImpactSfxPlayed = true;
        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: AssetKey.SFX.WaterLegendarySplash,
            spatialMode: ESpatialMode.Positional3D,
            position: targetPos));
    }
}
