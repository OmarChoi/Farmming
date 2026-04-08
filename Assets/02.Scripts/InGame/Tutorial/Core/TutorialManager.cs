using Photon.Pun;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private bool _isMapReady;
    private bool _isPlayerReady;
    private bool _isTutorialStarted;

    private Transform _playerTransform;
    
    [SerializeField] private NpcDataSO _tutorialNpc;

    public void NotifyMapReady()
    {
        _isMapReady = true;
        TryStartTutorial();
    }

    public void NotifyPlayerReady(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _isPlayerReady = true;
        TryStartTutorial();
    }

    private void TryStartTutorial()
    {
        if (_isTutorialStarted) return;
        if (!_isMapReady) return;
        if (!_isPlayerReady) return;

        _isTutorialStarted = true;
        StartTutorial(_playerTransform);
    }

    private void StartTutorial(Transform playerTransform)
    {
        TutorialNpcSpawner.SpawnNearPlayer(_tutorialNpc, playerTransform);
    }
}
