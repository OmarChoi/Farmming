using TMPro;
using Unity.VisualScripting;
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

    [Header("설명 텍스트")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

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

    private void RefreshVideo(VideoClip clip)
    {
        if (_videoPlayer == null) return;

        if (clip == null)
        {
            _videoPlayer.Stop();
            _videoPlayer.clip = null;

            if (_videoImage != null)
            {
                _videoImage.gameObject.SetActive(false);
            }

            return;
        }

        if (_videoImage != null)
        {
            _videoImage.gameObject.SetActive(true);
        }

        _videoPlayer.Stop();
        _videoPlayer.clip = clip;
        _videoPlayer.isLooping = true;
        _videoPlayer.Play();
    }
}