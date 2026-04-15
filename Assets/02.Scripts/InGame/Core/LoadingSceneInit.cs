using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneInit : MonoBehaviourPunCallbacks
{
    private const string PropLoadingReady = "ldRdy";
    private const string PropTerrainReady = "tRdy";
    private const float ReadyTimeoutSeconds = 15f;
    private const float TransitionResolveTimeoutSeconds = 5f;

    private bool _transitionStarted;

    private void Start()
    {
        StartFlow().Forget();
    }

    private async UniTaskVoid StartFlow()
    {
        if (!PhotonNetwork.IsConnected)
        {
            InitOffline();
            return;
        }

        ETransitionType transitionType = await ResolveTransitionType();
        if (transitionType == ETransitionType.None)
        {
            Debug.LogWarning("[LoadingSceneInit] Unknown transition type, returning to village");
            PhotonNetwork.LoadLevel(SceneName.Game);
            return;
        }

        SceneTransitionData.Type = transitionType;
        ClearPlayerReadyProps();

        switch (transitionType)
        {
            case ETransitionType.VillageToDungeon:
                InitVillageToDungeon();
                break;
            case ETransitionType.DungeonToVillage:
                InitDungeonToVillage();
                break;
        }
    }

    private void InitOffline()
    {
        if (SceneTransitionData.Type == ETransitionType.VillageToDungeon)
        {
            DungeonSceneInit.FloorOverride = SceneTransitionData.DungeonFloor;
            SceneManager.LoadScene(SceneName.Dungeon1);
        }
        else
        {
            GameSceneInit.ReturningFromDungeon = true;
            SceneManager.LoadScene(SceneName.Game);
        }
    }

    private void InitVillageToDungeon()
    {
        LoadingProgress.Begin();
        VillageCache.CapturePlayerPositions();
        if (TerrainGridManager.Instance != null)
            VillageCache.Capture(TerrainGridManager.Instance);

        if (PhotonNetwork.IsMasterClient)
        {
            int seed = System.Environment.TickCount;
            SceneTransitionData.DungeonSeed = seed;
            SceneTransitionData.SeedReady = true;

            var roomProps = new Hashtable
            {
                { SceneTransitionRoomProps.TransitionType, (int)ETransitionType.VillageToDungeon },
                { SceneTransitionRoomProps.DungeonSeed, seed },
                { SceneTransitionRoomProps.DungeonFloor, SceneTransitionData.DungeonFloor }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            SetLocalReady();
            LoadingProgress.Value = 0.2f;
        }
        else
        {
            TryReadSeedFromRoomProps();
        }

        WaitForAllReadyAndTransition().Forget();
    }

    private void InitDungeonToVillage()
    {
        LoadingProgress.Begin();
        GameSceneInit.ReturningFromDungeon = true;

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            var roomProps = new Hashtable
            {
                { SceneTransitionRoomProps.TransitionType, (int)ETransitionType.DungeonToVillage }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }

        SetLocalReady();
        WaitForAllReadyAndTransition().Forget();
    }

    private void TryReadSeedFromRoomProps()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.ContainsKey(SceneTransitionRoomProps.DungeonSeed))
        {
            SceneTransitionData.DungeonSeed = (int)props[SceneTransitionRoomProps.DungeonSeed];

            if (props.ContainsKey(SceneTransitionRoomProps.DungeonFloor))
                SceneTransitionData.DungeonFloor = (int)props[SceneTransitionRoomProps.DungeonFloor];

            SceneTransitionData.SeedReady = true;
            SetLocalReady();
            LoadingProgress.Value = 0.2f;
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (SceneTransitionData.Type == ETransitionType.None &&
            propertiesThatChanged.ContainsKey(SceneTransitionRoomProps.TransitionType))
        {
            TryReadTransitionTypeFromRoomProps();
        }

        if (SceneTransitionData.Type != ETransitionType.VillageToDungeon || SceneTransitionData.SeedReady)
            return;

        if (propertiesThatChanged.ContainsKey(SceneTransitionRoomProps.DungeonSeed))
            TryReadSeedFromRoomProps();
    }

    private void SetLocalReady()
    {
        var props = new Hashtable { { PropLoadingReady, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private async UniTaskVoid WaitForAllReadyAndTransition()
    {
        float timeout = Time.realtimeSinceStartup + ReadyTimeoutSeconds;
        int totalCount = Mathf.Max(PhotonNetwork.PlayerList.Length, 1);

        while (!AreAllPlayersReady() && Time.realtimeSinceStartup <= timeout)
        {
            int readyCount = CountReadyPlayers();

            if (SceneTransitionData.Type == ETransitionType.VillageToDungeon)
                LoadingProgress.Value = 0.2f + (0.3f * readyCount / totalCount);
            else if (SceneTransitionData.Type == ETransitionType.DungeonToVillage)
                LoadingProgress.Value = 0.3f * readyCount / totalCount;

            await UniTask.Yield();
        }

        if (!AreAllPlayersReady())
            Debug.LogWarning("[LoadingSceneInit] Timeout - proceeding with available players");

        if (!PhotonNetwork.IsMasterClient) return;
        if (_transitionStarted) return;

        _transitionStarted = true;

        DungeonSceneInit.FloorOverride = SceneTransitionData.DungeonFloor;

        string targetScene = SceneTransitionData.Type == ETransitionType.VillageToDungeon
            ? SceneName.Dungeon1
            : SceneName.Game;

        if (SceneTransitionData.Type == ETransitionType.VillageToDungeon)
        {
            LoadingProgress.Value = 0.5f;
            LoadingProgress.TrackSceneLoad(0.5f, 0.2f);
        }
        else if (SceneTransitionData.Type == ETransitionType.DungeonToVillage)
        {
            LoadingProgress.Value = 0.3f;
            LoadingProgress.TrackSceneLoad(0.3f, 0.3f);
        }

        PhotonNetwork.LoadLevel(targetScene);
    }

    private bool AreAllPlayersReady()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(PropLoadingReady, out object val))
            {
                if (val is bool b && b) continue;
            }
            return false;
        }
        return true;
    }

    private int CountReadyPlayers()
    {
        int readyCount = 0;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(PropLoadingReady, out object val) &&
                val is bool isReady &&
                isReady)
            {
                readyCount++;
            }
        }

        return readyCount;
    }

    private async UniTask<ETransitionType> ResolveTransitionType()
    {
        if (SceneTransitionData.Type != ETransitionType.None)
            return SceneTransitionData.Type;

        ETransitionType transitionType = TryReadTransitionTypeFromRoomProps();
        if (transitionType != ETransitionType.None)
            return transitionType;

        float timeout = Time.realtimeSinceStartup + TransitionResolveTimeoutSeconds;
        while (Time.realtimeSinceStartup <= timeout)
        {
            transitionType = TryReadTransitionTypeFromRoomProps();
            if (transitionType != ETransitionType.None)
                return transitionType;

            await UniTask.Yield();
        }

        return ETransitionType.None;
    }

    private ETransitionType TryReadTransitionTypeFromRoomProps()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return ETransitionType.None;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.TryGetValue(SceneTransitionRoomProps.TransitionType, out object transitionTypeObj))
            return ETransitionType.None;

        if (!(transitionTypeObj is int transitionTypeInt))
            return ETransitionType.None;

        var transitionType = (ETransitionType)transitionTypeInt;
        SceneTransitionData.Type = transitionType;

        if (props.TryGetValue(SceneTransitionRoomProps.DungeonFloor, out object floorObj) && floorObj is int floor)
            SceneTransitionData.DungeonFloor = floor;

        return transitionType;
    }

    private void ClearPlayerReadyProps()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            var clearPlayer = new Hashtable
            {
                { PropLoadingReady, null },
                { PropTerrainReady, null }
            };
            player.SetCustomProperties(clearPlayer);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // PlayerList is auto-updated by PUN2, readiness check adjusts automatically
    }
}
