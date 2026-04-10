using UnityEngine;
using UnityEngine.Video;
using System;

[Serializable]
public class InfoPageData
{
    public string Title;

    public VideoClip VideoClip;

    [TextArea(3, 10)]
    public string Description;
}
