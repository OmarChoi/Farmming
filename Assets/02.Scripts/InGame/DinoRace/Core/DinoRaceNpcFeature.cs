using UnityEngine;

public class DinoRaceNpcFeature : MonoBehaviour
{
    public DinoRaceHost Host { get; private set; }

    public void Initialize(DinoRaceHost host)
    {
        Host = host;
    }
}
