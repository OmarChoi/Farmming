using UnityEngine;

public class NpcInteractionContext: MonoBehaviour
{
    public NpcController Npc { get; }
    public Transform Interactor { get; }
    public NpcInteractionComponent InteractionComponent { get; }
    public string NpcName => Npc.Data.NpcName;

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
