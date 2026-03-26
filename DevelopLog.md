# Development Log (개발 일지)

이 문서는 프로젝트의 개발 타임라인, 주요 변경 사항, 마주친 버그 및 해결 방법(Troubleshooting)을 기록하는 공간입니다. 

## [작성 규칙]
1. 나(AI)는 특정 Task(예: Phase 1)가 완료되거나 중요한 버그를 수정했을 때, 이 문서의 **최상단(가장 위)**에 새로운 로그를 추가합니다.
2. 각 로그는 **날짜(YYYY-MM-DD), 제목, 내용, 해결된 이슈** 포맷을 포함해야 합니다.

---

### 📅 [2026-03-25] Phase 1.3: Player Ball 물리 세팅 및 프리팹 구축
* **작업 내용**: 
  * `PlayerBall` 게임 오브젝트 및 프리팹(`Assets/Prefabs/PlayerBall.prefab`) 생성 완료.
  * `Rigidbody2D` 최적화: 고속 터널링 방지를 위한 `Continuous` 설정 및 렌더링 보간(`Interpolate`) 세팅. 빠릿한 낙하감을 위해 `gravityScale` 1.5로 상향 조정.
  * `CircleCollider2D` 반지름 0.4 크기로 부착 완료.
* **비고**: `.cursorrules` 원칙에 따라, 튕기는 반발력 등은 코드로 강제하지 않았습니다. 에디터에서 `Physics Material 2D`를 생성해 PlayerBall 콜라이더에 할당하세요.
* **다음 목표**: `Task.md`의 Phase 1.4 (Mathf.Atan2 기반 터치 조작 컨트롤러 구현).

---

### 📅 [2026-03-25] Phase 1.2: 해상도 대응 카메라 및 Tilemap 뼈대 구축
* **작업 내용**: 
  * `CameraController.cs` 작성 (기기 화면비에 따른 Orthographic Size 동적 스케일링 보정).
  * Main Camera에 해당 스크립트 부착.
  * `MazeGrid` (Grid) 및 하위 `Maze` 오브젝트 생성 (Tilemap, TilemapCollider2D, CompositeCollider2D, Rigidbody2D-Kinematic 세팅 완료).
* **해결된 이슈**: 모바일 노치나 19.5:9 등 다양한 화면비에서도 맵이 잘리지 않고 화면에 꽉 차게 렌더링되도록 수학적 보정 완벽 적용.
* **다음 목표**: `Task.md`의 Phase 1.3 (Player Ball 물리 세팅 및 프리팹 생성).

---

### 📅 [2026-03-25] Phase 1.1: 모바일 설정 초기화 (Bootstrapper) 코딩 완료
* **작업 내용**: 
  * `Bootstrapper.cs` 작성 (60FPS 강제 타겟팅, 화면 꺼짐 방지).
  * 씬 로드 시 즉각 실행되도록 빈 오브젝트 `[Bootstrapper]`를 씬 루트에 생성하고 스크립트 부착 완료.
* **해결된 이슈**: 모바일 기기의 답답한 30FPS 기본 제한을 풀고 배터리 절약 강제 모드 해제.
* **다음 목표**: `Task.md`의 Phase 1.2 (해상도 비율 대응 카메라 및 Tilemap 뼈대) 구축.

---

### 📅 [2026-03-25] 기획 및 코어 아키텍처 설계 완료
* **작업 내용**: 
  * `GDD.md` 작성 (다중 색상 공 기믹, 고정 뷰, 물리 한계치, 조작 스냅 방지 등)
  * `.cursorrules` 작성 (에디터 활용, UI 터치 가드, Fast Retry 등 AI 코딩 가이드라인 확립)
  * `Task.md` 작성 (Phase 1~3 체크리스트 구축 완료)
* **주요 해결 사항 (Troubleshooting)**: 
  * [이슈] 모바일 기기 고속 회전 시 공이 벽을 뚫는 현상 (터널링) 예측
  * [해결] Box2D Collision Detection: Continuous 강제 및 Velocity/Position Iterations 상향 규칙 수립
* **다음 목표**: `Task.md`의 Phase 1 (기본 물리 및 조작 프로토타입) 개발 시작.
