using System;
using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트가 가리킬 수 있는 월드 대상(예: 파괴 가능한 돌문)의 최소 규약입니다. 대상 쪽 비주얼 컴포넌트(DestructibleDoorComponent 등)가 구현하고,
    /// 이벤트 시스템(완료 평가기/동작 핸들러)은 이 인터페이스로만 대상을 다룹니다(구체 컴포넌트 직접 참조 금지).
    /// 대상 목록은 EventSystemLifetimeScope 인스펙터에 직접 등록합니다(Find* 금지).
    /// </summary>
    public interface IEventTarget
    {
        /// <summary>이벤트 SO(TargetDestroyedCompletion/NpcRepeatAttackAction 등)가 이 대상을 가리킬 때 쓰는 id.</summary>
        string TargetId { get; }

        /// <summary>공격 대상 등으로 쓰이는 대상 GameObject.</summary>
        GameObject TargetObject { get; }

        /// <summary>대상이 이미 파괴되었는지 여부(한 번 true가 되면 되돌아가지 않음).</summary>
        bool IsDestroyed { get; }

        /// <summary>대상이 파괴된 시점에 한 번 발행됩니다(폴링 없이 완료 판정을 하기 위한 통지).</summary>
        event Action OnDestroyed;
    }
}
