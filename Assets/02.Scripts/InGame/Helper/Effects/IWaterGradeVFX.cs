using System;
using UnityEngine;

public interface IWaterGradeVFX
{
    // 물 액션 시작 시 호출
    // onWaterOpen: WaterOpen을 트리거할 콜백 (Epic/Legendary는 내부에서 0.2초 후 호출)
    // onComplete:  시각 연출이 완전히 끝났을 때 호출 (Epic/Legendary만 사용, Normal은 Update로 감지)
    void BeginAction(Action onWaterOpen, Action onComplete);

    // WaterOpen 시점에 헬퍼 본체에 붙는 VFX 스폰 (Epic: 물방울 아우라, Legendary: 오로라)
    void SpawnHelperVFX();

    // 각 타겟 셀에 대한 VFX 스폰
    // onCellLand: 착지 시 게임 효과 적용을 위한 콜백(cell, isCenter)
    void SpawnCellVFX(TerrainCell cell, Vector3 targetPos, bool isCenter,
        Vector3 spawnPos, Action<TerrainCell, bool> onCellLand);

    // 진행 중인 코루틴 강제 중단 (OnDisable, 조기 취소 시 사용)
    void Cancel();
}
