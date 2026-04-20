using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum ENpcVoiceType
{
    Male,
    Female,
    Child,
    Deep,
    Custom,
}

// PyAnimalese(https://github.com/hwi-middle/PyAnimalese) 알고리즘 C# 포팅.
// 원본: pitch = 2^(baseOctaves * uniform(0.96, 1.15)) 를 재생 샘플레이트에 적용.
// Unity에서는 AudioSource.pitch가 동일한 역할(속도·피치 배율)을 한다.
[RequireComponent(typeof(AudioSource))]
public class NpcVoicePlayer : MonoBehaviour
{
    private const int HangulBase = 0xAC00;
    private const int HangulEnd = 0xD7A3;
    private const int ChosungDivisor = 588;
    private const int ChosungCount = 19;

    private const float VariationMin = 0.96f;
    private const float VariationMax = 1.15f;
    private const float UnityPitchMin = 0.1f;
    private const float UnityPitchMax = 3.0f;

    [Header("초성 오디오 클립 (ㄱ, ㄲ, ㄴ, ㄷ, ㄸ, ㄹ, ㅁ, ㅂ, ㅃ, ㅅ, ㅆ, ㅇ, ㅈ, ㅉ, ㅊ, ㅋ, ㅌ, ㅍ, ㅎ)")]
    [SerializeField] private AudioClip[] _chosungClips = new AudioClip[ChosungCount];

    [Header("목소리 프리셋")]
    [SerializeField] private ENpcVoiceType _voiceType = ENpcVoiceType.Male;

    [Header("커스텀 옥타브 (VoiceType = Custom 일 때만 사용)")]
    [Tooltip("원본 PyAnimalese 기본값은 2.0 (≈4배). 낮출수록 저음, 높일수록 고음.")]
    [Range(0.1f, 2.5f)]
    [SerializeField] private float _customOctaves = 1.0f;

    [Header("믹서 라우팅 (SoundManager의 Effect Group 연결 권장)")]
    [SerializeField] private AudioMixerGroup _outputMixerGroup;

    [Header("재생 옵션")]
    [Range(0f, 1f)]
    [SerializeField] private float _volume = 0.7f;
    [SerializeField] private bool _skipDuplicateConsonant = true;

    [Header("스트리밍 옵션 (AI 대화용)")]
    [Tooltip("LLM 토큰 버스트를 풀어낼 간격(초). UI 타자기 속도와 맞추면 자연스럽다.")]
    [SerializeField] private float _streamInterval = 0.07f;

    private AudioSource _source;
    private int _lastChosungIndex = -1;

    private readonly Queue<char> _streamQueue = new Queue<char>();
    private Coroutine _streamCoroutine;
    private string _lastStreamedText = string.Empty;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        if (_outputMixerGroup != null)
        {
            _source.outputAudioMixerGroup = _outputMixerGroup;
        }
    }

    public void PlayChar(char ch)
    {
        if (_chosungClips == null || _chosungClips.Length < ChosungCount) return;
        if (ch < HangulBase || ch > HangulEnd)
        {
            _lastChosungIndex = -1;
            return;
        }

        int idx = (ch - HangulBase) / ChosungDivisor;
        if (idx < 0 || idx >= ChosungCount) return;

        if (_skipDuplicateConsonant && idx == _lastChosungIndex) return;
        _lastChosungIndex = idx;

        AudioClip clip = _chosungClips[idx];
        if (clip == null) return;

        _source.pitch = ComputePitch();
        _source.PlayOneShot(clip, _volume);
    }

    public void ResetRepeatFilter()
    {
        _lastChosungIndex = -1;
    }

    public void Stop()
    {
        StopStreaming();
        if (_source != null)
        {
            _source.Stop();
        }
        _lastChosungIndex = -1;
    }

    // LLM 스트리밍용: partial은 지금까지 누적된 전체 텍스트(델타 아님).
    // 이전에 전달된 텍스트와의 차분만 큐에 쌓아 고정 간격으로 재생한다.
    public void PlayStreaming(string fullText)
    {
        if (string.IsNullOrEmpty(fullText)) return;

        // 새 응답 시작 감지: 이전보다 짧거나 접두사가 달라진 경우
        if (fullText.Length < _lastStreamedText.Length || !fullText.StartsWith(_lastStreamedText))
        {
            _lastStreamedText = string.Empty;
        }

        for (int i = _lastStreamedText.Length; i < fullText.Length; i++)
        {
            _streamQueue.Enqueue(fullText[i]);
        }
        _lastStreamedText = fullText;

        if (_streamCoroutine == null && isActiveAndEnabled)
        {
            _streamCoroutine = StartCoroutine(DrainStreamQueue());
        }
    }

    public void BeginStreaming()
    {
        StopStreaming();
    }

    public void StopStreaming()
    {
        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }
        _streamQueue.Clear();
        _lastStreamedText = string.Empty;
    }

    private IEnumerator DrainStreamQueue()
    {
        WaitForSeconds wait = new WaitForSeconds(_streamInterval);
        while (_streamQueue.Count > 0)
        {
            PlayChar(_streamQueue.Dequeue());
            yield return wait;
        }
        _streamCoroutine = null;
    }

    public void SetVoiceType(ENpcVoiceType voiceType)
    {
        _voiceType = voiceType;
    }

    private float ComputePitch()
    {
        // PyAnimalese: octaves = baseOctaves * uniform(0.96, 1.15), pitch = 2^octaves
        float baseOctaves = GetBaseOctaves();
        float variation = Random.Range(VariationMin, VariationMax);
        float pitch = Mathf.Pow(2f, baseOctaves * variation);
        return Mathf.Clamp(pitch, UnityPitchMin, UnityPitchMax);
    }

    private float GetBaseOctaves()
    {
        // 원본 기본값은 2.0이지만 Unity의 AudioSource.pitch 권장 상한(3.0)을 초과한다.
        // 타입별로 현실적인 범위 내에서 캐릭터 구분을 준다. (2^1.58 ≈ 3.0)
        switch (_voiceType)
        {
            case ENpcVoiceType.Male:   return 0.90f; // 2^0.9  ≈ 1.87x
            case ENpcVoiceType.Female: return 1.25f; // 2^1.25 ≈ 2.38x
            case ENpcVoiceType.Child:  return 1.45f; // 2^1.45 ≈ 2.73x
            case ENpcVoiceType.Deep:   return 0.55f; // 2^0.55 ≈ 1.46x
            case ENpcVoiceType.Custom: return _customOctaves;
            default:                   return 1.0f;
        }
    }
}