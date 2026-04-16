using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectData", menuName = "Scriptable Objects/World Effect Data")]
public class WorldEffectDataSO : ScriptableObject, IWorldEffect
{
    [Header("Identity")]
    [SerializeField] private string _effectId;
    [SerializeField] private EWorldEffectKind _kind;
    [SerializeField] private EWorldEffectCategory _category;

    [Header("Display")]
    [SerializeField] private string _displayName;
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    [Header("Modifier")]
    [Tooltip("퍼센트 배율. 0.2 = +20%, -0.15 = -15%")]
    [SerializeField] private float _percentModifier;

    public string EffectId => _effectId;
    public EWorldEffectKind Kind => _kind;
    public EWorldEffectCategory Category => _category;
    public string DisplayName => _displayName;
    public string Description => _description;
    public float PercentModifier => _percentModifier;

    public virtual void Apply(WorldEffectEntry entry) { }
    public virtual void Remove(WorldEffectEntry entry) { }
}
