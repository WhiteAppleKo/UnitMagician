namespace UnitSystem
{
    /// <summary>
    /// 락온 대상 위에 "마법이 적용되면 대상이 어떻게 바뀌는지"를 적용 전에 보여주는 비주얼 추상 인터페이스입니다(13-C).
    /// 구현체는 대상별로 미리보기 오브젝트를 1개씩 관리하고, 대상이 움직이면 따라다니며,
    /// 대상이 파괴/비활성화되면 스스로 정리해야 합니다.
    /// </summary>
    public interface ILockOnPreviewVisualizer
    {
        /// <summary>대상의 미리보기를 생성하거나(없으면) 새 예측값으로 갱신한다(대상당 1개).</summary>
        void ShowPreview(LockOnTargetPreview preview);

        /// <summary>대상의 미리보기를 즉시 제거한다. 없으면 무시.</summary>
        void HidePreview(RuntimeDataUnitGroup target);

        /// <summary>모든 미리보기를 즉시 제거한다.</summary>
        void HideAll();
    }
}
