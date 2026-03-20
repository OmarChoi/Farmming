using UnityEngine;

public class TestMapGenerator : MonoBehaviour
{
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private Transform _player;

    [Header("Generate")]
    [SerializeField] private KeyCode _generateVillageKey = KeyCode.F1;

    private void Update()
    {
        if (Input.GetKeyDown(_generateVillageKey))
        {
            _mapManager.GenerateVillage(_player);
            Debug.Log($"Village generated. Player: {_player.position}");
        }
    }
}