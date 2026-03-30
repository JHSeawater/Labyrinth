# 프로젝트 작업 목록 (Task List)

## Phase 1: 기본 물리 및 조작 프로토타입
- [x] **프로젝트 세팅**: 모바일 타겟 프레임(60) 설정 및 화면 꺼짐 방지 (Bootstrapper.cs 구현 완료)
- [x] **환경 구성**: 기기 해상도 대응 및 **미로 대각선 외접원 기준** Orthographic 카메라 구성 + Tilemap 뼈대 구축
- [x] **Player 생성**: 물리 엔진(Box2D) 중력의 영향을 받는 기본 Ball 프리팹 생성 (Continuous, Interpolate 모드, 적절한 크기 설정)
- [x] **조작 구현 (Input)**: 화면 터치 & 드래그 각도(`Mathf.Atan2`) 기반 미로 회전. (UI 이벤트 터치 무시, 속도 상한선 체크 및 보간 적용) (완료)
- [x] **저장소 최적화**: Unity 6 표준 및 로컬 IDE(.vscode) 환경에 맞춘 `.gitignore` 고도화 완료

## Phase 1.5: 물리 아키텍처 리팩토링 (카메라 착시 방식 전환) [최우선]
> **배경**: Kinematic 미로 회전 시 Box2D 표면 속도 전달로 인해 공이 의도치 않게 튀어오르는 버그.  
> **해결**: "카메라 착시 + 중력 회전" 방식으로 전환. 미로는 Transform(0,0,0)으로 완전 고정, 카메라를 역방향 회전, `Physics2D.gravity`를 동기화.
- [x] **[Editor] Maze 루트 오브젝트의 `Rigidbody2D`를 `Static`으로 설정** (CompositeCollider2D 유지를 위해 컴포넌트 존치하되 물리 연산 배제)
- [x] **[Script] `MazeRotator.cs` → `WorldRotationController.cs` 리팩토링**:
    - `Rigidbody2D` 의존성 제거, `MoveRotation()` 삭제
    - `FixedUpdate`에서 각도 보간 계산 후 `Physics2D.gravity` 방향 갱신
    - `CameraController`에 현재 각도를 매 프레임 제공하는 인터페이스 추가
- [x] **[Script] `CameraController.cs` 수정**:
    - `SetWorldRotation(float angle)` 역할의 각도값을 참조하여 **`LateUpdate()`** 에서 `transform.rotation = Quaternion.Euler(0, 0, -angle)` 적용 (Jittering 방지)
- [x] **[Editor] UI Canvas의 Render Mode가 `Screen Space - Overlay`인지 확인** (UI가 함께 돌아가는 현상 방어 완료)
- [x] **[Script] `InputController.cs` 참조 수정**: `MazeRotator` → `WorldRotationController` (SerializeField를 통한 인스펙터 연결 완료)
- [x] **[QA] 검증**: 빠른 회전 시 공이 튀어오르지 않으며, 중력 방향이 화면상 "아래"와 일치함 확인 완료

## Phase 2: 인게임 로직 및 게임 오버 플로우 (Fast Retry)
- [ ] **Goal 로직**: 공이 닿았을 때 클리어(성공) 판정을 내리는 트리거 구현
- [ ] **장애물(Hole/Spike/DeadZone)**: Hole/Spike 및 **장외 이탈(OOB) 감지용 데드존 트리거**와 실패 판정 연동 구현
- [ ] **상태 제어 (GameManager)**: 씬 재로드(LoadScene) 없이 상태계(Transform 및 Velocity)만 초기화하는 빠른 재시작(Fast Retry) 및 **물리 가속도(`zero`) 초기화** 로직 구현
- [ ] **피드백 적용**: 쿨타임(0.1초) 제한이 포함된 햅틱 진동 및 충돌/성공 효과음(SFX/Particle) 로직 추가

## Phase 3: 색깔 공 기믹 (핵심 퍼즐 요소)
- [ ] **다중 공 레이어 설정**: 서로 다른 두 색상의 공을 생성하고, 선택적 통과를 위한 Layer Collision Matrix 분리 구성
- [ ] **단일/다중 복합 승리 조건**: 모든 공이 각자의 색상에 맞는 Goal에 들어갔을 때만 스테이지 클리어로 판정하는 알고리즘 작성

## Phase 4: 구조 확장 및 레벨 제작 고도화 (Advanced Level Design)
- [ ] **데이터 모델링**: ScriptableObject 기반의 스테이지 데이터(제한 시간, 획득 별점 기준 등) 로드 시스템 구현
- [ ] **2D SpriteShape 도입**: Package Manager를 통한 패키지 설치 및 기본 벽면 프로필(Profile) 구축
- [ ] **정밀 기하 구조 테스트**: SpriteShape와 PolygonCollider2D를 활용한 곡선/대각형 미로 테스트 씬 구성 및 **동적 기믹의 독립 콜라이더 워크플로우(Used By Composite 미사용) 검증**
- [ ] **병합 가이드 준수**: 모든 비정형 콜라이더의 'Used By Composite' 활성화 및 Maze 루트 자식 배치 규칙 수립


## Phase 5: 메타 게임 및 UI 플로우 (UI & Meta)
- [ ] **로비 및 월드맵**: 메인 로비 메뉴, 스테이지 선택 UI, 각 스테이지 성과(별점 기록) 표시 시스템 구현
- [ ] **환경설정 팝업**: 사운드 볼륨, 햅틱 ON/OFF, 색각이상(Colorblind) 모드 전환 플로우

## Phase 6: 시청각 폴리싱 (Audio & Polish)
- [ ] **시각 효과(VFX)**: 공 이동 시 잔상 효과(Trail Renderer) 보강 및 Goal 도달 파티클 이펙트
- [ ] **청각/촉각 효과**: 물리 엔진 속도 수치에 비례한 BGM/SFX/Haptic 세분화 연동

## Phase 7: 모바일 최적화 및 출시 (Optimization & Release)
- [ ] **프로파일링**: GC(Garbage Collector) 튜닝 및 메모리 누수 점검
- [ ] **모바일 디바이스 테스트**: 실제 기기 터치 조작감 튜닝, Safe Area 노치 대응 완벽 검토
- [ ] **오브젝트 풀링(Object Pooling)**: 파티클 및 SFX 시스템에 풀링 적용 및 메모리 누수 점검
