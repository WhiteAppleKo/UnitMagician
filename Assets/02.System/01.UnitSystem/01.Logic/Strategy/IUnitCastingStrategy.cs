using System;

namespace UnitSystem
{
    /// <summary>
    /// 카메라 시점별(탑뷰, 숄더뷰 등) 단위 마법 조작 및 시전 방식을 정의하는 전략 인터페이스입니다.
    /// </summary>
    public interface IUnitCastingStrategy : IDisposable
    {
        /// <summary>
        /// 해당 시점 모드로 전환되어 전략이 활성화될 때 호출됩니다. (UI 활성화, 상태 초기화)
        /// </summary>
        void Enter();

        /// <summary>
        /// 매 프레임 실행되어 마우스 조준/호버 및 시전 입력을 처리합니다.
        /// </summary>
        void Update();

        /// <summary>
        /// 다른 시점 모드로 전환되어 전략이 비활성화될 때 호출됩니다. (UI 숨김, 타겟/락온 해제)
        /// </summary>
        void Exit();
    }
}
