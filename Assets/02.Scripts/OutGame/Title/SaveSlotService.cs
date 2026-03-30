using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class SaveSlotService
{
    private readonly ISaveRepository _repository;
    private readonly int _maxSlots;
    private bool[] _slotStates;

    public SaveSlotService(ISaveRepository repository, int maxSlots)
    {
        _repository = repository;
        _maxSlots = maxSlots;
    }

    public int MaxSlots => _maxSlots;

    public async UniTask RefreshAsync()
    {
        _slotStates = new bool[_maxSlots];
        for (int i = 0; i < _maxSlots; i++)
            _slotStates[i] = await _repository.HasSaveAsync(i);
    }

    public bool HasSave(int slot)
    {
        return _slotStates != null && slot >= 0 && slot < _maxSlots && _slotStates[slot];
    }

    public async UniTask DeleteAsync(int slot)
    {
        await _repository.DeleteAsync(slot);
        if (_slotStates != null && slot >= 0 && slot < _maxSlots)
            _slotStates[slot] = false;
    }

    public int FindFirstEmptySlot()
    {
        if (_slotStates == null) return -1;
        for (int i = 0; i < _slotStates.Length; i++)
        {
            if (!_slotStates[i]) return i;
        }
        return -1;
    }

    public async UniTask<List<string>> GetPlayerIdsAsync(int slot)
    {
        var data = await _repository.LoadAsync(slot);
        if (data == null) return new List<string>();

        var ids = new List<string>();
        foreach (var p in data.Players)
            ids.Add(p.PlayerId);
        return ids;
    }
}