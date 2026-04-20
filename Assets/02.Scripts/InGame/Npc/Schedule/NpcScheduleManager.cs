using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class NpcScheduleManager : MonoBehaviour
{
    public static NpcScheduleManager Instance { get; private set; }

    private HashSet<NpcController> _npcs = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        TimeEvents.OnMinuteChanged += HandleTimeChanged;
        TimeEvents.OnNetDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        TimeEvents.OnMinuteChanged -= HandleTimeChanged;
        TimeEvents.OnNetDayChanged -= HandleDayChanged;
    }

    public void Register(NpcController npc)
    {
        if (npc == null || _npcs.Contains(npc)) return;

        _npcs.Add(npc);
    }

    public void Unregister(NpcController npc)
    {
        if (npc == null) return;

        _npcs.Remove(npc);
    }

    private void HandleTimeChanged(GameTime currentGameTime)
    {
        UpdateNpcSchedules(currentGameTime);
    }

    private void UpdateNpcSchedules(GameTime currentTime)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        foreach (var npc in _npcs)
        {
            if (npc.TryGetNextScheduleEntry(currentTime, out var entry))
            {
                // ExecuteSchedule 성공 시에만 인덱스를 전진하고, 실패하면 다음 분에 재시도합니다.
                if (npc.ExecuteSchedule(entry))
                {
                    npc.AdvanceScheduleIndex();
                }
            }
        }
    }

    // 하루가 끝나면 스케줄을 리셋합니다.
    private void HandleDayChanged()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        foreach (var npc in _npcs)
        {
            npc.ResetScheduleForNewDay();
        }
    }
}