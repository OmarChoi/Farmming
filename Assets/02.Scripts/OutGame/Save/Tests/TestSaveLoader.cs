using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestSaveLoader : MonoBehaviour
{
    [SerializeField] private SaveManager _saveManager;

    private void Update()
    {
        int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;

        if (Input.GetKeyDown(KeyCode.Alpha9))
            _saveManager.SaveAsync(slot).Forget();

        if (Input.GetKeyDown(KeyCode.Alpha0))
            _saveManager.LoadAsync(slot).Forget();
    }
}
