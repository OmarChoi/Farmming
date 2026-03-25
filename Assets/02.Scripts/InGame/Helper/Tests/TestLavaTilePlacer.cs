using UnityEngine;

public class TestLavaTilePlacer : MonoBehaviour
{
    [SerializeField] private GameObject _lavaTilePrefab;
    [SerializeField] private int _width = 5;
    [SerializeField] private int _height = 5;
    [SerializeField] private float _cellSize = 2f;

    private void Start()
    {
        PlaceLavaTiles();
    }

    public void PlaceLavaTiles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                Vector3 pos = new Vector3(x * _cellSize, 0, z * _cellSize);
                GameObject tile = Instantiate(_lavaTilePrefab, pos, Quaternion.identity, transform);
                tile.name = $"LavaTile_{x}_{z}";
            }
        }

        Debug.Log($"용암 타일 {_width * _height}개 배치 완료");
    }
}
