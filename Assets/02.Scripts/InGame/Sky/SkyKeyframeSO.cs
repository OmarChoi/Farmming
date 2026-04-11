using UnityEngine;

[CreateAssetMenu(fileName = "SkyKeyframe", menuName = "Skybox/Keyframe")]
public class SkyKeyframeSO : ScriptableObject
{
    public Cubemap Cubemap;

    [Tooltip("이 키프레임에서 다음 키프레임으로 전환할 때 사용하는 커브")]
    public AnimationCurve BlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Directional Light")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color LightColor = Color.white;

    [Min(0f)]
    public float LightIntensity = 1f;
}
