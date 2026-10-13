// [99.Legacy / 15-A] 은퇴한 옛 전술 명령(시간정지 예약/시전) 시스템. 시간정지 마법 지정/발동은 01.UnitSystem의 RuntimeDataMultiLockOn 큐 시스템으로 일원화되었다.
// 컴파일만 유지되며 어떤 LifetimeScope/씬에도 등록되지 않는다. 새 코드에서 이 폴더의 타입을 참조하지 말 것(호버/순서 인디케이터/궤적선 연출 참고용 보관).
using UnityEngine;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령 큐 및 연쇄 발동 시스템에 필요한 불변 설정 데이터입니다.
    /// DLV 아키텍처 규칙에 따라 Logic이 배제된 Immutable ScriptableObject로 구성됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataTacticalCommand_", menuName = "TacticalCommand/PureDataTacticalCommand")]
    public class PureDataTacticalCommand : ScriptableObject
    {
        [Header("Queue Limits")]
        [Tooltip("최대 예약 가능 슬롯 수")]
        [SerializeField] private int maxQueueCount = 5;

        [Header("Execution Timing")]
        [Tooltip("예약된 마법 순차 실행 시 각 발동 간격(초)")]
        [SerializeField] private float executionDelay = 0.25f;

        [Header("Targeting Settings")]
        [Tooltip("타깃 탐색 및 선택 최대 거리")]
        [SerializeField] private float maxTargetDistance = 50f;

        [Tooltip("타깃 레이어 마스크")]
        [SerializeField] private LayerMask targetLayerMask = ~0;

        [Header("Visual Colors")]
        [Tooltip("예약 순서 인디케이터 기본 색상")]
        [SerializeField] private Color indicatorColor = new Color(0.2f, 0.8f, 1f, 1f);

        [Tooltip("궤적 연결선 색상")]
        [SerializeField] private Color trajectoryColor = new Color(0.4f, 0.9f, 1f, 0.8f);

        [Tooltip("마나 부족 경고 색상")]
        [SerializeField] private Color insufficientManaColor = new Color(1f, 0.2f, 0.2f, 1f);

        public int MaxQueueCount => maxQueueCount;
        public float ExecutionDelay => executionDelay;
        public float MaxTargetDistance => maxTargetDistance;
        public LayerMask TargetLayerMask => targetLayerMask;
        public Color IndicatorColor => indicatorColor;
        public Color TrajectoryColor => trajectoryColor;
        public Color InsufficientManaColor => insufficientManaColor;
    }
}
