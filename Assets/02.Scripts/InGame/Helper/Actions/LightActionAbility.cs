using System;
using System.Collections;
using System.ComponentModel.Design;
using UnityEngine;

public class LightActionAbility : HelperAbility
{
    //helper 소환된 상태
    //helper 따라다니는 상태
    // spotlight 빛은 helper 머리위 vector3.up * _headOffset 에 위치해있다. _haedOffset은 5f정도, helper근처 빛 범위도 5f 정도의원으로 비추게, 빛 intensity는 2f
    // player의 머리위치를 찾는다.
    // player의 머리 위+helper가 올라간 높이 * _playerHeadOffset 위에 spotlight가 위치하게 한다. 빛 범위는 15f 정도, 빛 intensity는 5f
    // helper 장착상태

    [SerializeField] private Light _light;

    [SerializeField] private float _headOffset = 5f;
    [SerializeField] private float _summonedRange = 5f;
    [SerializeField] private float _summonedIntensity = 2f;

    [SerializeField] private float _playerHeadOffset = 5f;
    [SerializeField] private float _equippedRange = 20f;
    [SerializeField] private float _equippedIntensity = 100f;

    [SerializeField] private float _defaultHeadHeight = 2f;

    [SerializeField] private float _transitionSpeed = 2f;

    private EHelperState _lastState;
    private Coroutine _transitionCoroutine;

    private HelperAnimationAbility _animAbility;

    private void Start()
    {
        _lastState = _owner.State;
        ApplyState(_owner.State, instant: true);
    }

    private void OnEnable()
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        if (_light != null)
        {
            _light.enabled = false;
        }
    }

    private void Update()
    {
        if(_owner.State == _lastState)
        {
            return;
        }
        _lastState = _owner.State;
        ApplyState(_owner.State, instant: false);
    }

    private void ApplyState(EHelperState state, bool instant)
    {
        if(_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        switch(state)
        {
            case EHelperState.Summoned:
                Vector3 summonedPos = Vector3.up*_headOffset;
                if(instant)
                {
                    SetLight(summonedPos, _summonedRange, _summonedIntensity);
                }
                else
                {
                    _transitionCoroutine = StartCoroutine(TransitionLight(summonedPos, _summonedRange, _summonedIntensity));
                }
                    break;

            case EHelperState.Equipped:
                Vector3 playerHeadPos = GetPlayerHeadPos();
                Vector3 equippedPos = playerHeadPos + Vector3.up * _playerHeadOffset;
                if (instant)
                {
                    SetLightWorld(equippedPos, _equippedRange, _equippedIntensity);
                }
                else
                {
                    _transitionCoroutine = StartCoroutine(TransitionLightWorld(equippedPos, _equippedRange, _equippedIntensity));
                }
                break;

            case EHelperState.Inventory:
                if(_light != null)
                {
                    _light.enabled = false;
                }
                break;

        }
    }

    private void SetLight(Vector3 localPos, float range, float intensity)
    {
        if (_light == null)
        {
            return;
        }
        _light.enabled = true;
        _light.transform.localPosition = localPos;
        _light.range = range;
        _light.intensity = intensity;
    }

    private void SetLightWorld(Vector3 worldPos, float range, float intensity)
    {
        if (_light == null)
        {
            return;
        }
        _light.enabled = true;
        _light.transform.position = worldPos;
        _light.range = range;
        _light.intensity = intensity;
    }

    private Vector3 GetPlayerHeadPos()
    {
        if (_owner.PlayerOwner == null)
        {
            return _owner.transform.position + Vector3.up * _defaultHeadHeight;
        }

        CharacterController characterController = _owner.PlayerOwner.GetComponent<CharacterController>();
        if (characterController != null)
        {
            return _owner.PlayerOwner.transform.position + Vector3.up * characterController.height;
        }

        return _owner.PlayerOwner.transform.position + Vector3.up * _defaultHeadHeight;
    }

    private IEnumerator TransitionLight(Vector3 targetLocalPos, float targetRange, float targetIntensity)
    {
        if (_light == null)
        {
            yield break;
        }

        _light.enabled = true;

        Vector3 startPos = _light.transform.localPosition;
        float startRange = _light.range;
        float startIntensity = _light.intensity;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * _transitionSpeed;
            float time = Mathf.SmoothStep(0f, 1f, elapsed);

            _light.transform.localPosition = Vector3.Lerp(startPos, targetLocalPos, time);
            _light.range = Mathf.Lerp(startRange, targetRange, time);
            _light.intensity = Mathf.Lerp(startIntensity, targetIntensity, time);

            yield return null;
        }

        SetLight(targetLocalPos, targetRange, targetIntensity);
    }

    private IEnumerator TransitionLightWorld(Vector3 targetWorldPos, float targetRange, float targetIntensity)
    {
        if (_light == null)
        {
            yield break;
        }

        _light.enabled = true;

        Vector3 startPos = _light.transform.position;
        float startRange = _light.range;
        float startIntensity = _light.intensity;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * _transitionSpeed;
            float time = Mathf.SmoothStep(0f, 1f, elapsed);

            _light.transform.position = Vector3.Lerp(startPos, targetWorldPos, time);
            _light.range = Mathf.Lerp(startRange, targetRange, time);
            _light.intensity = Mathf.Lerp(startIntensity, targetIntensity, time);

            yield return null;
        }

        SetLightWorld(targetWorldPos, targetRange, targetIntensity);
    }
}
