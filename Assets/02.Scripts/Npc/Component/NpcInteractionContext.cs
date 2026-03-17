using UnityEngine;

public class NpcInteractionContext : MonoBehaviour
{
    public NpcController Npc { get; }
    public Transform Interactor { get; }
    public NpcInteractionComponent InteractionComponent { get; }

    public NpcInteractionContext(
        NpcController npc,
        Transform interactor,
        NpcInteractionComponent interactionComponent)
    {
        Npc = npc;
        Interactor = interactor;
        InteractionComponent = interactionComponent;
    }
}
