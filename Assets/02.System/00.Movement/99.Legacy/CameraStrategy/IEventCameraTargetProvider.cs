using UnityEngine;

namespace CameraMovement
{
    /// <summary>
    /// [Legacy — Goal 44에서 폐기] 이벤트 카메라는 이 임시 인터페이스 대신 IEventCameraLookTarget(투사체 위치)을 사용한다.
    /// 레거시 EventTargetTrackingCameraStrategy의 컴파일 유지를 위해서만 남긴다.
    ///
    /// EventTargetTrackingCameraStrategy가 구도를 계산하기 위해 참조하는 두 지점(아군 발사 위치, 보스 위치)을
    /// 노출하는 인터페이스입니다(10번 기획서 10.8절). 02(아군NPC)/03(보스AI)이 아직 구현되지 않아, 현재는
    /// 씬의 임의 테스트 오브젝트를 감싼 EventCameraTargetProvider만 존재합니다 — 02/03 구현 시점에 실제
    /// 발사 위치/보스 위치를 노출하는 구현체로 교체될 예정입니다(Goal 프롬프트 "제외" 절).
    /// </summary>
    public interface IEventCameraTargetProvider
    {
        Vector3 GetAllyLaunchPosition();
        Vector3 GetBossPosition();
    }
}
