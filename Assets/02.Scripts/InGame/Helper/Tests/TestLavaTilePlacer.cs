using UnityEngine;

public class TestLavaTilePlacer : MonoBehaviour
{
    [SerializeField] private int _width = 5;
    [SerializeField] private int _height = 5;

    private void Start()
    {
        PlaceLavaTiles();
    }

    private void PlaceLavaTiles()
    {
        if (TerrainGridManager.Instance == null)
        {
            return;
        }

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                var pos = new Vector3Int(x, 0, z);
                var data = new TerrainCellData(
                    ECellType.Dirt,
                    ETileType.Dungeon2Lava,
                    1,
                    EGridObjectType.None,
                    0,
                    false,
                    isTop: true
                );
                TerrainGridManager.Instance.SetCell(pos, data);
            }
        }

        Debug.Log($"용암 타일 {_width * _height}개 배치 완료");
    }
}
