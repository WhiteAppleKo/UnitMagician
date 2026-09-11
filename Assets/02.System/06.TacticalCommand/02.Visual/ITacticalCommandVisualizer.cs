using System.Collections.Generic;
using UnityEngine;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령 시각 연출을 위한 Visual Interface입니다.
    /// DLV 아키텍처 원칙에 따라 로직 시스템은 비주얼 구체 클래스를 직접 참조하지 않고
    /// 오직 이 인터페이스를 통해서만 연출을 지시합니다.
    /// </summary>
    public interface ITacticalCommandVisualizer
    {
        /// <summary>
        /// 전술 모드 활성화/비활성화 시 비주얼 오버레이 및 메뉴를 노출/숨김합니다.
        /// </summary>
        void ShowTacticalVisuals(bool show);

        /// <summary>
        /// 마우스 커서 아래 호버링된 타깃 하이라이트 지시
        /// </summary>
        void SetHoverTarget(GameObject target);

        /// <summary>
        /// 타깃 머리 위에 예약 순서 인디케이터([1st], [2nd] 등)를 추가/표시합니다.
        /// </summary>
        void AddTargetIndicator(TacticalCommandEntry entry);

        /// <summary>
        /// 특정 예약 항목의 인디케이터를 제거합니다.
        /// </summary>
        void RemoveTargetIndicator(TacticalCommandEntry entry);

        /// <summary>
        /// 플레이어로부터 예약된 타깃들을 차례로 잇는 조준선/궤적 연결선을 갱신합니다.
        /// </summary>
        void UpdateTrajectoryLines(IReadOnlyList<TacticalCommandEntry> entries, Vector3 originPosition);

        /// <summary>
        /// 순차 격발 실행 시 해당 명령의 타깃 위치에 연쇄 타격 이펙트를 연출합니다.
        /// </summary>
        void PlayCommandExecutionEffect(TacticalCommandEntry entry);

        /// <summary>
        /// 마나 부족 등으로 예약이 차단되었을 때 시각/음향 피드백을 연출합니다.
        /// </summary>
        void PlayManaInsufficientFeedback();

        /// <summary>
        /// 모든 인디케이터, 조준선, 시각 효과를 일괄 정리합니다.
        /// </summary>
        void ClearAllVisuals();
    }
}
