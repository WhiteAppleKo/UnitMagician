using UnityEngine;
using UnitSystem;

namespace PickupSystem
{
    /// <summary>
    /// 줍기 가능한 바닥 아이템의 추상화입니다. 아이템 종류가 늘어나도 PickupLogicSystem은 변경 없이
    /// GrantedUnit만 읽어 습득을 처리합니다.
    /// </summary>
    public interface IPickupTarget
    {
        /// <summary>이 아이템을 주우면 습득하는 마법.</summary>
        PureDataUnit GrantedUnit { get; }

        /// <summary>월드 위치(근접 판정 거리 계산용).</summary>
        Vector3 Position { get; }

        /// <summary>주운 뒤 아이템을 소비(비활성화/제거)합니다.</summary>
        void Consume();
    }
}
