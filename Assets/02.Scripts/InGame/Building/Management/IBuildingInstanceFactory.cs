using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 건물 오브젝트의 생성/파기 담당. RemainingDays 등 상태 복원은 호출자가 별도로 수행한다.
/// 싱글: LocalBuildingInstanceFactory, 네트워크 마스터: PhotonBuildingInstanceFactory.
/// 네트워크 클라이언트는 factory를 거치지 않고 PhotonNetwork의 자동 복제 경로로 인스턴스를 받는다.
/// </summary>
public interface IBuildingInstanceFactory
{
    UniTask<BaseBuilding> CreateAsync(
        BuildingInfo info,
        Vector3 position,
        Quaternion rotation);

    /// <summary>
    /// 해당 instance의 파기를 실제로 요청/예약했다면 true. true면 caller가 registry/placement를 해제해도 된다.
    /// false면 caller는 상태를 유지하고 rejected 처리한다.
    /// Photon은 비동기 복제라 "삭제 완료"를 동기로 확인할 수 없으므로 "요청 수락" 의미로 해석한다.
    /// </summary>
    bool Destroy(BaseBuilding building);
}
