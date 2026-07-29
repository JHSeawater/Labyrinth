# Development Log (개발 일지)

이 문서는 프로젝트의 개발 타임라인, 주요 변경 사항, 마주친 버그 및 해결 방법(Troubleshooting)을 기록하는 공간입니다. 

## [작성 규칙]
1. 나(AI)는 특정 Task(예: Phase 1)가 완료되거나 중요한 버그를 수정했을 때, 이 문서의 **최상단(가장 위)**에 새로운 로그를 추가합니다.
2. 각 로그는 **날짜(YYYY-MM-DD), 제목, 내용, 해결된 이슈** 포맷을 포함해야 합니다.

---

### 📅 [2026-07-06] 색상 게이트 방향성 확정 & 색 시스템 구현 방식(레이어 vs 코드) 결정

* **작업 배경**: Phase 3.5 점검 중 씬에 유휴 색상 레이어(`Ball_/Gate_*`)가 미배선으로 남아 있어, 색 게이트를 (a)Layer Collision Matrix로 갈지 (b)코드로 갈지 방향을 확정해야 했음. 아울러 "게이트가 같은 색을 통과시키는가/막는가"의 기획 직관 판단이 필요했음.
* **결정 1 — 게이트 방향(기획, GDD §5.1.1 신설)**: **같은 색 통과 / 다른 색 차단**으로 확정. 근거: ①색=소속/친화의 보편 관습, ②Goal의 "같은 색=수용"과 규칙 일원화(단일 규칙 학습). "같은 색 차단"은 골과 정반대 의미라 인지 부조화 → 기각. 비주얼은 반드시 **통과 가능해 보이는 형태**(에너지 장막/뚫린 프레임), 색맹 대응 문양 각인 병행. "차단형" 게이트는 후반 별도 도구로만(시각 구분 필수).
* **결정 2 — 구현 방식(TDD §5.2 보강)**: **역할 분담형 하이브리드** 확정. **게이트 통과/차단=Layer Matrix**(고체 통과는 브로드페이즈에서만 깨끗이 결정, 런타임 상태 0 → Fast Retry 무부담·GC 0), **Goal 정답 판정=코드 `ColorType` 유지**(정체성 판정+오브젝트 제거이므로 코드가 최적, 현행 `Goal.cs`가 이미 그러함). 매트릭스 구성: `Gate_X`는 `Ball_X`만 무시(통과)·나머지 공과 충돌, 공-공 전 쌍 ON.
* **핵심 통찰 기록**: Goal은 Solid 콜라이더라 "오답=벽"이 무료로 나오고 코드는 "정답 제거"만 하면 되어 코드가 우아함. 반면 게이트의 "정답색 통과"는 `OnCollisionEnter` 시점엔 이미 충돌이 해결된 뒤라 코드로 되돌릴 수 없음 → 브로드페이즈(레이어)에서 결정해야만 함. 이 비대칭이 하이브리드의 근거.
* **해결된 이슈**: 유휴 색상 레이어의 존재 이유/처리 방침 확정(제거 아닌 Phase 4 배선 예정), 색 게이트 기획·구현 방향 미확정 상태 해소. GDD·TDD 동기화 완료.

---

### 📅 [2026-06-30] 에셋 폴더 구조 정규화 (README 규칙 일치)

* **작업 내용**: README가 선언한 폴더 규칙과 실제 배치가 어긋난 점을 정정. `Assets/Prefabs/`에 섞여 있던 에셋을 규칙대로 이동 — `Ball/Wall Physics Material.physicsMaterial2D` → `Assets/PhysicsMaterials/`, `Wall.png` → `Assets/Sprites/`. Unity 에디터가 닫힌 상태에서 에셋과 `.meta`를 **짝으로** 이동해 GUID/참조 보존(다음 에디터 실행 시 자동 반영). SpriteShape 관련 에셋(`Square.asset`, `Sprite Shape Profile.asset`)은 전용 폴더 규칙이 없어 `Prefabs/`에 존치.
* **해결된 이슈**: 문서(README)가 선언한 폴더 구조와 실제 프로젝트 레이아웃의 불일치 제거. 본격 레벨 제작(Phase 4) 전 에셋 분류 체계 정렬.

---

### 📅 [2026-06-29] 문서 구조 개편: GDD/TDD 분리 및 기획 정합성 정정

* **작업 배경**: 단일 `GDD.md`에 게임 디자인(무엇/왜)과 구현 스펙(어떻게)이 혼재되어 가독성·드리프트 문제가 있었고, 검토 과정에서 내부 모순 몇 건이 식별됨.
* **주요 작업 내용**:
  * **문서 분리**: `TDD.md` 신설. GDD의 기술/구현 내용(B방식 수식, 물리, 레이어, Fast Retry, 풀링, 카메라, 세이브, 스크립트 현황)을 **실제 코드 기준으로** 이관·정정. `GDD.md`는 순수 기획(코어 루프·규칙·다중공·경제·UX)으로 재작성.
  * **모순 정정(P0)**: ① "미로 회전" 표현 → "중력/카메라 회전, 미로는 Static"으로 용어 통일. ② DeadZone 판정을 코드(`OnTriggerEnter2D`)에 맞춰 **'진입=실패'로 확정**(구 "닿거나 벗어나는" 모순 제거). ③ Fast Retry 초기화 범위에 **동적 기믹·풀·비활성 공 재활성화** 명문화. ④ **별(성취 기록) ↔ 소프트 화폐(소비재) 분리**로 경제 설계 결함 해소.
  * **설계 보강(P1)**: 카메라 착시 부호를 코드 검증(`R(θ)·(−sinθ,−cosθ)=(0,−1)` → 중력 항상 화면 아래)하여 계약으로 명시. 다중 공 상호작용·승패 동시 발생(패배 우선) 규칙 정의. 색상 레이어 예산 주의/하이브리드 매칭 기록.
  * **누락 섹션 추가(P2)**: 타겟 유저, 콘텐츠 스코프, 난이도 곡선/메커닉 도입, 튜토리얼/FTUE, 화면 전이도, 접근성(모션 완화 포함), 현지화, KPI/분석 이벤트, MVP/리스크 레지스터, 용어집, 버전 헤더·변경 이력.
  * **CLAUDE.md 동기화**: 문서 맵에 `TDD.md` 추가, §3 핵심 불변식 명시, §3~7에 TDD 참조 연결 및 Fast Retry 초기화 범위 보강.
  * **공-공 충돌 확정**: 다중 공이 색에 관계없이 서로 충돌(핀볼식)하도록 GDD/TDD §5.3 확정 및 Matrix 구현 지침 기록.
  * **잔여 문서 정리**: `Task.md`에 Phase 3.5(공-공 Matrix)·세이브·경제(별/화폐 분리)·모션 완화·KPI·기기사양·Deferred 섹션 추가. `README.md`에 문서 안내 맵 추가 및 MCP 명칭 정정(`MCP for Unity`/`UnityMCP`), 현재 씬(SampleScene) 반영. CLAUDE.md 헤더의 구 규칙파일(`.cursorrules`/`GEMINI.md`, 이미 제거됨) 안내 정리.
  * **문서 드리프트 재점검·정정**: 3대 문서(CLAUDE/TDD/GDD) 교차 검토에서 `CLAUDE.md`만 코드와 어긋난 2건을 실제 코드 기준으로 정정 — ① `enum ColorType`에 `Default` 누락(`Goal.cs:3`과 불일치) 보완, ② Unity 6에서 deprecated된 `velocity` → `linearVelocity`(`PlayerBall.cs:49`) 표기 정정. `PoolManager`/풀링은 '예정·미구현(Phase 7)'로 명시해 구현 상태 오해 소지 제거.
* **해결된 이슈**: 기획/구현 혼재로 인한 가독성·드리프트 문제와 문서 내부 논리 모순(데드존 판정, 별/화폐 이중 용도, 미로 회전 용어)을 제거하고, 코드와 전체 문서 세트(GDD·TDD·CLAUDE·Task·README)의 정합성을 확보함.

---

### 📅 [2026-03-30] Phase 4: 구조 확장 (레벨 데이터 모델링) 및 Jitter 물리 최적화

* **작업 내용**:
  * **핵심 물리 최적화**: `PlayerBall.cs`에서 `FastRetry()` 수행 시 Transform 할당으로 인해 발생하는 Box2D Spatial Hash 트리 리빌딩 미세 튀는 현상(Jitter)을 방지하기 위해, `_rb.position`과 `_rb.rotation` API를 직접 할당하는 방식으로 마이그레이션 적용. (유저 피드백 수용 완료)
  * **데이터 아키텍처 모델링**: 스테이지 레벨 번호 및 별점 컷오프 타임(15초 3별 등)을 하드코딩하지 않고 관리하기 위해 `ScriptableObject` 상속 객체 `StageData.cs` 인프라 신설.
  * **전역 타이머 로직**: `GameManager.cs`에 인게임 플레이 타이머(`_playTimer`) 및 `CalculateStars()` 메소드를 구축하여 Clear 진입 시 콘솔에 획득 별점을 로깅하도록 작성.
* **해결된 이슈**: 확장성 없는 하드코딩된 기획 수치를 독립된 데이터 분리 구조로 탈바꿈하고 물리 엔진 위치 동기화를 완벽하게 보장함.

---

### 📅 [2026-03-30] Phase 3: 다중 색상 공 기믹 및 다중 골 판정 구현 완료

* **작업 내용**:
  * **자율적 공 상태 제어 (`PlayerBall.cs`)**: 여러 개의 공이 존재할 수 있도록, 각 공이 자신의 고유 색상(`ColorType`), 초기 위치, 리셋 로직(`FastReset`)을 스스로 책임지도록 설계 및 컴포넌트 신설.
  * **`GameManager` 병렬 클리어 판정**: 씬에 존재하는 공의 갯수(`TotalBallsCount`)를 추적하고, 조건이 일치하는 골에 들어간 공의 갯수(`ReachedBallsCount`)가 똑같아질 때만 씬 클리어 코루틴을 돌리도록 통계 로직 개편.
  * **게이트 물리 튕겨냄 (`Goal.cs` 변경)**: 기존 `OnTriggerEnter2D`를 `OnCollisionEnter2D` 방식으로 전환하여 색상이 불일치할 경우 퍼즐의 벽처럼 물리 반발력을 유지하도록 구조 개선.
* **해결된 이슈**: 1개의 공만 추적하던 하드코딩된 로직을 벗어나 확장성 있는 다중 공 처리의 기반을 성공적으로 마련함. (유니티 Collision Matrix 튜닝은 에디터 수동 설정 가이드로 제공)

---

### 📅 [2026-03-30] Phase 2: 인게임 핵심 로직 및 Fast Retry 구현 (코드 작성 완료)

* **작업 내용**:
  * **상태 제어 (GameManager.cs)**: `GameState` 정의 및 코루틴(1.5초 딜레이) 기반의 클리어 이벤트 처리. 물리 상태(Velocity)와 Transform을 0으로 강제 복원하는 `FastRetry()` 무한 루프 구현 완료.
  * **피드백 시스템 (FeedbackManager.cs)**: `0.1초 쿨타임`이 강제된 `PlayHaptic(intensity)` 인터페이스 구축.
  * **환경 오브젝트**: `Goal.cs`(활성화 제어 및 이벤트 우선순위 처리), `DeadZone.cs`(장외 이탈 방지), `Obstacle.cs` 트리거 방어 완료.
  * **핵심 매니저 초기화 보완**:
    * `WorldRotationController.FastReset()`: 다음 프레임 보간(Lerp) 연산을 차단하기 위해 `_targetAngle = 0`, `_currentAngle = 0` 동시 대입 후 `ApplyGravity(0)` 강제.
    * `InputController.ResetInput()`: 에디터/마우스 로직의 영구 무시 버그를 차단하기 위해 `_isUsingTouch = false` 초기화.
* **해결된 이슈**: 씬 재로드(LoadScene) 없이도 메모리 누출이나 오작동(버그) 없이 즉각 처음 상태로 복원되는 Zero-Overhead 재시작 모델 안착.

---

### 📅 [2026-03-30] 프로젝트 설정 최적화: .gitignore 업데이트 및 환경 정리

* **작업 배경**: Unity 6 버전 대응 및 로컬 개발 환경(.vscode, .unity 등)의 불필요한 파일이 저장소에 추적되는 것을 방지하기 위한 설정 최적화.
* **주요 작업 내용**:
  * **Unity 6 대응**: `BurstCache/`, `GraphVisualization/` 등 최신 캐시 폴더 추가.
  * **로컬 환경 격리**: 프로젝트 루트의 `.vscode/`, `.unity/`, `.idea/` 폴더를 무시하도록 설정하여 개인별 설정 충돌 방지.
  * **시스템 파일 제거**: `.DS_Store`, `Thumbs.db` 등 OS 생성 찌꺼기 파일 차단 목록 강화.
  * **백업 폴더 관리**: `/[Bb]ackup/`, `/[Bb]ackups/` 패턴 추가.
* **해결된 이슈**: 로컬 도구 설정 파일이 Git 저장소에 포함되어 발생할 수 있는 팀원 간 설정 충돌 및 리포지토리 크기 비대화 예방.

---

### 📅 [2026-03-30] 최종 피드백 반영: OOB 데드존 및 오브젝트 풀링 설계 완료

* **작업 배경**: 물리 엔진 한계로 인한 소프트락 방지 및 빈번한 재시작 환경에서의 모바일 성능 최적화 전략 수립.
* **주요 작업 내용**:
  * **데드존(Dead Zone) 도입**: 미로 외곽에 거대 트리거를 배치하여 공이 벽을 뚫고 이탈할 경우 즉시 실패(Fast Retry)로 처리하는 '방어적 물리 설계' 반영.
  * **오브젝트 풀링(Object Pooling) 명문화**: 파티클 및 SFX 시스템에 생성/파괴 대신 비활성화/활성화 방식을 채택하여 GC 부하 최소화 및 60FPS 안정성 확보.
  * **아키텍처 확장**: `PoolManager`를 핵심 매니저 그룹에 추가하여 전역적인 리소스 재사용 기반 마련.
* **해결된 이슈**: 희박한 확률로 발생하는 장외 이탈 버그 대응 및 반복 플레이 시 발생하는 성능 저하 예방.

---

### 📅 [2026-03-30] GDD 주요 피드백 반영 및 아키텍처 가이드라인 수립


* **작업 배경**: 외부 피드백을 통해 식별된 물리 엔진 성능 이슈(CompositeCollider 런타임 Rebuild), 카메라 클리핑, 재시작 시 물리 잔존력 버그 등을 사전에 방어하기 위한 문서 최적화.
* **주요 작업 내용**:
  * **물리 최적화**: 문, 파괴 블록 등 동적 기믹은 `CompositeCollider2D` 병합에서 제외하도록 명시 (런타임 렉 방지).
  * **카메라 로직 개선**: 회전 시 모서리 잘림 방지를 위해 '미로 대각선 외접원' 기준으로 `orthographicSize`를 설정하는 수식 도입 반영.
  * **승리 조건 정교화**: 먼저 골인한 공은 즉시 `Disable` 처리하여 물리 부하 감소 및 다른 공의 경로 방해 차단.
  * **Fast Retry 안정성**: 재시작 시 `velocity`, `angularVelocity`를 `0`으로 강제 초기화하도록 태스크 구체화.
* **해결된 이슈**: 대규모 맵이나 복잡한 기믹 추가 시 발생할 수 있는 성능 저차 및 물리 버그를 아키텍처 레벨에서 차단.

---

### 📅 [2026-03-30] 레벨 제작 도구 확장 기획 및 문서 업데이트 (Advanced Geometry)


* **작업 배경**: 기존 정사각형 타일맵 방식의 단조로움을 탈피하고곡선, 대각선 등 정교한 레벨 디자인을 가능하게 하기 위한 기술적 검토 및 기획 내용 반영.
* **주요 작업 내용**:
  * **GDD 업데이트**: 2D SpriteShape 도입 명문화 및 CompositeCollider2D 병합을 통한 '물리 매터리얼 일괄 제어' 이점 추가.
  * **Task 업데이트**: Phase 4를 '레벨 제작 고도화'로 구체화하여 SpriteShape 패키지 설치 및 검증 단계를 세부 항목으로 분리.
  * **아키텍처 가이드**: 비정형 콜라이더 사용 시 `Used By Composite` 옵션 활성화 및 `Maze` 루트 자식 배치 규칙 수립.
* **해결된 이슈**: 레벨 디자이너가 물리 설정을 일일이 수정하지 않고도 전체 맵의 물리 속성을 한 번에 관리할 수 있는 워크플로우 확립.

---

### 📅 [2026-03-29] [정기 점검] 프로젝트 문서 논리적 정렬 및 동기화 (Sync)


* **작업 배경**: Phase 1.5 아키텍처 변경(B-Mode) 및 입력 시스템 마이그레이션 이후, 구버전 정보가 남은 문서(`README`, `GDD`)들을 최신화하여 AI 개발 컨텍스트의 일관성을 확보함.
* **주요 작업 내용**:
  * **Input System**: `README.md` 및 `GDD.md`에서 Legacy Input Manager 참조를 모두 삭제하고 `New Input System (EnhancedTouch)`으로 업데이트.
  * **Physics Rules**: `.cursorrules`에서 공의 `gravityScale` 조정 허용(GDD 조작감 대응) 및 미로의 `Static` 속성(B-Mode)을 명문화하여 `MoveRotation()` 등 구버전 조작 방식의 오남용 차단.
  * **Path Fixes**: `.cursorrules` 내 `GDD.md` 참조 경로 오류 수정 (Root 경로).
* **해결된 이슈**: AI가 구버전 문서를 보고 잘못된 물리 연산이나 입력 API를 작성할 위험을 원천 제거함.

---

### 📅 [2026-03-29] Phase 1.5: 씬 클린업 및 리팩토링 최종 검증

* **작업 내용**:
  * `Maze` 오브젝트: `Rigidbody2D`를 `Static`으로 변경하여 `CompositeCollider2D` 기능을 유지하면서 표면 속도 버그 원천 차단 (B방식 최적화).
  * `InputManager`: `InputController` → `WorldRotationController` 인스펙터 레퍼런스 연결 완료.
  * `WorldRotationController`: `Main Camera`의 `CameraController` 레퍼런스 연결 완료.
  * 모든 핵심 스크립트 변수 캡슐화(`[SerializeField] private`) 및 참조 안전성 검증 완료.
* **해결된 이슈**: 씬 리셋 시 참조 누락 방지 및 물리 엔진 안정성 완벽 확보.

---

### 📅 [2026-03-27] Phase 1.5: 물리 아키텍처 리팩토링 (카메라 착시 방식)

* **배경 및 원인**: 기존 Kinematic `Rigidbody2D.MoveRotation()` 방식에서 Box2D의 **표면 속도 전달(Surface Velocity Transfer)** 버그 발견. 빠른 회전 시 공이 의도치 않게 튀어오르는 현상 발생. `Continuous`, 속도 클램핑으로 근본 해결 불가.
* **해결 방식 채택 (B방식: 카메라 착시 + 중력 회전)**:
  * 미로 오브젝트를 `Transform(0,0,0)` 완전 고정 → Static Collider로 전환, Static Collider Rebuild 성능 저하 원천 차단.
  * `Physics2D.gravity` 방향을 인터랙션 각도에 맞게 매 `FixedUpdate`에서 갱신.
  * `CameraController.LateUpdate()`에서 `-angle`로 카메라 역방향 회전 → 유저 눈에는 미로가 도는 착시 효과.
* **변경 파일**:
  * `WorldRotationController.cs` [신규]: 기존 `MazeRotator.cs` 역할 대체. 각도 보간 + `Physics2D.gravity` 갱신 + `CameraController` 호출 담당.
  * `CameraController.cs` [수정]: `SetWorldRotation(float angle)` 메서드 추가, `LateUpdate()`에서 카메라 역회전 적용.
  * `InputController.cs` [수정]: `MazeRotator` → `WorldRotationController` 참조 교체, 개발 임시 진단 로그 삭제.
  * `GDD.md` [수정]: 7장 아키텍처 원칙을 B방식으로 업데이트, 3장 조작 방식 설명 보완.
* **주의 사항 (QA 필수)**:
  * ⚠️ UI Canvas의 Render Mode가 **`Screen Space - Overlay`** 인지 반드시 확인 (Camera 회전 시 HUD 같이 돌아가는 버그 방지).
  * ⚠️ 배경에 이미지/스프라이트가 있다면 해당 오브젝트를 **Main Camera의 자식**으로 이동해야 착시 유지됨 (현재 단색 배경이면 불필요).

---


### 📅 [2026-03-27] Phase 1 스크립트 리팩토링 및 터치 구조 개선
* **작업 내용**:
  * `CameraController.cs`, `MazeRotator.cs`: 외부 접근이 불필요한 설정 변수들을 `[SerializeField] private`로 캡슐화 처리하여 보안 및 프로젝트 룰 `.cursorrules` 준수.
  * `InputController.cs`: 
    1. 메모리 최적화: `UpdateDrag`에서 매 프레임 발생하던 `Debug.Log` 문자열 할당부를 삭제하여 프레임 스파이크 방지.
    2. 터치 대응 고도화: 단일 포인터(`Pointer.current`) 추적 방식에서 `UnityEngine.InputSystem.EnhancedTouch.Touch` 기반으로 업그레이드. 처음 닿은 `finger.index`만 추적하도록 하여 화면에 추가 손가락이 닿아도 회전 중심이 튀지 않음.
    3. UI 터치 가드 보충: `EventSystem.current.IsPointerOverGameObject(touch.finger.index)`로 각 터치 슬롯에 대한 UI 점유 여부를 정확히 판단하도록 방어 코드 보강.
* **해결된 이슈**: 다중 터치 시 오작동 가능성 차단 및 가비지 컬렉터(GC) 부하 해소.

---

### 📅 [2026-03-26] 이슈 해결: New Input System 패키지로 인한 Legacy Input 차단 버그
* **문제 상황**: 에디터의 Game 뷰에서 마우스를 드래그해도 미로가 회전하지 않고, `Input.GetMouseButtonDown` 등의 레거시 코드가 아예 이벤트를 수신하지 못하고 조용히 씹히는 현상 발생.
* **원인 분석**: 프로젝트에 `com.unity.inputsystem` (New Input System) 패키지가 기본 설치 및 활성화되어 있어, 유니티 엔진이 기존 Legacy Input Manager 방식의 API 호출을 강제로 비활성화(에러 처리)시킨 상태였음.
* **조치 사항**: 플레이어 세팅에서 복잡하게 Active Input Handling을 되돌리는 대신, `InputController.cs`를 최신의 견고한 Input System API인 `UnityEngine.InputSystem.Pointer.current`를 직접 사용하도록 전면 업그레이드 마이그레이션 완료.
* **교훈 및 규칙**: 유니티 6 환경에서는 Legacy Input 코드가 호환성 충돌로 무력화될 가능성이 매우 높으므로, 향후 입력 코드 작성 시 반드시 `Pointer.current` 등 New Input System API를 1순위로 사용할 것.

---

### 📅 [2026-03-26] Phase 1.4: 터치/물리 회전 로직 탑재 (Phase 1 완료)
* **작업 내용**: 
  * `MazeRotator.cs`: FixedUpdate 내 Rigidbody2D.MoveRotation을 통한 물리 회전 적용. 고속 스와이프 방지를 위한 최대 회전각 Clamp. 
  * `InputController.cs`: Legacy Input 기반 모바일 터치 및 마우스 드래그 추적. `MazeRotator`로 직접 참조(Method Call) 통신 구축을 통해 Update 오버헤드 최소화.
  * 씬에 `InputManager` 빈 오브젝트 및 `EventSystem` 구축 완료.
* **해결된 이슈**: 터치 스냅 방지 완벽 수식(Touch 시점 오프셋 연산) 적용. UI 클릭 시 미로 회전 무시(`fingerId` 인자 포함) `IsPointerOverGameObject` 처리.
* **다음 목표**: 전체 핵심 뼈대인 Phase 1 작업 완료. 인게임 Goal 및 Game Manager 관련 Phase 2 진입 대기.

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
