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
}