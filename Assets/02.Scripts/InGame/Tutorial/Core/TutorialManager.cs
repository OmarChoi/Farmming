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
    private PlayerQuestAbility _playerQuestAbility;
    private NpcController _tutorialNpcController;

    private bool _sceneReady;
    private bool _dialogueReady;
    private bool _playerQuestReady;
    private bool _tutorialStarted;

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

    private void OnEnable()
    {
        GameSceneInit.OnLocalPlayerSceneReady += HandleLocalPlayerSceneReady;
        NpcDialogueController.OnDialogueReady += HandleDialogueReady;

        if (NpcDialogueController.Instance != null && NpcDialogueController.Instance.IsReady)
        {
            HandleDialogueReady();
        }
    }

    private void OnDisable()
    {
        GameSceneInit.OnLocalPlayerSceneReady -= HandleLocalPlayerSceneReady;
        NpcDialogueController.OnDialogueReady -= HandleDialogueReady;

        UnbindPlayerQuestAbility();
    }

    private void HandleLocalPlayerSceneReady(PlayerController player)
    {
        if (player == null || !player.IsMine) return;

        _currentPlayer = player;
        _sceneReady = true;

        BindPlayerQuestAbility(player);
        EvaluateTutorialStart();
    }

    private void BindPlayerQuestAbility(PlayerController player)
    {
        UnbindPlayerQuestAbility();

        _playerQuestAbility = player.GetAbility<PlayerQuestAbility>();
        if (_playerQuestAbility == null) return;

        _playerQuestAbility.OnInitialized += HandlePlayerQuestInitialized;
        _playerQuestReady = _playerQuestAbility.IsInitialized;

        if (_playerQuestReady)
        {
            EvaluateTutorialStart();
        }
    }

    private void UnbindPlayerQuestAbility()
    {
        if (_playerQuestAbility != null)
        {
            _playerQuestAbility.OnInitialized -= HandlePlayerQuestInitialized;
            _playerQuestAbility = null;
        }

        _playerQuestReady = false;
    }

    private void HandlePlayerQuestInitialized()
    {
        _playerQuestReady = true;
        EvaluateTutorialStart();
    }

    private void HandleDialogueReady()
    {
        _dialogueReady = true;
        EvaluateTutorialStart();
    }

    private void EvaluateTutorialStart()
    {
        if (_tutorialStarted) return;
        if (_currentPlayer == null) return;
        if (!_sceneReady || !_playerQuestReady || !_dialogueReady) return;
        if (_tutorialNpc == null || _tutorialProgressController == null) return;
        if (GameSceneInit.ReturningFromDungeon) return;

        if (_playerQuestAbility == null) return;
        if (_playerQuestAbility.TutorialState == ETutorialState.Completed) return;

        StartTutorial();
    }

    private void StartTutorial()
    {
        if (_currentPlayer == null)
        {
            ClearTutorial();
            return;
        }

        DespawnTutorialNpc();

        bool isFirstTutorialQuestCompleted =
            _tutorialProgressController != null &&
            _tutorialProgressController.IsFirstTutorialQuestCompleted();

        _tutorialNpcController = TutorialNpcSpawner.SpawnNearPlayer(
            _tutorialNpc,
            _currentPlayer,
            isFirstTutorialQuestCompleted);

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

        _tutorialStarted = true;
        StartCoroutine(BeginTutorialInteraction());
    }

    private IEnumerator BeginTutorialInteraction()
    {
        yield return null;

        if (_tutorialNpcController == null || _currentPlayer == null || _tutorialProgressController == null)
        {
            ClearTutorial();
            yield break;
        }

        NpcInteractionComponent npcInteraction = _tutorialNpcController.GetComponent<NpcInteractionComponent>();
        PlayerNPCInteractionAbility playerInteraction = _currentPlayer.GetAbility<PlayerNPCInteractionAbility>();

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
        _tutorialProgressController?.ResetRuntimeState();
        _tutorialStarted = false;
        _currentPlayer = null;

        if (SaveManager.Instance != null && RoomManager.Instance != null)
        {
            int slot = RoomManager.Instance.SelectedSlot;
            SaveManager.Instance.RequestPlayerOnlySave(slot);
        }
    }

    private void ClearTutorial()
    {
        DespawnTutorialNpc();
        _tutorialProgressController?.ResetRuntimeState();
        _tutorialStarted = false;
        _currentPlayer = null;
    }

    public void DespawnTutorialNpc()
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
