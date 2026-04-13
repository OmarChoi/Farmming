
public interface ITroubleReceiver
{
    bool CanReceiveTrouble();
    void ApplyTrouble(TroubleContext context);
}
