using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("튜토리얼 NPC")]
    [SerializeField] private NpcDataSO _tutorialNpc;

    [Header("TutorialProgressController")]
    [SerializeField] private TutorialProgressController _tutorialProgressController;

    private PlayerController _currentPlayer;
    private Transform _playerTransform;
    private NpcController _tutorialNpcController;
    private bool _isTutorialStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_tutorialProgressController == null)
        {
            _tutorialProgressController = FindFirstObjectByType<TutorialProgressController>();
        }
    }

    public void TryStartTutorial(PlayerController player)
    {
        if (_isTutorialStarted || player == null || _tutorialNpc == null) return;

        PlayerQuestAbility questAbility = player.GetAbility<PlayerQuestAbility>();
        if (questAbility == null) return;
        if (questAbility.TutorialState == ETutorialState.Completed) return;

        _currentPlayer = player;
        _playerTransform = player.transform;

        if (QuestManager.Instance == null || !QuestManager.Instance.IsLoaded)
        {
            StartCoroutine(WaitAndStartTutorialCoroutine());
            return;
        }

        StartTutorial();
    }

    private IEnumerator WaitAndStartTutorialCoroutine()
    {
        while (QuestManager.Instance == null || !QuestManager.Instance.IsLoaded)
        {
            yield return null;
        }
        if (_currentPlayer == null)
        {
            yield break;
        }
        PlayerQuestAbility questAbility = _currentPlayer.GetAbility<PlayerQuestAbility>();
        if (questAbility == null || questAbility.TutorialState == ETutorialState.Completed)
        {
            yield break;
        }

        StartTutorial();
    }

    private void StartTutorial()
    {
        if (_playerTransform == null)
        {
            ClearTutorial();
            return;
        }

        DespawnTutorialNpc();

        _tutorialNpcController = TutorialNpcSpawner.SpawnNearPlayer(_tutorialNpc, _playerTransform);
        if (_tutorialNpcController == null)
        {
            ClearTutorial();
            return;
        }
        if (_tutorialProgressController == null)
        {
            ClearTutorial();
            return;
        }
        bool began = _tutorialProgressController.BeginTutorial(_currentPlayer, _tutorialNpcController);
        if (!began)
        {
            ClearTutorial();
            return;
        }

        _isTutorialStarted = true;
        StartCoroutine(BeginTutorialInteraction());
    }

    private IEnumerator BeginTutorialInteraction()
    {
        yield return null;

        if (_tutorialNpcController == null || _playerTransform == null || _tutorialProgressController == null)
        {
            ClearTutorial();
            yield break;
        }

        NpcInteractionComponent npcInteraction = _tutorialNpcController.GetComponent<NpcInteractionComponent>();
        PlayerNPCInteractionAbility playerInteraction = _playerTransform.GetComponentInChildren<PlayerNPCInteractionAbility>();

        if (npcInteraction == null || playerInteraction == null)
        {
            ClearTutorial();
            yield break;
        }

        // 자동으로 상호작용을 시작합니다.
        playerInteraction.BeginAutoInteraction(npcInteraction);
    }

    public void CompleteTutorial()
    {
        if (_currentPlayer != null)
        {
            PlayerQuestAbility questAbility = _currentPlayer.GetAbility<PlayerQuestAbility>();
            if (questAbility != null)
            {
                questAbility.SetTutorialState(ETutorialState.Completed);
            }
        }
        _isTutorialStarted = false;
        _currentPlayer = null;
        _playerTransform = null;

        if (SaveManager.Instance != null && RoomManager.Instance != null)
        {
            int slot = RoomManager.Instance.SelectedSlot;
            SaveManager.Instance.SaveAsync(slot).Forget();
        }
    }

    private void ClearTutorial()
    {
        DespawnTutorialNpc();
        _isTutorialStarted = false;
        _currentPlayer = null;
        _playerTransform = null;
    }

    // 기존에 존재하는 튜토리얼 NPC가 있다면 제거합니다.
    private void DespawnTutorialNpc()
    {
        if (_tutorialNpcController == null) return;

        if (NpcSpawnManager.Instance != null)
        {
            NpcSpawnManager.Instance.Despawn(_tutorialNpcController);
        }
        else
        {
            Destroy(_tutorialNpcController.gameObject);
        }

        _tutorialNpcController = null;
    }
}
