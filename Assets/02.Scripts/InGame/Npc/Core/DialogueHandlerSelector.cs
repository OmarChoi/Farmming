
using UnityEngine;

public class DialogueHandlerSelector
{
    private IDialogueHandler _aiHandler;
    private IDialogueHandler _scriptedHandler;

    public DialogueHandlerSelector(
        IDialogueHandler aiHandler,
        IDialogueHandler scriptedHandler)
    {
        _aiHandler = aiHandler;
        _scriptedHandler = scriptedHandler;
    }

    public IDialogueHandler Resolve()
    {
        if (IsOnline())
        {
            return _aiHandler;
        }

        return _scriptedHandler;
    }

    // 인터넷 연결 상태를 확인하는 메서드입니다. 추후 서버와 통신하는 방식으로 변경될 수 있습니다.
    private bool IsOnline()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
}
