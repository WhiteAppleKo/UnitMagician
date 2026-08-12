# Project Environment & Tool Specification

---

## 1. 개요 및 기본 정보

* **프로젝트명**: UnitMagician
* **장르**: 3D 쿼터뷰 탑다운 슈팅 액션
* **타깃 플랫폼**: PC (Windows)
* **저장 위치**: `Assets/00.Documents/Project_Environment.md`

---

## 2. 핵심 개발 환경 (Core Environment)

* **Unity 엔진 버전**: Unity 6 (Version `6000.3.10f1`)
* **렌더링 파이프라인**: Universal Render Pipeline (URP)
* **스크립트 언어**: C# (.NET Standard 2.1 / C# 9.0+)

---

## 3. 주요 패키지 및 에셋 명세 (Packages & Assets)

### 3-1. 코어 아키텍처 및 시스템 패키지
* **의존성 주입 (DI)**: `VContainer` (의존성 주입, 서비스 라이프타임 바인딩 제어)
* **비동기 연산 파이프라인**: `UniTask` (`Cysharp.Threading.Tasks`, 비동기 순차 연산 파이프라인 제어)
* **카메라 시스템**: `Cinemachine` (3D 쿼터뷰 추적, Lens FOV / Camera Distance 동적 줌 제어)
* **입력 시스템**: `Unity Input System` (New Input System, 마우스/키보드 입력바인딩)
* **렌더링 파이프라인**: `Universal RP` (URP 렌더러, 화면 그레이스케일 Post-Processing, 아웃라인 셰이더)

### 3-2. 유틸리티 및 렌더링 에셋
* **셰이더 제작**: `Shader Graph` (단위 변환 액체 셰이더 및 아웃라인 셰이더 제작)
* **UI & 애니메이션 연출**: `DOTween` / `UI Toolkit` (퀵슬롯 UI, 자원 회복 Floating Text, 타깃 팝업 UI)

---

## 4. 개발 도구 및 개발 환경 (Tools & IDE)

* **주요 IDE**: Visual Studio / JetBrains Rider
* **버전 관리 (VCS)**: Git / GitHub
* **AI 개발 보조**: Google Antigravity AI Coding Assistant

---

## 5. 패키지 및 에셋 관리 가이드 (업데이트 이력)

> 본 문서는 프로젝트 진행 중 신규 에셋 및 패키지 추가 시 지속적으로 업데이트 관리됩니다.

### 5-1. 패키지 추가 이력
* **2026-08-03**: 프로젝트 초기 환경 구축 (`Unity 6000.3.10f1`, `URP`, `VContainer`, `Cinemachine`, `Input System`)
* **2026-08-08**: `UI Toolkit` 패키지 및 가이드라인 반영 (마우스 선택 모드 UI 및 UI 상호작용 시스템 구축)
* **2026-08-12**: `UniTask` 비동기 연산 패키지 명세 추가 및 범용 파이프라인 구조 구축 반영

