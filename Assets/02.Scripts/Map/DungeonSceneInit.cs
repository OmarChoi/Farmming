using UnityEngine;

public class DungeonSceneInit : MonoBehaviour
{
    [SerializeField] private int _floor = 1;

    private void Start()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length > 0)
        {
            MapManager.Instance.EnterDungeon(_floor, players[0].transform);
            for (int i = 1; i < players.Length; i++)
                players[i].transform.position = players[0].transform.position;
        }
        else
        {
            MapManager.Instance.EnterDungeon(_floor);
        }
    }
}