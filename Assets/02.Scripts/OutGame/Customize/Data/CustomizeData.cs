using UnityEngine;

public class CustomizeData : MonoBehaviour
{
    public static CustomizeData Instance { get; private set; }

    private CustomizeSaveData _data = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CopyFrom(CharacterPartSwapper swapper)
    {
        _data = swapper.CreateSaveData();
    }

    public void ApplyTo(CharacterPartSwapper swapper)
    {
        swapper.ApplySaveData(_data);
    }
}