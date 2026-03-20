using UnityEngine;

public class DungeonSceneInit : MonoBehaviour
{
    [SerializeField] private int _floor = 1;
    
    // TODO : 추후 생각
    private void Start()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length > 0)
        {
            MapManager.Instance.EnterDungeon(_floor, players[0].transform);
            for (int i = 1; i < players.Length; i++)
            {
                var cc = players[i].GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                players[i].transform.position = players[0].transform.position;
                if (cc != null) cc.enabled = true;
            }
        }
        else
        {
            MapManager.Instance.EnterDungeon(_floor);
        }
    }
}