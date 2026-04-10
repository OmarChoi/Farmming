using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UI_InfoPageView : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private TypewriterWithWrap _wrapper;

    [Header("설명 제목")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("설명 비디오")]
    [SerializeField] private RawImage _videoImage;
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RenderTexture _renderTexture;

    private int _videoRequestId = 0;

    [Header("설명 텍스트")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    private void Awake()
    {
        if (_videoPlayer != null && _renderTexture != null)
        {
            _videoPlayer.targetTexture = _renderTexture;
        }

        if (_videoImage != null && _renderTexture != null)
        {
            _videoImage.texture = _renderTexture;
        }
    }

    public void Refresh(InfoPageData pageData)
    {
        _videoRequestId++;

        if (_titleText != null)
        {
            _titleText.text = pageData != null ? pageData.Title : string.Empty;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = pageData != null
                ? (_wrapper != null ? _wrapper.WrapText(pageData.Description, _descriptionText) : pageData.Description)
                : string.Empty;
        }

        if (pageData == null)
        {
            ResetVideoState();
            return;
        }

        RefreshVideoAsync(pageData.VideoClip, _videoRequestId).Forget();
    }

    private async UniTaskVoid RefreshVideoAsync(VideoClip clip, int requestId)
    {
        ResetVideoState();

        if (_videoPlayer == null || _videoImage == null || clip == null) return;

        _videoImage.gameObject.SetActive(true);

        _videoPlayer.clip = clip;
        _videoPlayer.isLooping = true;

        await PrepareVideo(_videoPlayer);

        // 이전에 요청이 있었으면 무시합니다.
        if (requestId != _videoRequestId) return;

        _videoPlayer.Play();
    }

    private void ResetVideoState()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.Stop();
            _videoPlayer.clip = null;
        }

        ClearRenderTexture();

        if (_videoImage != null)
        {
            _videoImage.gameObject.SetActive(false);
        }
    }

    private void ClearRenderTexture()
    {
        if (_renderTexture == null) return;

        RenderTexture current = RenderTexture.active;
        RenderTexture.active = _renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = current;
    }

    private async UniTask PrepareVideo(VideoPlayer player)
    {
        if (player == null || player.clip == null) return;

        var tcs = new UniTaskCompletionSource();

        void OnPrepared(VideoPlayer video)
        {
            video.prepareCompleted -= OnPrepared;
            tcs.TrySetResult();
        }

        player.prepareCompleted += OnPrepared;
        player.Prepare();

        await tcs.Task;
    }

    private void OnDisable()
    {
        _videoRequestId++;
        ResetVideoState();
    }
}