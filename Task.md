# 프로젝트 작업 목록 (Task List)

## Phase 1: 기본 물리 및 조작 프로토타입
- [x] **프로젝트 세팅**: 모바일 타겟 프레임(60) 설정 및 화면 꺼짐 방지 (Bootstrapper.cs 구현 완료)
- [x] **환경 구성**: 해상도 비율(Aspect Ratio) 대응 고정 카메라(Orthographic) 구성 및 Tilemap + CompositeCollider2D 기반 미로(Maze) 뼈대 구축
- [x] **Player 생성**: 물리 엔진(Box2D) 중력의 영향을 받는 기본 Ball 프리팹 생성 (Continuous, Interpolate 모드, 적절한 크기 설정)
- [ ] **조작 구현 (Input)**: 화면 터치 & 드래그 각도(`Mathf.Atan2`) 기반 미로 회전. (UI 이벤트 터치 무시, 속도 상한선 체크 및 보간 적용)

## Phase 2: 인게임 로직 및 게임 오버 플로우 (Fast Retry)
- [ ] **Goal 로직**: 공이 닿았을 때 클리어(성공) 판정을 내리는 트리거 구현
- [ ] **장애물(Hole/Spike)**: 닿았을 때 즉시 실패 판정을 내리는 트리거 구현
- [ ] **상태 제어 (GameManager)**: 씬 재로드(LoadScene) 없이 상태계(Transform 및 Velocity)만 초기화하는 빠른 재시작(Fast Retry) 유체계 기반 FSM 로직 구현
- [ ] **피드백 적용**: 쿨타임(0.1초) 제한이 포함된 햅틱 진동 및 충돌/성공 효과음(SFX/Particle) 로직 추가

## Phase 3: 색깔 공 기믹 (핵심 퍼즐 요소)
- [ ] **다중 공 레이어 설정**: 서로 다른 두 색상의 공을 생성하고, 선택적 통과를 위한 Layer Collision Matrix 분리 구성
- [ ] **단일/다중 복합 승리 조건**: 모든 공이 각자의 색상에 맞는 Goal에 들어갔을 때만 스테이지 클리어로 판정하는 알고리즘 작성

## Phase 4: 구조 확장 & 레벨 툴링 (Level Design)
- [ ] **데이터 모델링**: ScriptableObject 기반의 스테이지 데이터(제한 시간, 획득 별점 기준 등) 로드 시스템 구현
- [ ] **레벨 빌더**: Tilemap을 이용한 다수 맵 제작 시 수정/테스트가 용이한 게임 플레이 환경 툴링

## Phase 5: 메타 게임 및 UI 플로우 (UI & Meta)
- [ ] **로비 및 월드맵**: 메인 로비 메뉴, 스테이지 선택 UI, 각 스테이지 성과(별점 기록) 표시 시스템 구현
- [ ] **환경설정 팝업**: 사운드 볼륨, 햅틱 ON/OFF, 색각이상(Colorblind) 모드 전환 플로우

## Phase 6: 시청각 폴리싱 (Audio & Polish)
- [ ] **시각 효과(VFX)**: 공 이동 시 잔상 효과(Trail Renderer) 보강 및 Goal 도달 파티클 이펙트
- [ ] **청각/촉각 효과**: 물리 엔진 속도 수치에 비례한 BGM/SFX/Haptic 세분화 연동

## Phase 7: 모바일 최적화 및 출시 (Optimization & Release)
- [ ] **프로파일링**: GC(Garbage Collector) 튜닝 및 메모리 누수 점검
- [ ] **모바일 디바이스 테스트**: 실제 기기 터치 조작감 튜닝, Safe Area 노치 대응 완벽 검토
