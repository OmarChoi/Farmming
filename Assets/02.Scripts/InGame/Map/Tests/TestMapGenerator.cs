using UnityEngine;
using UnityEngine.SceneManagement;

public class TestMapGenerator : MonoBehaviour
{
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private Transform _player;

    [Header("Generate")]
    [SerializeField] private KeyCode _generateVillageKey = KeyCode.F5;
    [SerializeField] private MapNavMeshController _mapNavMeshController;

    private void Update()
    {
        if (Input.GetKeyDown(_generateVillageKey))
        {
            _mapManager.GenerateVillage(_player);
            Debug.Log($"Village generated. Player: {_player.position}");

            _mapNavMeshController.BuildInitialNavMesh();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            SceneManager.LoadScene(SceneName.Dungeon1);
        }
    }
}