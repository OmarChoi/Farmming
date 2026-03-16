using UnityEngine;

public class TestSaveLoader : MonoBehaviour
{
    [SerializeField] private SaveManager _saveManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha9))
            _saveManager.Save();

        if (Input.GetKeyDown(KeyCode.Alpha0))
            _saveManager.Load();
    }
}