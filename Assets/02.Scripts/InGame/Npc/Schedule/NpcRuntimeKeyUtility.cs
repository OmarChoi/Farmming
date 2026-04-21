using UnityEngine;

public static class NpcRuntimeKeyUtility
{
    public static string CreateBuildingNpcKey(NpcDataSO data, BuildingSaveData saveData)
    {
        if (data == null || saveData == null) return string.Empty;

        return $"{data.NpcId}_{saveData.AnchorX:F2}_{saveData.AnchorY:F2}_{saveData.AnchorZ:F2}";
    }
}
