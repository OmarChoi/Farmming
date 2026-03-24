using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizeSceneManager : MonoBehaviour
{
    [SerializeField] private CharacterPartSwapper _partSwapper;
    [SerializeField] private CustomizeData _customizeData;
    [SerializeField] private string _gameSceneName = "GameScene";

    public void OnConfirm()
    {
        _customizeData.CopyFrom(_partSwapper);
        SceneManager.LoadScene(_gameSceneName);
    }
}