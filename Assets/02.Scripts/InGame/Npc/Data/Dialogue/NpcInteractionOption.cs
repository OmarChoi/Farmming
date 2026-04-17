using System;
using UnityEngine;

[Serializable]
public class NpcInteractionOption
{
    [SerializeField] private ENpcInteractionType _type;
    [SerializeField] private string _buttonName;

    public ENpcInteractionType Type => _type;
    public string ButtonName => _buttonName;

    public NpcInteractionOption()
    {
    }

    public NpcInteractionOption(ENpcInteractionType type, string buttonName)
    {
        _type = type;
        _buttonName = buttonName;
    }
}
