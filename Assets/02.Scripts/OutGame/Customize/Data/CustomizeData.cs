using UnityEngine;

public class CustomizeData : MonoBehaviour
{
    public static CustomizeData Instance { get; private set; }

    public CustomizeSaveData Data { get; private set; } = new();

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
        Data = swapper.CreateSaveData();
    }
}