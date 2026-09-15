# VContainer 작업 가이드라인 v0.2
> 기반: DLV 아키텍처 v0.3  
> 목적: 코드베이스 결합도 제거 및 인터페이스 기반 의존성 주입 표준화

---

## 1. 교체 대상 핵심 원칙

VContainer 사용 목적은 **코드베이스의 잘못된 결합 제거**입니다.  
아래 4가지 안티 패턴은 코드베이스에서 확인 즉시 VContainer 주입으로 교체합니다.

| 구분 | 교체 대상 (Before) | 교체 방식 (After) | 교체 이유 |
|------|--------------------|-------------------|-----------|
| 씬 검색 | `GameObject.Find`, `FindObjectOfType` | `RegisterComponent` 후 인터페이스 주입 | 씬 로딩 순서 의존성 및 프레임 지연 제거 |
| 전역 접근 | `Manager.Instance` 정적 싱글톤 | 인터페이스 바인딩 후 생성자 주입 | 숨겨진 결합 제거, 테스트 독립성 확보 |
| 객체 생성 | `new LogicSystem()` 직접 생성 | 컨테이너 자동 생성 등록 | 생명주기 관리 중앙화 |
| 컴포넌트 탐색 | `GetComponent<IVisualizer>()` | 인터페이스 단위 주입 | 하이라키 구조 변경 시 런타임 에러 방지 |

> [!IMPORTANT]
> **인터페이스 주입 원칙:**  
> 구체 클래스를 직접 `[Inject]` 주입받는 것을 엄격히 금지합니다. 모든 주입은 인터페이스(`As<IInterface>`)를 거쳐야 합니다.

---

## 2. 코드 교체 예시 (Before vs After)

### 2-1. 씬 오브젝트 검색 제거

* **Before:**
```csharp
void Awake()
{
    // 씬에서 이름을 통해 직접 검색
    playerVisualizer = GameObject.Find("Player").GetComponent<IPlayerVisualizer>();
}
```

* **After (Scope 등록):**
```csharp
// LifetimeScope 설정
[SerializeField] private PlayerVisualizer playerVisualizer;

protected override void Configure(IContainerBuilder builder)
{
    // 구체 컴포넌트를 인터페이스로 등록
    builder.RegisterComponent(playerVisualizer).As<IPlayerVisualizer>();
}
```

* **After (로직 주입):**
```csharp
public class PlayerLogicSystem
{
    private readonly IPlayerVisualizer _visualizer;

    [Inject]
    public PlayerLogicSystem(IPlayerVisualizer visualizer)
    {
        _visualizer = visualizer;
    }
}
```

---

### 2-2. 정적 싱글톤 제거 (인터페이스 바인딩)

* **Before:**
```csharp
void Start()
{
    // 전역 싱글톤 직접 접근
    ScoreManager.Instance.AddScore(10);
}
```

* **After:**
```csharp
// 인터페이스 정의
public interface IScoreHandler
{
    void AddScore(int amount);
}

// Scope 등록
builder.Register<ScoreSystem>(Lifetime.Scoped).As<IScoreHandler>();

// 사용 클래스
public class BattleSystem
{
    private readonly IScoreHandler _scoreHandler;

    public BattleSystem(IScoreHandler scoreHandler)
    {
        _scoreHandler = scoreHandler;
    }
}
```

---

### 2-3. 로직 클래스 생성 및 생명주기 분리

#### A. 유니티 생명주기 연동 로직
유니티 생명주기(Start, Update, FixedUpdate, LateUpdate, OnDestroy)에 맞춰 실행되는 시스템은 VContainer 생명주기 인터페이스를 구현합니다.

| 유니티 생명주기 | VContainer 인터페이스 | 역할 |
|---|---|---|
| `Start` | `IInitializable` | 초기화 |
| `Update` | `ITickable` | 매 프레임 판단 |
| `FixedUpdate` | `IFixedTickable` | 물리 주기 연산 |
| `LateUpdate` | `ILateTickable` | 프레임 후처리 연산 |
| `OnDestroy` | `IDisposable` | 메모리 및 이벤트 해제 |

```csharp
// 클래스 구현 예시: 유니티 생명주기 인터페이스 구현
public class BattleActionSystem : IInitializable, ITickable, IFixedTickable, IDisposable
{
    public void Initialize() { /* Start 초기화 */ }
    public void Tick() { /* Update 매 프레임 판단 */ }
    public void FixedTick() { /* FixedUpdate 물리 연산 */ }
    public void Dispose() { /* OnDestroy 자원 정리 */ }
}

// Scope 등록: 유니티 생명주기 인터페이스로 등록하여 자동 실행
builder.Register<BattleActionSystem>(Lifetime.Scoped)
       .As<IInitializable>()
       .As<ITickable>()
       .As<IFixedTickable>()
       .As<IDisposable>();
```

#### B. 호출형 순수 로직 (On-Demand Pure Logic)
상시 갱신 없이 필요할 때만 호출되는 로직은 순수 인터페이스로 바인딩합니다.

```csharp
// 인터페이스 및 구현
public interface IDamageCalculator
{
    int Calculate(PureAttackData attackData, int targetDefense);
}

public class DamageCalculateSystem : IDamageCalculator
{
    public int Calculate(PureAttackData attackData, int targetDefense)
    {
        return Mathf.Max(1, attackData.Damage - targetDefense);
    }
}

// Scope 등록
builder.Register<DamageCalculateSystem>(Lifetime.Scoped)
       .As<IDamageCalculator>();
```

---

## 3. DLV 아키텍처 연계 주입 규칙

### 3-1. [D] 데이터 그룹 주입
* **PureData (ScriptableObject):**
  - LifetimeScope 인스펙터에 연결 후 `RegisterInstance`로 바인딩합니다.
* **PureDataBase (ScriptableObject):**
  - 전체 게임 Root Scope에 `RegisterInstance`로 단일 등록합니다.
* **RuntimeData:**
  - `Lifetime.Scoped`로 등록하여 씬 단위로 관리합니다.
  - 외부 수정 차단을 위해 읽기 전용 인터페이스(`IReadOnlyPlayerData`)로 분리 바인딩을 권장합니다.

```csharp
[SerializeField] private PlayerPureData playerPureData;

builder.RegisterInstance(playerPureData);
builder.Register<PlayerRuntimeData>(Lifetime.Scoped);
```

### 3-2. [L] 로직 그룹 주입
* 순수 C# 클래스로 작성하며 MonoBehaviour를 상속받지 않습니다.
* 모든 의존성은 **생성자 주입**을 통해 전달받습니다.
* 데이터 수정 시 직접 값을 대입하지 않고 Runtime Data의 전용 메서드를 호출합니다.

```csharp
public class CombatLogicSystem
{
    private readonly PlayerRuntimeData _runtimeData;
    private readonly IPlayerVisualizer _visualizer;

    public CombatLogicSystem(PlayerRuntimeData runtimeData, IPlayerVisualizer visualizer)
    {
        _runtimeData = runtimeData;
        _visualizer = visualizer;
    }

    public void OnHit(int damage)
    {
        // 데이터 전용 메서드 호출 (판단 및 지시)
        _runtimeData.DecreaseHp(damage);
        // 비주얼 연출 지시
        _visualizer.PlayHitEffect();
    }
}
```

### 3-3. [V] 비주얼 그룹 주입
* 구체 클래스 등록 금지, 반드시 **인터페이스 단위(`As<IInterface>`)**로 등록합니다.
* 씬에 존재하는 컴포넌트는 `builder.RegisterComponent()`를 사용합니다.

---

## 4. 주입 대상 경계 기준

모든 연결을 VContainer로 처리하지 않습니다.

* **VContainer 주입 대상:**
  * 시스템 간 통신 및 인터페이스 연결
  * DLV 계층 간 의존성 전달
  * RuntimeData 및 상태 객체 전달
  * 씬 진입점 시스템

* **인스펙터 직렬화 유지 대상:**
  * 동일 프리팹 내부 UI 요소 (버튼, 텍스트)
  * 컴포넌트 내부 리소스 (애니메이터, 파티클, 오디오 소스)
  * 프리팹 자체 내부 계층 참조
