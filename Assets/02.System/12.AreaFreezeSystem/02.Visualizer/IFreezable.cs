namespace AreaFreezeSystem
{
    /// <summary>
    /// 광역 빙결 대상 계약([V] Visual Interface)입니다. 보스(BossFreezableComponent)와 지형
    /// (FreezableTerrainComponent)이 각각 구현하며, AreaFreezeVisualizer가 Physics.OverlapSphere로 수집한
    /// 각 대상에 순차적으로 Freeze()를 호출합니다(05번 기획서 5.8절 데이터 가공/흐름 서술).
    /// </summary>
    public interface IFreezable
    {
        /// <summary>이 대상이 빙결 순서에 도달했을 때 호출됩니다. 실제 얼음 텍스처/이펙트 재생을 책임집니다.</summary>
        void Freeze();
    }
}
