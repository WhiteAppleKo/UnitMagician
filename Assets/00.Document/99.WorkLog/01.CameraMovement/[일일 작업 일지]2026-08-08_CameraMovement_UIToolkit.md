# [일일 작업 일지]2026-08-08 (카메라 LookAt 시선 조준 및 UI Toolkit 모드 선택 시스템)

---

## 1. 작업 개요

* **작업 대상**: 카메라 LookAt 시선 조준 보완, UI Toolkit 기반 마우스 선택 모드 UI 구축 및 키보드 단축키 제거
* **관련 시스템**: `CameraMovement` (Visualizer & UI Component)
* **아키텍처**: DLV (Data-Logic-Visual) 아키텍처 및 UI Toolkit 표준 규격 준수

---

## 2. 시간의 흐름별 문제·변경·성과 (Before - After - Why)

### 2-1. [1단계] 카메라 LookAt 시선 조준 구문 추가
* **Before**: `virtualCamera.Follow = pivotTarget`만 지정되어 있어 위치 이동은 작동하나 카메라 회전 시선이 타깃을 쳐다보지 않음
* **After**: [CameraFollowVisualizer.cs](file:///c:/Users/kot77/Desktop/Unity/UnitMagician/Assets/02.System/00.Movement/02.Visualizer/01.CameraMovement/CameraFollowVisualizer.cs) 내 `virtualCamera.LookAt = pivotTarget;` 추가
* **Why**: 가상 카메라 오프셋 위치 이동과 더불어 타깃 응시 회전(Rotation) 동시 연동 보장

### 2-2. [2단계] 키보드 단축키 제거 및 UI Toolkit 마우스 선택 시스템 구축
* **Before**: 키보드 숫자키 `1`, `2`, `3` 단축키 방식 모드 변경
* **After**: 
  * `PlayerInputSystem.cs` 내 키보드 카메라 모드 코드 전면 삭제
  * `CameraModeUIComponent.cs` 신규 생성 및 `MovementLifetimeScope.cs` VContainer 등록
* **Why**: 마우스 조작 기반 직관적 UI 선택 모드 전환 체계 마련

### 2-3. [3단계] 화면 좌측 중단 (Left-Center) UI 위치 조정
* **Before**: 화면 좌측 상단 수평 패널 구도
* **After**: 화면 좌측 중단 (`top: 50%`, `translateY: -50%`) 수직 (Column) 패널 구조 레이아웃 수정
* **Why**: 화면 구도 시각적 안정감 확보 및 접근성 향상

### 2-4. [4단계] UXML 에셋 정석 전환 및 최상위 Root Ignore 피킹 차단 해결
* **Before**: 런타임 C# 동적 패널 생성 시 최상위 Root 레이아웃 피킹 차단 및 마우스 뷰포트 영역 포착 불능으로 버튼 클릭 미작동 발생
* **After**: 
  * 정석 [CameraModeUI.uxml](file:///c:/Users/kot77/Desktop/Unity/UnitMagician/Assets/02.System/00.Movement/02.Visualizer/01.CameraMovement/UI/CameraModeUI.uxml) 에셋 신규 생성
  * 최상위 `Root` 패널 `picking-mode="Ignore"` 속성 지정 (투영 영역 클릭 통과)
  * C# 동적 생성 코드 전면 삭제, `UnitUnlockTestUIComponent.cs` 기존 프로젝트 정석 패턴 100% 동일 정돈
* **Why**: UI Toolkit 표준 에셋 관리 준수 및 마우스 버튼 클릭 반응 원천 보장

---

## 3. 상세 사용 가이드 및 씬 바인딩

### 3-1. 정석 UI Document 씬 배치 절차
1. Hierarchy 창 우클릭 -> `UI Toolkit` -> `UI Document` 선택 생성 (오브젝트 이름: `UI_CameraMode`)
2. `UI_CameraMode` 오브젝트 선택 -> `Add Component` -> [CameraModeUIComponent.cs](file:///c:/Users/kot77/Desktop/Unity/UnitMagician/Assets/02.System/00.Movement/02.Visualizer/01.CameraMovement/UI/CameraModeUIComponent.cs) 부착
3. `UI Document` 컴포넌트 **`Source Asset`** 필드: [CameraModeUI.uxml](file:///c:/Users/kot77/Desktop/Unity/UnitMagician/Assets/02.System/00.Movement/02.Visualizer/01.CameraMovement/UI/CameraModeUI.uxml) 에셋 드래그 연결
4. `MovementLifetimeScope` 선택 -> `Camera Mode UI` 필드: `UI_CameraMode` 오브젝트 드래그 연결

### 3-2. 카메라 모드 조작 검수 가이드
* `Hybrid Focus`: 마우스 클릭 시 하이브리드 보정 모드 전환 (평상시 플레이어 100% + 줌인 진입 시 마우스 시선 보정)
* `Player Only`: 마우스 클릭 시 플레이어 전용 모드 전환 (오직 플레이어 100% 고정 추적)
* `Mouse Focus`: 마우스 클릭 시 마우스 추적 모드 전환 (마우스 3D 위치 주 추적 + Viewport 화면 이탈 방지 한계 클램프)
