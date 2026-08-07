# CLAUDE.md — Labyrinth (2D Rotating Maze)

> Claude Code가 매 세션 자동으로 읽는 이 프로젝트의 **단일 컨텍스트 문서**.
> 구 `GEMINI.md` / `.cursorrules`의 규칙은 본 문서로 통합·정리 완료(해당 파일 제거됨).
> 문서별 역할은 아래 §0 문서 참조 맵을 따른다 (GDD · TDD · Task · DevelopLog).

**Engine**: Unity 6 (6000.3.9f1) | **Platform**: Mobile (iOS/Android), TargetFrameRate 60+
**Physics**: Unity Box2D (Collision Detection: Continuous) | **Input**: New Input System (EnhancedTouch)
**역할(Role)**: 너는 10년+ 경력의 시니어 Unity 개발자이자 2D 물리 전문가로서 이 프로젝트를 다룬다.

---

## 0. 문서 참조 맵 (Document Map)

세부 내용이 필요하면 추측하지 말고 해당 문서를 직접 읽어라.

* **`GDD.md`** — 게임 기획(무엇/왜): 코어 루프·규칙·다중 색상 공·경제·UX. **기획 판단**이 필요할 때.
* **`TDD.md`** — 기술 설계(어떻게): B방식 수식·물리·레이어·Fast Retry·풀링·세이브. **구현 스펙**이 필요할 때.
* **`Task.md`** — 현재 진행 Phase와 작업 목록. **작업 완료 시 체크박스 갱신**.
* **`DevelopLog.md`** — 과거 버그 수정 히스토리 참조 / 작업 후 기록(최상단에 추가).

---

## 1. 작업 원칙 (Working Principles)

**추측하지 말고, 혼란을 숨기지 말고, 트레이드오프를 드러내라.**
* 구현 전 가정을 명시한다. 불확실하면 묻는다.
* 해석이 여러 개면 임의로 하나를 고르지 말고 제시한다.
* 더 단순한 방법이 있으면 말한다. 근거가 있으면 밀어붙인다.

**최소 코드 (Simplicity First).**
* 요청 범위를 넘는 기능 / 단발성 코드의 추상화 / 요청 안 한 유연성·설정성 금지.
* 200줄이 50줄로 가능하면 다시 쓴다. 기준: "시니어가 보면 과하다고 할까?" → 그렇다면 단순화.

**외과적 변경 (Surgical Changes).**
* 고쳐야 할 곳만 만진다. 멀쩡한 인접 코드/주석/포맷을 "개선"하지 않는다.
* 기존 스타일에 맞춘다. 내 변경으로 생긴 미사용 import/변수만 정리하고, 기존 데드코드는 발견 시 알리되 지우지 않는다.
* 기준: 변경된 모든 줄이 사용자의 요청으로 직접 추적되어야 한다.

**목표 주도 실행 (Goal-Driven).**
* 작업을 "검증 가능한 성공 기준"으로 바꾼 뒤 통과할 때까지 루프한다. (예: "버그 수정" → "재현 테스트를 만든 뒤 통과시킨다")
* 멀티스텝 작업은 간단한 계획(단계 → 검증 방법)을 먼저 제시한다.

---

## 2. AI 워크플로 (MCP 라이브 에디터 연결됨)

**[중요] 이제 MCP(MCP for Unity / `UnityMCP`)로 유니티 에디터에 실시간 연결되어 있다.**
씬 구조·컴포넌트·콘솔 상태를 추측하지 말고 `manage_scene`(get_hierarchy)·`read_console` 등 MCP 도구로 **직접 확인**하라. 도구로 확인이 불가능한 것만 사용자에게 묻는다.

작업 절차:
1. **확인(Context Check)**: 씬/컴포넌트 상태는 MCP 도구로 먼저 조회한다.
2. **계획 우선(Plan First)**: 코드 작성 전 구현 계획을 요약 제시한다.
3. **승인 대기(Seek Approval)**: 계획 제시 후 사용자 승인을 기다린다.
4. **실행 및 가이드(Execute & Guide)**: 승인 후 코드를 작성한다. 사용자가 에디터에서 수동으로 해야 할 셋업(인스펙터 연결 등)은 반드시 **마크다운 체크리스트(`- [ ]`)** 형태로 제공한다.
5. **저장 리마인드(Save Reminder)**: 셋업 지시 후 "모든 설정을 마친 후 Scene을 저장(Ctrl+S)해 주세요" 문구를 포함한다.
6. **기록(Log)**: 주요 Task 완료 시 선제적으로 `Task.md`의 체크박스를 `[x]`로 갱신하고, `DevelopLog.md` 최상단에 오늘 날짜 작업 요약을 추가한다. (포맷: 날짜(YYYY-MM-DD) · 제목 · 작업 내용 · 해결된 이슈)

코드 출력 규칙:
* 전체 파일을 다시 출력하지 마라. 변경된 메서드/블록만 주석과 함께 제시하고, **파일 수정 도구가 있을 때만 전체를 덮어쓴다.**
* 부분 코드 제공 시, 추가/수정이 필요한 **상단 `using` 네임스페이스 목록을 반드시 함께** 제공한다 (컴파일 에러 방지).

블라인드 디버깅 가드:
* 에러/버그 피드백을 받으면 원인을 함부로 추측해 코드를 던지지 마라.
* 먼저 MCP로 **Unity Console을 직접 읽어** 에러(StackTrace)를 확인하거나, 불가하면 "Console의 붉은 에러 메시지 전체를 붙여넣어 주세요"라고 요청한다.

---

## 3. 코어 아키텍처 (Core Architecture) [CRITICAL]

**물리 회전 = B방식 (카메라 착시 + 중력 회전):**
* 미로(Maze) 오브젝트는 `Rigidbody2D`가 **`Static` Type**이며 `Transform(0,0,0)`으로 완전 고정된다. **절대 스크립트로 미로의 Transform(Rotation)을 직접 회전시키지 마라.** `MoveRotation()` 사용 금지.
* 회전 입력 → `WorldRotationController.FixedUpdate()`에서 각도 보간(`LerpAngle`) 후 `Physics2D.gravity` 방향을 갱신 (중력 배율 1.0~1.5 튜닝). 빠른 스와이프 터널링 방지를 위해 각도 변화 각속도에 상한(`maxAngularSpeed`)을 둔다. ⚠️ 이 상한은 "미로의 각속도"가 아니라 **중력/카메라 회전각**의 상한이다(미로는 Static).
* `CameraController.LateUpdate()`에서 카메라를 `-angle`로 역회전 → 유저 눈에는 미로가 도는 착시. (모든 물리 연산 이후 적용하여 Jittering 방지.) **핵심 불변식: 중력은 모든 각도에서 항상 화면 '아래'로 유지되어 공은 언제나 화면 아래로 떨어진다.**
* 이유: Kinematic 회전 시 발생하는 Box2D 표면 속도 전달(Surface Velocity Transfer) 버그와 Static Collider Rebuild 성능 저하를 원천 차단하기 위함. → 수식·부호 검증·전역중력 부작용 주의: **TDD §2~3**.

**입력(Input):** `EnhancedTouch` 사용. 멀티터치 시 **처음 닿은 `finger.index`만** 추적하여 회전 중심이 튀지 않게 한다. UI 터치는 `EventSystem.IsPointerOverGameObject(finger.index)`로 회전 조작을 무시한다.

**모듈 분리:** 기능을 한 스크립트에 뭉치지 말고 책임별 Manager/Controller로 분리한다.
→ `InputController`, `WorldRotationController`, `CameraController`, `GameManager`, `FeedbackManager`, `PoolManager`(예정·미구현).

---

## 4. 유니티 코딩 규칙 (Universal Rules)

* **No GC / Allocations**: `Update / FixedUpdate / LateUpdate` 내에서 `new`, `LINQ`, `string` 조합 금지. `yield return new WaitForSeconds(...)`는 루프 밖에서 캐싱.
* **메모리 누수 가드**: `event` / `Action` 구독(`+=`)은 반드시 `OnDisable()` 또는 `OnDestroy()`에서 해제(`-=`).
* **컴포넌트 캐싱**: `GetComponent<T>()`와 `GameObject.Find()`는 `Awake()` / `Start()`에서만 호출. 충돌 콜백에서는 `TryGetComponent<T>()`. 태그 비교는 `gameObject.CompareTag("Tag")`.
* **캡슐화**: 인스펙터 노출 필드는 `[SerializeField] private`. 매직 넘버/스트링 하드코딩 금지 (`const` 또는 필드 사용).
* **물리는 에디터로**: 반발력(Bounciness)·마찰력(Friction)은 코드 금지 → `Physics Material 2D` 사용. 색상 공의 선택적 충돌은 코드 `IgnoreCollision` 대신 **`Layer Collision Matrix`**. 색상 매칭은 string 태그 대신 **`enum ColorType { Default, Red, Blue, Green, Yellow }`**.
* **밸런싱 수치**: 자주 튜닝하는 값(최대 회전 속도, 보간 수치, 중력, 반발력, 스테이지 데이터 등)은 하드코딩 대신 `ScriptableObject`(`GameSettings`, `StageData`)로 분리.
* **로깅 & 물리 컨텍스트**: 로그는 `Debug.LogWarning("Msg", this)`로 컨텍스트 포함. 모든 물리 수치 변경은 `FixedUpdate()`에서 `Time.fixedDeltaTime`을 활용.
* 물리 수치·레이어/색상 매칭(enum `ColorType`)·세이브 스키마 등 구현 상세는 **TDD §4·§5·§10** 참조.

---

## 5. Fast Retry & 상태 관리 (State Management)

* 실패(Game Over) 시 `SceneManager.LoadScene()` **절대 금지.**
* `GameManager`는 `enum GameState { Play, Pause, GameOver, Clear }` 기반 FSM을 가진다.
* 초기 `Transform`(Position/Rotation)과 물리 상태를 `Awake()` / `Start()`에서 캐싱해두고, 재시작 시 이 데이터를 즉시 덮어쓴다.
* 위치 대입 시 **`linearVelocity`, `angularVelocity`를 반드시 `0`으로 초기화** (벽 뚫기 방지). Jitter 방지를 위해 Transform 대신 `_rb.position` / `_rb.rotation`을 직접 할당한다. (단, 골인으로 비활성화된 공은 `_rb.position`이 무시되므로 `transform` 이동 후 `SetActive(true)`.)
* **초기화 범위(누락 주의)**: 공·중력/시점뿐 아니라 **동적 기믹 상태(문/파괴블록/스위치)·풀 오브젝트·골인으로 비활성화된 공의 재활성화**까지 모두 되돌린다. **승/패 동시 발생은 패배 우선.** → 전체 범위·구현 한계: **TDD §7**.

---

## 6. 피드백 & UI (Feedback & UI)

* **충돌 피드백 쿨타임**: SFX/Haptic은 `Time.time` 기반 **0.1초 내부 쿨타임**을 강제한다 (연속 충돌 시 모터 폭주/사운드 깨짐 방지).
* **풀링(Pooling) — 예정(Task Phase 14, 현재 미구현)**: 파티클·SFX는 `Instantiate/Destroy` 대신 `PoolManager`로 활성화/비활성화 (GC 부하 ↓, 60FPS 유지).
* **해상도**: 기준 `1080 × 1920`, `Canvas Scaler` = `Scale With Screen Size` (Match `0.5`). 카메라는 미로의 **대각선(외접원 반지름)** 기준으로 `orthographicSize`를 설정하여 회전 시 모서리 클리핑 방지.
* **Safe Area**: 최상단 UI 패널에 노치(Notch) 대응 스크립트를 부착한다.
* 쿨타임·풀링·해상도 보정 수식 등 구현 상세: **TDD §8~9**.

---

## 7. 동적 기믹 물리 주의 (Phase 4+ 확장 시)

* 문(Door), 파괴 블록 등 **런타임에 상태가 변하거나 파괴되는 동적 기믹은 절대 `CompositeCollider2D`에 병합(`Used By Composite`)하지 마라.** 독립 `BoxCollider2D` 등을 사용해 런타임 콜라이더 Rebuild로 인한 렉(Lag Spike)을 방지한다.
* 정적 지형(Tilemap/SpriteShape/PolygonCollider)만 최상위 `Maze` 오브젝트의 `CompositeCollider2D`에 병합(`Used By Composite` 체크)하여 단일 `Physics Material 2D`로 일괄 제어한다.
* 레벨 오소링·병합 파이프라인 상세: **TDD §6**. 동적 기믹 추가 시 Fast Retry 초기화(TDD §7.5)에 해당 기믹 상태 복원을 반드시 포함한다.