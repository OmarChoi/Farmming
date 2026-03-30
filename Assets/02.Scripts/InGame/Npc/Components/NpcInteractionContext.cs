using UnityEngine;

public class NpcInteractionContext
{
    public NpcController Npc { get; }
    public Transform Interactor { get; }
    public NpcInteractionComponent InteractionComponent { get; }
    public string NpcName => Npc.Data.NpcName;
    public string NpcId => Npc.Data.NpcId;

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
