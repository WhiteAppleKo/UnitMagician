# [일일 작업 일지]2026-08-03 (카메라 추적 및 동적 줌 시스템)

---

## 1. 작업 개요

* **작업 대상**: 3D 쿼터뷰 카메라 추적, 동적 줌 및 3가지 카메라 모드 시스템 구축
* **관련 기획 문서**: `Assets/00.Documents/01.DEV/01.CameraFollowSystem_Plan.md`
* **아키텍처**: DLV (Data-Logic-Visual) 아키텍처 기반 설계

---

## 2. 시간의 흐름별 문제·변경·성과 (Before - After - Why)

### 2-1. [1단계] DLV 아키텍처 기반 모듈 분리 설계
* **Before**: 기존 구식 카메라 스크립트 파편화 상태
* **After**: 
  * Data: `CameraSettingSO` (Pure Data)
  * Logic: `ICameraFollowService`, `CameraFollowService`, `MouseWorldPositionProvider`
  * Visual: `CameraFollowVisualizer`
  * DI: `MovementLifetimeScope` 등록
* **Why**: 입력 및 데이터 보존, 판단 로직 분리, 비주얼 연동 결합도 최소화 준수

### 2-2. [2단계] Cinemachine 3.x 독립 오브젝트 구조 확립
* **Before**: Visualizer 컴포넌트 부착 위치 (독립 오브젝트 vs Cinemachine Camera) 혼선
* **After**: 독립 GameObject 기반 `CameraFollowVisualizer` 구조 및 명시적 Virtual Camera 지정 원복
* **Why**: 단일 카메라 종속 제거 및 다중 카메라 런타임 전환 유연성 확보

### 2-3. [3단계] 3D Perspective 투영 모드 전환 및 C# Struct 렌즈 갱신 버그 해결
* **Before**: 구식 2D Orthographic 모드 오적용 및 `virtualCamera.Lens.OrthographicSize` 수정 시 수치 미반영 현상 발생
* **After**: 
  * 3D Perspective 투영 모드 전환
  * `LensSettings lens = virtualCamera.Lens;` 구조체 변수 추출 후 수치 변경, `virtualCamera.Lens = lens;` 구조체 전체 재할당 적용
* **Why**: 3D 쿼터뷰 원근감 카메라 구도 형성 및 C# Struct (Value Type) 복사본 수정 버그 원천 차단

### 2-4. [4단계] 마우스 뷰포트 영역 Clamping 및 ILateTickable 렌더링 튕김(Bouncing) 차단
* **Before**: 마우스 원거리 이동 시 카메라 피벗 시야 밖 튐 현상 및 이동 보간 도중 억지 복귀 튕김 현상 발생
* **After**: 
  * `MouseWorldPositionProvider` 마우스 Screen 좌표 `Screen.width`, `Screen.height` 내 Clamping 적용
  * `CameraFollowService` 연산 시점 `ITickable` -> `ILateTickable` (`LateUpdate`) 파이프라인 전환
  * SmoothDamp 보간 전 목표 피벗 좌표 선제적 한계 제한 (Pre-Clamp) 적용
* **Why**: 카메라 렌더링 직전 파이프라인 단계 연산 적용 및 목표 지점 선제 한정 처리 시 시야 이탈 후 튕김 현상 100% 소멸

### 2-5. [5단계] 기획서 Section 5 정정 명세 반영, 3가지 카메라 모드 및 하드코딩 제거
* **Before**: 2D 구식 중복 수치 잔재 및 단일 카메라 모드 방치
* **After**: 
  * `CameraMode` enum (PlayerOnly, MouseFocus, HybridFocus) 3가지 모드 연산 구현
  * `PlayerInputSystem` 키보드 숫자키 `1`, `2`, `3` 카메라 모드 실시간 전환 연동
  * Pure Data (`CameraSettingSO`) 내 구식 수치 제거 및 3D FOV (30°~60°), 거리 (8m~18m) 수치 100% 바인딩
  * `CameraSettingSO` 전체 필드 쉬운 워딩 `[Tooltip]` 주석 등록
* **Why**: 기획 문서 최신 정정 스펙 100% 준수 및 Pure Data 아키텍처 규칙 이행

---

## 3. 상세 사용 가이드

### 3-1. 카메라 모드 단축키 안내
* 숫자키 `1`: `HybridFocus` (하이브리드 보정 모드 - 평상시 플레이어 100% + 줌인 진입 시 마우스 3D 지점 시선 보정, 디폴트)
* 숫자키 `2`: `PlayerOnly` (플레이어 전용 모드 - 마우스 조작 무시, 플레이어 100% 고정 추적)
* 숫자키 `3`: `MouseFocus` (마우스 추적 모드 - 마우스 3D 위치 주 추적 + 플레이어 Viewport 화면 이탈 방지 한계 클램프)

### 3-2. 씬 구성 및 바인딩 가이드
1. Main Camera: `CinemachineBrain` 컴포넌트 포함 확인
2. Cinemachine Camera: `CinemachineFollow` 컴포넌트 포함 확인
3. CameraFollowVisualizer:
   - `Virtual Camera` 항목: Cinemachine Camera 할당
   - `Player Transform` 항목: 플레이어 캐릭터 Transform 할당
4. MovementLifetimeScope: `CameraSettingSO` 에셋 및 `CameraFollowVisualizer` 참조 드래그 등록
