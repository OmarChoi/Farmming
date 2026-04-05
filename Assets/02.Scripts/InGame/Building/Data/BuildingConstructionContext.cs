using UnityEngine;

public readonly struct BuildingConstructionContext
{
    public readonly Shader RevealLitShader;
    public readonly Shader GhostRevealShader;
    public readonly Material GhostMaterial;

    public BuildingConstructionContext(Shader revealLitShader, Shader ghostRevealShader, Material ghostMaterial)
    {
        RevealLitShader = revealLitShader;
        GhostRevealShader = ghostRevealShader;
        GhostMaterial = ghostMaterial;
    }

    public bool HasRequiredShaders => RevealLitShader != null && GhostRevealShader != null;
}
