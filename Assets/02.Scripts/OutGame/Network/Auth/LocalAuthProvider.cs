using UnityEngine;

public class LocalAuthProvider : IAuthProvider
{
    private const string PLAYER_GUID_KEY = "Farmming_PlayerGUID";

    public string PlayerId { get; private set; }

    public void Init()
    {
        if (PlayerPrefs.HasKey(PLAYER_GUID_KEY))
        {
            PlayerId = PlayerPrefs.GetString(PLAYER_GUID_KEY);
        }
        else
        {
            PlayerId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PLAYER_GUID_KEY, PlayerId);
            PlayerPrefs.Save();
        }
    }
}