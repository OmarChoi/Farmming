using UnityEngine;

public class NpcInteractionContext
{
    public NpcController Npc { get; }
    public PlayerController Interactor { get; }
    public NpcInteractionComponent InteractionComponent { get; }
    public string NpcName => Npc.Data.NpcName;
    public string NpcId => Npc.Data.NpcId;

    public NpcInteractionContext(
        NpcController npc,
        PlayerController interactor,
        NpcInteractionComponent interactionComponent)
    {
        Npc = npc;
        Interactor = interactor;
        InteractionComponent = interactionComponent;
    }
}
