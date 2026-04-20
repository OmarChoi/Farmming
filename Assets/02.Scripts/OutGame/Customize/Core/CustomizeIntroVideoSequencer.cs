using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 커스터마이징 씬 진입 시 지정된 영상들을 순차적으로 재생한다.
/// 각 영상 사이에는 CanvasGroup을 이용한 페이드 아웃 → 페이드 인 트랜지션이 들어가며,
/// 마지막 영상까지 끝나면 VideoPlayer를 정지하고 루트 오브젝트를 비활성화한다.
/// </summary>
public class CustomizeIntroVideoSequencer : MonoBehaviour
{
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private VideoClip[] _videoClips;

    [Tooltip("영상 위를 덮는 검정 오버레이의 CanvasGroup. 알파 1 = 검정, 0 = 투명.")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;

    [Tooltip("시퀀스 종료 시 비활성화할 루트. 비워두면 영상만 멈춘다.")]
    [SerializeField] private GameObject _videoRoot;

    [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;
    [SerializeField] private bool _playOnStart = true;

    private Coroutine _routine;
    private bool _clipFinished;

    private void Start()
    {
        if (_playOnStart)
            Play();
    }

    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlaySequence());
    }

    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        if (_videoPlayer != null)
        {
            _videoPlayer.loopPointReached -= HandleClipEnd;
            _videoPlayer.Stop();
        }
        if (_videoRoot != null) _videoRoot.SetActive(false);
    }

    private IEnumerator PlaySequence()
    {
        if (_videoPlayer == null || _videoClips == null || _videoClips.Length == 0)
            yield break;

        if (_videoRoot != null) _videoRoot.SetActive(true);

        // 시작은 완전히 가린 상태에서 첫 영상 준비 후 페이드 인
        SetFadeAlpha(1f);

        for (int i = 0; i < _videoClips.Length; i++)
        {
            VideoClip clip = _videoClips[i];
            if (clip == null) continue;

            yield return PlayOne(clip);

            // 마지막 영상이 끝나면 검정 상태 유지 후 종료 처리로 넘어간다
            yield return FadeTo(1f, _fadeDuration);
        }

        _videoPlayer.Stop();
        if (_videoRoot != null) _videoRoot.SetActive(false);
        _routine = null;
    }

    private IEnumerator PlayOne(VideoClip clip)
    {
        _videoPlayer.Stop();
        _videoPlayer.isLooping = false;
        _videoPlayer.clip = clip;
        _videoPlayer.Prepare();
        while (!_videoPlayer.isPrepared) yield return null;

        _clipFinished = false;
        _videoPlayer.loopPointReached -= HandleClipEnd;
        _videoPlayer.loopPointReached += HandleClipEnd;
        _videoPlayer.Play();

        // 페이드 인 (검정 → 투명)
        yield return FadeTo(0f, _fadeDuration);

        // 재생 종료 대기
        while (!_clipFinished) yield return null;

        _videoPlayer.loopPointReached -= HandleClipEnd;
    }

    private void HandleClipEnd(VideoPlayer _) => _clipFinished = true;

    private IEnumerator FadeTo(float target, float duration)
    {
        if (_fadeCanvasGroup == null || duration <= 0f)
        {
            SetFadeAlpha(target);
            yield break;
        }

        float start = _fadeCanvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _fadeCanvasGroup.alpha = target;
    }

    private void SetFadeAlpha(float a)
    {
        if (_fadeCanvasGroup != null) _fadeCanvasGroup.alpha = a;
    }

    private void OnDisable()
    {
        if (_videoPlayer != null)
            _videoPlayer.loopPointReached -= HandleClipEnd;
    }
}