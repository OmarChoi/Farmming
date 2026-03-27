using Photon.Pun;
using UnityEngine;

public class PlayerCustomizeAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private CharacterPartSwapper _partSwapper;

    private void Start()
    {
        if (!_owner.IsMine) return;
        if (CustomizeData.Instance != null)
            Initialize(CustomizeData.Instance.Data);
    }

    public void Initialize(CustomizeSaveData data)
    {
        if (data == null || _partSwapper == null) return;
        _partSwapper.ApplySaveData(data);
        SyncToOthers(data);
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        if (_partSwapper != null)
            saveData.Customize = _partSwapper.CreateSaveData();
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData.Customize == null) return;
        _partSwapper.ApplySaveData(saveData.Customize);
        SyncToOthers(saveData.Customize);
    }

    private void SyncToOthers(CustomizeSaveData data)
    {
        if (!_owner.IsMine) return;
        string json = JsonUtility.ToJson(data);
        _owner.PhotonView.RPC(nameof(PlayerController.RPC_SyncCustomize), RpcTarget.OthersBuffered, json);
    }

    public void ApplyFromJson(string json)
    {
        var data = JsonUtility.FromJson<CustomizeSaveData>(json);
        if (_partSwapper != null)
            _partSwapper.ApplySaveData(data);
    }
}