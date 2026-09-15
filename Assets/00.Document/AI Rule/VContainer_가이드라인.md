# VContainer 작업 가이드라인 v0.1
> 기반: DLV 아키텍처 v0.2  
> 목적: 코드베이스 결합도 제거 및 의존성 주입 표준화

---

## 1. 교체 대상 핵심 원칙

VContainer 사용 목적은 **코드베이스의 잘못된 결합 제거**입니다.  
아래 4가지 패턴은 코드베이스에서 확인 즉시 VContainer 주입으로 교체합니다.

| 구분 | 교체 대상 (Before) | 교체 방식 (After) | 교체 이유 |
|------|--------------------|-------------------|-----------|
| 씬 검색 | `GameObject.Find`, `FindObjectOfType` | `RegisterComponent` 후 주입 | 씬 로딩 순서 의존성 및 검색 지연 제거 |
| 전역 접근 | `Manager.Instance` 정적 싱글톤 | 컨테이너 등록 후 생성자 주입 | 숨겨진 의존성 제거, 테스트 용이성 확보 |
| 객체 생성 | `new LogicSystem()` 직접 생성 | 컨테이너 자동 생성 등록 | 생명주기 관리 중앙화 |
| 컴포넌트 탐색 | `GetComponent<IVisualizer>()` | 인터페이스 단위 주입 | 하이라키 구조 변경 시 오류 방지 |

---

## 2. 코드 교체 예시 (Before vs After)

### 2-1. 씬 오브젝트 검색 제거

* **Before:**
```csharp
void Awake()
{
    // 씬에서 직접 검색
    playerVisualizer = GameObject.Find("Player").GetComponent<IPlayerVisualizer>();
}
```

* **After (Scope 등록):**
```csharp
// LifetimeScope 설정
[SerializeField] private PlayerVisualizer playerVisualizer;

protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterComponent(playerVisualizer).As<IPlayerVisualizer>();
}
```

* **After (소비 클래스):**
```csharp
// LogicSystem
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

### 2-2. 정적 싱글톤 제거

* **Before:**
```csharp
void Start()
{
    // 전역 싱글톤 직접 호출
    GameManager.Instance.AddScore(10);
}
```

* **After:**
```csharp
// Scope 등록
builder.Register<ScoreManager>(Lifetime.Scoped);

// 사용 클래스
public class BattleSystem
{
    private readonly ScoreManager _scoreManager;

    public BattleSystem(ScoreManager scoreManager)
    {
        _scoreManager = scoreManager;
    }
}
```

---

### 2-3. 로직 클래스 직접 생성 제거

* **Before:**
```csharp
public class PlayerController : MonoBehaviour
{
    private ActionLogicSystem _logic;

    void Awake()
    {
        // 직접 객체 생성
        _logic = new ActionLogicSystem();
    }
}
```

* **After:**
```csharp
// Scope 등록: 인터페이스와 함께 등록하여 자동 실행
builder.Register<ActionLogicSystem>(Lifetime.Scoped)
       .As<IInitializable>()
       .As<ITickable>();
```

---

## 3. DLV 아키텍처 연계 규칙

### 3-1. [D] 데이터 그룹 주입
* **PureData (ScriptableObject):**
  - LifetimeScope 인스펙터에 등록 후 `RegisterInstance`로 바인딩합니다.
* **RuntimeData:**
  - `Lifetime.Scoped`로 등록하여 씬 단위로 관리합니다.

```csharp
[SerializeField] private PlayerPureData playerPureData;

builder.RegisterInstance(playerPureData);
builder.Register<PlayerRuntimeData>(Lifetime.Scoped);
```

### 3-2. [L] 로직 그룹 주입
* MonoBehaviour를 상속받지 않는 순수 C# 클래스로 작성합니다.
* VContainer의 `IInitializable`, `ITickable` 인터페이스를 구현합니다.
* 반드시 **생성자 주입**을 사용합니다.

### 3-3. [V] 비주얼 그룹 주입
* 구체 클래스가 아닌 **인터페이스 단위(`As<IInterface>`)**로 등록합니다.
* 씬에 존재하는 컴포넌트는 `builder.RegisterComponent()`를 사용합니다.

---

## 4. 주입 대상 경계 기준

모든 연결을 VContainer로 바꾸지 않습니다.

* **VContainer 주입 대상:**
  - 시스템 간 통신
  - DLV 계층 간 인터페이스 연결
  - 데이터 및 매니저 객체
  - 씬 진입점

* **인스펙터 직렬화 유지 대상:**
  - 동일 프리팹 내부 UI 요소 (버튼, 텍스트)
  - 단순 리소스 컴포넌트 (애니메이터, 사운드, 이펙트)
  - 프리팹 자체 연결
