using VContainer;
using VContainer.Unity;

namespace CodeLibrary.Scripts.VContainer
{
    public class GameLifetimeScope_KWT : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // GameManager 등록
            builder.Register<GameManager>(Lifetime.Singleton);
            
            // 씬 하이어라키에 있는 GameController를 찾아서 등록 및 주입
            builder.RegisterComponentInHierarchy<GameController>();

            // IStartable, ITickable 인터페이스를 구현한 Tester 클래스 등록
            builder.RegisterEntryPoint<Tester>();
        }
    }
}
