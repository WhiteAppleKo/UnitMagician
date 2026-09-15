# 2026-08-12 UI Toolkit 설계 및 상호작용 가이드라인

## 1. 시각 영역 - 포인터 감지 영역 일치

> **원칙**: 실제 시각 형태 점유 영역만 포인터 감지 영역 설정.

### 상세 규칙
투명 패널, 빈 컨테이너, 불필요 여백 등 비노출 영역 클릭 방해 차단.

### 권장 설정
입력 불필요 투명 패널 / 레이아웃 요소: `Picking Mode` -> `Ignore` 설정 (`pickingMode = PickingMode.Ignore`).

---

## 2. UI - 인게임 상호작용 분리 (Pure UI Toolkit API)

> **원칙**: 마우스 클릭 입력 UI 클릭 또는 인게임 월드 상호작용 중 단일 역할 수행.

### 상세 규칙
UI 클릭 시 백그라운드 레이캐스트 중복 발생 차단 (uGUI EventSystem 구문 미사용, UI Toolkit 순수 API 사용).

### 구현 표준 (Unity 6 / UI Toolkit Pure API)
1. `UIDocument` 가리키는 패널(`panel`) 실시간 획득.
2. `RuntimePanelUtils.ScreenToPanel(panel, mouseScreenPos)` 좌표 변환.
3. `panel.Pick(panelPos)` 통해 마우스 위치 상호작용 UI 엘리먼트 감지.
4. `picked != null && picked != rootVisualElement` 만족 시 인게임 3D 레이캐스트 즉시 중단.

## 3. UI 유형 분류 및 레이아웃 관리

> **원칙**: 고정형 UI, 가변형 UI 2가지 유형 분리 설계.

### 유형별 정의 및 규칙

#### 고정형 UI
* **정의**: HUD, 미니맵, 주요 메뉴 등 위치 상시 고정 UI.
* **규칙**: Flexbox 및 USS 배치 속성 (`position: relative` 또는 `position: absolute` 및 `top`/`left`/`right`/`bottom`) 활용 위치 고정. 런타임 위치 변경 금지.

#### 가변형 UI
* **정의**: 인벤토리, 정보 창, 팝업 등 드래그 이동 가변 UI.
* **규칙**:
  1. `position: absolute` 상태 드래그 포인터 이벤트 (`PointerDownEvent`, `PointerMoveEvent`, `PointerUpEvent`) 수신. USS `translate` 속성 업데이트 위치 이동 구현.
  2. 드래그 시작 시 해당 요소 부모 `BringToFront()` 메서드 사용 계층 구조 최하단 이동 (렌더링 및 입력 수신 우선순위 최상단 반영).
  3. UI 창 패널 영역 밖 이탈 방지 좌표 제한 처리 필수.