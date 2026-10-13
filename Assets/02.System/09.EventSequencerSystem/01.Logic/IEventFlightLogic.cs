using System;
using UnityEngine;

namespace EventSequencerSystem
{
    /// <summary>
    /// TutorialSequencerLogicSystem이 3단계 비행 구간 계산 로직(EventFlightLogicSystem)을 구체 클래스 없이
    /// 사용하기 위한 인터페이스입니다(VContainer_가이드라인 - 구체 클래스 직접 주입 금지).
    /// </summary>
    public interface IEventFlightLogic
    {
        bool IsActive { get; }
        EventFlightPhase Phase { get; }
        float Progress { get; }

        /// <summary>구간(접근/이탈) 또는 진행도(0~1)가 바뀔 때마다 발행됩니다.</summary>
        event Action<EventFlightPhase, float> OnFlightChanged;

        /// <summary>접근 -> 이탈로 바뀌는 전환점에서 사이클당 정확히 한 번 발행됩니다.</summary>
        event Action OnTurnaround;

        /// <summary>
        /// 이벤트 투사체를 발사한 뒤 아직 판정이 나지 않은 상태에서 그 투사체가 소실(파괴/비활성화)되면 사이클당 한 번 발행됩니다
        /// (소프트락 안전망). 발사 전, 또는 End()로 비행이 끝난 뒤에는 발행되지 않습니다.
        /// </summary>
        event Action OnProjectileLost;

        /// <summary>
        /// 주어진 가해 오브젝트(<c>DamageContext.Source</c>)가 이번 이벤트의 이벤트 투사체인지 판정합니다
        /// (식별은 비주얼 구현체가 수행). 비행이 활성 상태가 아니거나 투사체를 받기 전이면 false입니다.
        /// </summary>
        bool IsEventProjectile(GameObject source);

        /// <summary>이벤트 진입 시점에 P/B를 고정하고 발사 지점 L을 정해 접근 구간 추적을 시작합니다(사이클 상태/래치 초기화).</summary>
        void Begin();

        /// <summary>
        /// 이벤트 시작 신호 방송이 끝난 직후 호출합니다. 이벤트 투사체를 받을 수 있는 "발사 창"을 닫습니다
        /// (그 방송 안에서 아군이 쏜 투사체만 이벤트 투사체로 인정).
        /// </summary>
        void CloseLaunchWindow();

        /// <summary>비행 추적을 끝냅니다(이벤트 종료). 이벤트는 발행하지 않습니다.</summary>
        void End();
    }
}
