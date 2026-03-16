using UnityEngine;

/// <summary>
/// 에디터 브러시 데이터. 현재 선택된 페인팅 모드와 설정을 담는다.
/// </summary>
public class TerrainBrush
{
    public CellType CellType = CellType.Dirt;
    public int DirtLevel = 1;
    public GridObjectType ObjectType = GridObjectType.None;
    public int ObjectLevel = 1;
    public bool IsEraser;

    /// <summary>
    /// 현재 브러시 설정으로 TerrainCellData 생성.
    /// </summary>
    public TerrainCellData CreateCellData()
    {
        return new TerrainCellData(CellType, DirtLevel, ObjectType, ObjectLevel);
    }
}