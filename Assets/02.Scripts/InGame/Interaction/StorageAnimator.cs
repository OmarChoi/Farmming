using System.Threading;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// 창고 뚜껑 열고 닫는 애니메이션. _lidObject의 로컬 z 회전을 보간한다.
/// RPC 브로드캐스트로 모든 클라이언트에 같은 동작을 전파하되,
/// 시각 효과이므로 엄격한 상태 동기화는 하지 않는다.
public class StorageAnimator : MonoBehaviourPun
{
    [SerializeField] private GameObject _lidObject;
    [SerializeField] private float _openAngle = -20f;
    [SerializeField] private float _closedAngle = 90f;
    [SerializeField] private float _duration = 0.3f;

    private CancellationTokenSource _cts;
    private float _currentAngle;
    private Quaternion _baseLocalRotation;

    private void Awake()
    {
        if (_lidObject != null)
            _baseLocalRotation = _lidObject.transform.localRotation;

        _currentAngle = _closedAngle;
        ApplyAngle(_currentAngle);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void OnOpenAnimation()
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC(nameof(RPC_PlayOpen), RpcTarget.All);
        else
            AnimateAsync(_openAngle).Forget();
    }

    public void OnCloseAnimation()
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC(nameof(RPC_PlayClose), RpcTarget.All);
        else
            AnimateAsync(_closedAngle).Forget();
    }

    [PunRPC]
    private void RPC_PlayOpen() => AnimateAsync(_openAngle).Forget();

    [PunRPC]
    private void RPC_PlayClose() => AnimateAsync(_closedAngle).Forget();

    private async UniTaskVoid AnimateAsync(float targetAngle)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        float startAngle = _currentAngle;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            _currentAngle = Mathf.Lerp(startAngle, targetAngle, t);
            ApplyAngle(_currentAngle);

            bool canceled = await UniTask.Yield(PlayerLoopTiming.Update, token)
                .SuppressCancellationThrow();
            if (canceled) return;
        }

        _currentAngle = targetAngle;
        ApplyAngle(_currentAngle);
    }

    private void ApplyAngle(float x)
    {
        if (_lidObject == null) return;

        _lidObject.transform.localRotation = _baseLocalRotation * Quaternion.Euler(x, 0f, 0f);
    }
}