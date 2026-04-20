using UnityEngine;

public abstract class UI_InfoSectionBase : MonoBehaviour
{
    protected PlayerController _playerController;
    public abstract void Subscribe(PlayerController playerController);

    public abstract void Unsubscribe();

    public abstract void Refresh();
}
