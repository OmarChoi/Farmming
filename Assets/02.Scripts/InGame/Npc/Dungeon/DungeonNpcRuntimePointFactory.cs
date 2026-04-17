using UnityEngine;

public static class DungeonNpcRuntimePointFactory
{
    public static GameObject CreateSpawnPoint(
        NpcDataSO npcData,
        string runtimeNpcKey,
        Vector3 spawnPosition,
        string locationKey,
        ENpcLocationType locationType = ENpcLocationType.Dungeon,
        Transform parent = null)
    {
        GameObject go = new GameObject($"DungeonNpcSpawnPoint_{npcData.NpcId}");
        if (parent != null)
        {
            go.transform.SetParent(parent);
        }

        go.transform.position = spawnPosition;
        go.transform.rotation = Quaternion.identity;

        NpcLocationAnchor anchor = go.AddComponent<NpcLocationAnchor>();
        anchor.Initialize(npcData, runtimeNpcKey, locationType, locationKey, go.transform, go);

        return go;
    }
}
