using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizeSceneManager : MonoBehaviour
{
    [SerializeField] private CharacterPartSwapper _partSwapper;
    [SerializeField] private string _gameSceneName = "GameScene";

    public void OnConfirm()
    {
        CustomizeData.Instance.CopyFrom(_partSwapper);

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.LoadLevel(_gameSceneName);
        else
            SceneManager.LoadScene(_gameSceneName);
    }
}