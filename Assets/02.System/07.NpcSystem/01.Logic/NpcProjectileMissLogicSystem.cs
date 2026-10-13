namespace NpcSystem
{
    /// <summary>
    /// 아군 투사체가 "충돌하지 못하고 범위를 벗어났는지"만 판단하는 호출형 순수 로직입니다(15-B, [L]).
    /// Transform/Rigidbody를 참조하지 않고 NpcProjectile(비주얼)이 넘겨주는 값(경과 시간, 발사 지점에서의 거리)만 사용합니다.
    /// 기준(최대 거리/최대 수명)은 PureDataNpcProjectile이 소유합니다.
    /// </summary>
    public class NpcProjectileMissLogicSystem
    {
        private readonly PureDataNpcProjectile pureData;
        private float elapsedSeconds;

        public NpcProjectileMissLogicSystem(PureDataNpcProjectile pureData)
        {
            this.pureData = pureData;
        }

        /// <summary>새 발사를 시작한다(경과 시간 초기화).</summary>
        public void Begin()
        {
            elapsedSeconds = 0f;
        }

        /// <summary>시간을 진행시키고, 최대 수명 또는 발사 지점으로부터의 최대 거리를 넘었는지(빗나감) 돌려준다.</summary>
        public bool Tick(float deltaSeconds, float distanceFromLaunch)
        {
            elapsedSeconds += deltaSeconds;

            return elapsedSeconds >= pureData.MaxLifetimeSeconds
                   || distanceFromLaunch >= pureData.MaxDistanceFromLaunch;
        }
    }
}
