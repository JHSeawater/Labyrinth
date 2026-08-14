# Project: Labyrinth
- 이 프로젝트는 유니티 기반의 2D 로테이팅 메이즈 게임입니다. 화면을 터치해 회전시켜 맵 안의 공을 목표 지점에 넣는 것이 목표입니다.
- 사용자는 인디게임 개발자를 지향하는 컴퓨터학부 4학년 학부생이며, 이 프로젝트를 통해 개발 경험을 쌓고 포트폴리오를 만들며, 실제 출시를 통해 실력을 기르고 경험을 쌓는 것을 목표로 합니다. 실제 출시를 목적으로는 하지만, 최대한의 수익 창출을 가장 주된 목적으로 하지는 않습니다.

## Development Environment & AI Integration
- 이 프로젝트는 **MCP for Unity**(`UnityMCP`)를 도입하여, AI 어시스턴트(Claude Code 등)가 유니티 에디터와 실시간 연동되어 씬 구축 및 컴포넌트 제어를 자동화하는 환경에서 개발 중입니다.

## 📖 문서 안내 (Documentation)
프로젝트 문서는 역할별로 분리되어 있습니다. 세부 내용은 해당 문서를 직접 참조하세요.
* **`GDD.md`** — 게임 기획(무엇/왜): 코어 루프·규칙·다중 색상 공·경제·UX
* **`TDD.md`** — 기술 설계(어떻게): B방식·물리·레이어·Fast Retry·풀링·세이브
* **`CLAUDE.md`** — AI 작업 규칙 및 핵심 아키텍처 가드레일
* **`Task.md`** — Phase별 작업 목록 / 진행 상태
* **`DevelopLog.md`** — 개발 타임라인·버그 수정 히스토리

## 🛠 요건 및 사양 (Requirements)
* **Engine**: Unity 6 (6000.3.9f1 버전)
* **Input System**: `New Input System (EnhancedTouch)` (유니티 6 패키지 충돌 방지 및 멀티터치 대응)
* **Target Platform**: Mobile (iOS / Android), *TargetFrameRate = 60+*
* **Physics System**: Unity Box2D (Collision Detection: Continuous 적용)

## 🎮 조작 및 테스트 방법 (How to Test)
* **에디터 (PC)**: 마우스 클릭 후 드래그 (좌우 또는 원형) 시 **카메라 및 중력 회전**을 통한 미로 회전 착시 발생.
* **모바일 빌드**: 터치 및 스와이프 (`finger.index` 검사를 통한 UI 멀티터치 예외 처리 완료).

## 📂 프로젝트 폴더 구조 가이드 (Folder Structure)
AI가 코드 및 에셋을 무분별하게 루트에 배치하지 않도록 아래의 디렉토리 규칙을 엄격히 준수합니다.
* `Assets/Scripts/`: 모든 C# 스크립트 (Manager, Controller 분리 보관)
* `Assets/Prefabs/`: Player(Ball), Goal 오브젝트, **Sprite Shape/Polygon Collider 기반 비정형 장애물** 프리팹
* `Assets/Scenes/`: 씬 파일 (현재 `SampleScene`; 향후 GameScene, LobbyScene 등으로 분리)
* `Assets/Sprites/`: 게임에 사용되는 디자인 그래픽 에셋
* `Assets/PhysicsMaterials/`: 마찰력/반발력 조절용 Physics Material 2D 에셋