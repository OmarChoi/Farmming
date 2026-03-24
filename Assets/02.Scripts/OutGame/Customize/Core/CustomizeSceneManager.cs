using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizeSceneManager : MonoBehaviour
{
    [SerializeField] private CharacterPartSwapper _partSwapper;
    [SerializeField] private string _gameSceneName = "GameScene";

    public void OnConfirm()
    {
        CustomizeData.Instance.CopyFrom(_partSwapper);
        SceneManager.LoadScene(_gameSceneName);
    }
}