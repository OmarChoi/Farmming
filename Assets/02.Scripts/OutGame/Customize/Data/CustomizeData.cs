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

    private void OnEnable()
    {
        PlayerCustomizeAbility.OnReady += OnPlayerCustomizeReady;
    }

    private void OnDisable()
    {
        PlayerCustomizeAbility.OnReady -= OnPlayerCustomizeReady;
    }

    public void CopyFrom(CharacterPartSwapper swapper)
    {
        _data = swapper.CreateSaveData();
    }

    private void OnPlayerCustomizeReady(PlayerCustomizeAbility ability)
    {
        ability.Initialize(_data);
    }
}