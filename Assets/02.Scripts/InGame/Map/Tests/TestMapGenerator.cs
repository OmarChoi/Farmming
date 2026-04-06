using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
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
            if (_mapManager.IsDungeon)
            {
                ReturnToVillage();
                return;
            }

            _mapManager.GenerateVillage(_player);
            Debug.Log($"Village generated. Player: {_player.position}");
            _mapNavMeshController.BuildInitialNavMesh();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            EnterDungeon().Forget();
        }
    }

    private async UniTaskVoid EnterDungeon()
    {
        if (SaveManager.Instance != null)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            await SaveManager.Instance.SaveAsync(slot);
        }

        DungeonSceneInit.FloorOverride = 1;
        SceneTransitionData.Type = ETransitionType.VillageToDungeon;
        SceneTransitionData.DungeonFloor = 1;

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                var roomProps = new Hashtable
                {
                    { SceneTransitionRoomProps.TransitionType, (int)ETransitionType.VillageToDungeon },
                    { SceneTransitionRoomProps.DungeonFloor, 1 }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }

            PhotonNetwork.LoadLevel(SceneName.Loading);
        }
        else
            SceneManager.LoadScene(SceneName.Loading);
    }

    private void ReturnToVillage()
    {
        SceneTransitionData.Type = ETransitionType.DungeonToVillage;
        GameSceneInit.ReturningFromDungeon = true;

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                var roomProps = new Hashtable
                {
                    { SceneTransitionRoomProps.TransitionType, (int)ETransitionType.DungeonToVillage }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }

            PhotonNetwork.LoadLevel(SceneName.Loading);
        }
        else
            SceneManager.LoadScene(SceneName.Loading);
    }
}
