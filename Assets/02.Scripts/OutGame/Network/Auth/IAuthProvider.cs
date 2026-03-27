public interface IAuthProvider
{
    string PlayerId { get; }
    void Init();
}