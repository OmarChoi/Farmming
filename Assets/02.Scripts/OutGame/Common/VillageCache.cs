using System.Collections.Generic;
using UnityEngine;

public static class VillageCache
{
    public static TerrainSaveData Terrain { get; set; }
    public static List<BuildingSaveData> Buildings { get; set; }
    public static VillageSaveData Village { get; set; }

    // 플레이어 위치 캐시 (PlayerId → position/rotation)
    private static readonly Dictionary<string, Vector3> _positions = new();
    private static readonly Dictionary<string, float> _rotations = new();

    public static bool HasCache => Terrain != null;

    public static void Capture(TerrainGridManager gridManager)
    {
        Terrain = gridManager.ExportSaveData();
        Buildings = BuildingManager.Instance != null
            ? BuildingManager.Instance.ExportBuildings()
            : null;
        Village = VillageLevelManager.Instance != null
            ? VillageLevelManager.Instance.ExportSaveData()
            : null;
    }

    public static void CapturePlayerPositions()
    {
        _positions.Clear();
        _rotations.Clear();

        var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (string.IsNullOrEmpty(p.PlayerId)) continue;
            _positions[p.PlayerId] = p.transform.position;
            _rotations[p.PlayerId] = p.transform.eulerAngles.y;
        }
    }

    public static void RestorePlayerPositions()
    {
        if (_positions.Count == 0) return;

        var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (string.IsNullOrEmpty(p.PlayerId)) continue;
            if (!_positions.TryGetValue(p.PlayerId, out var pos)) continue;

            p.transform.position = pos;
            if (_rotations.TryGetValue(p.PlayerId, out var rotY))
                p.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        }
    }

    public static void Clear()
    {
        Terrain = null;
        Buildings = null;
        Village = null;
        _positions.Clear();
        _rotations.Clear();
    }
}
