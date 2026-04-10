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
        if (pageData == null)
        {
            if (_titleText != null)
            {
                _titleText.text = "";
            }
            if (_descriptionText != null)
            {
                _descriptionText.text = "";
            }
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.clip = null;
            }
            if (_videoImage != null)
            {
                _videoImage.gameObject.SetActive(false);
            }
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = pageData.Title;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = _wrapper != null
                ? _wrapper.WrapText(pageData.Description, _descriptionText)
                : pageData.Description;
        }

        RefreshVideo(pageData.VideoClip);
    }

    private async void RefreshVideo(VideoClip clip)
    {
        int requestId = ++_videoRequestId;

        _videoPlayer.Stop();
        _videoPlayer.clip = clip;

        await PrepareVideo(_videoPlayer);

        // 이전에 요청이 있었으면 무시합니다.
        if (requestId != _videoRequestId) return;

        _videoPlayer.Play();
    }

    private async UniTask PrepareVideo(VideoPlayer player)
    {
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
}