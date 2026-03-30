# Project: Labyrinth
- 이 프로젝트는 유니티 기반의 2D 로테이팅 메이즈 게임입니다. 화면을 터치해 회전시켜 맵 안의 공을 목표 지점에 넣는 것이 목표입니다.

## Development Environment & AI Integration
- 이 프로젝트는 **MCP For Unity**를 도입하여, AI 어시스턴트(Antigravity 등)가 유니티 에디터 창과 실시간으로 연동되어 씬 구축 및 컴포넌트 제어를 자동화하는 환경에서 개발 중입니다.

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
* `Assets/Scenes/`: 씬 파일 (GameScene, LobbyScene 등)
* `Assets/Sprites/`: 게임에 사용되는 디자인 그래픽 에셋
* `Assets/PhysicsMaterials/`: 마찰력/반발력 조절용 Physics Material 2D 에셋