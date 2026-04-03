using UnityEngine;

public readonly struct GhostConfig
{
    public readonly Material Material;
    public readonly Color ValidColor;
    public readonly Color InvalidColor;

    public GhostConfig(Material material, Color validColor, Color invalidColor)
    {
        Material = material;
        ValidColor = validColor;
        InvalidColor = invalidColor;
    }
}
