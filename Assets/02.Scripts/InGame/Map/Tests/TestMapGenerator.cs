using Cysharp.Threading.Tasks;
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
        // ?�???�료 ?????�환
        if (SaveManager.Instance != null)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            await SaveManager.Instance.SaveAsync(slot);
        }

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.LoadLevel(SceneName.Dungeon1);
        else
            SceneManager.LoadScene(SceneName.Dungeon1);
    }

    private void ReturnToVillage()
    {

        GameSceneInit.ReturningFromDungeon = true;

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.LoadLevel(SceneName.Game);
        else
            SceneManager.LoadScene(SceneName.Game);
    }
}
