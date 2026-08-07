# 프로젝트 작업 목록 (Task List)

> 기획 근거는 `GDD.md`(무엇/왜), 구현 스펙은 `TDD.md`(어떻게), 작업 규칙·아키텍처는 `CLAUDE.md`.
> **현재 위치: Phase 7 (인게임 UI) 진행 중.**
> 최종 재작성: 2026-08-07 (구조 전면 개편 — 아래 §개편 배경 참조)

---

## 📐 문서 사용 규칙 (필독)

### 완료 기준 (Definition of Done)

1. **모든 항목은 태그를 하나 갖는다.** 태그 없는 항목은 추가하지 않는다.
2. **`[x]`는 "MCP 실측 또는 플레이로 확인됨"일 때만 찍는다.** 코드만 작성한 상태·에셋만 만들어둔 상태는 `[ ]` 유지.
3. **코드 작성과 씬/프리팹 배선은 반드시 별도 항목으로 쪼갠다.**
   → Unity의 최빈 실패 모드는 "코드는 맞는데 인스펙터가 비어 있음"이다. 한 줄에 뭉치면 구조적으로 잡을 수 없다.
4. **`[QA]` 항목이 하나도 없는 Phase는 닫지 않는다.**
5. **에셋/오브젝트를 "정의"한 것과 "할당·배치"한 것은 다른 항목이다.**
   → 레이어를 만들었지만 아무 오브젝트에도 할당하지 않은 채 완료 처리한 사고(§개편 배경 ①)의 재발 방지.
6. **TDD/GDD와 상태가 어긋나면 `TDD.md`가 우선**이며, 발견 즉시 양쪽을 맞추고 `[Doc]` 항목으로 남긴다.

### 태그

| 태그 | 의미 |
|---|---|
| `[Code]` | 스크립트 작성·수정 |
| `[Editor]` | 씬 · 프리팹 · ProjectSettings 등 에디터 데이터 |
| `[Asset]` | 아트 · 오디오 · 폰트 등 외부 리소스 확보 |
| `[QA]` | 실측 · 플레이 검증 |
| `[Doc]` | 문서 갱신 · 설계 결정 기록 |

### Phase 헤더 규약

각 Phase는 **선행**(들어가기 전 만족해야 할 조건)과 **완료 조건**(관측 가능한 종료 상태)을 헤더에 명시한다.
의존성을 사후에 인라인 노트로 붙이지 않는다.

---

## 🔄 개편 배경 (2026-08-07)

구 문서를 실측 점검한 결과 **완료 표기와 실제 상태가 어긋난 항목 3건**이 발견되었고, 세 건 모두 원인이 같았다 — *완료 기준의 부재*.

| # | 사고 | 실측 근거 |
|---|---|---|
| ① | **색상 게이트(USP) 미구현인데 완료 표기** | 색 레이어 4종이 정의만 되고 어떤 오브젝트에도 미할당(전부 `Default(0)`), 씬·프리팹에 Gate 오브젝트 0개. `TDD.md §5.2`는 "미구현, 게이트 구현 시 배선"이라 정확히 기록해 두었으나 Task.md만 닫혀 있었다 |
| ② | **Layer Collision Matrix가 스펙과 정반대** | 꺼져 있던 유일한 두 칸이 `Gate_X × Gate_X`(정적끼리라 무의미), 반드시 꺼야 할 `Ball_X × Gate_X`는 켜져 있었다 → **2026-08-07 수정 완료** (커밋 `d23ec99`) |
| ③ | **`FeedbackManager`가 데드코드인데 완료 표기** | 호출처 0건, 코드베이스 전체에 `AudioSource`/`AudioClip`/`ParticleSystem` 참조 0건 |

추가로 **GDD가 요구하는데 Task에 항목 자체가 없던 워크스트림**(색상 게이트 구현 · 동적 기믹 · 레벨 콘텐츠 · FTUE · 씬 플로우 · 오디오 에셋 확보)을 신설했다. 구 계획은 팔 물건(레벨)이 없는 상태에서 상점부터 만들게 되어 있었다 — GDD §11.1의 MVP 순서와 어긋난다.

### 구 번호 ↔ 신 번호 매핑

`DevelopLog.md` · `TDD.md`의 과거 서술은 구 번호를 쓴다. 대조용.

| 구 | 신 | 비고 |
|---|---|---|
| Phase 1 | **Phase 1** | 그대로 |
| Phase 1.5 | **Phase 2** | 소수점 제거 |
| Phase 2 | **Phase 3** | |
| Phase 3 + 3.5 | **Phase 4** | 색상 매칭 기반으로 통합 |
| Phase 4 | **Phase 5** | 레벨 오소링 파이프라인 |
| Phase 5.0 | **Phase 6** | UI 기반 셋업 |
| Phase 5.1 | **Phase 7** | 인게임 UI |
| Phase 5 (메타) | **Phase 11~12** | 세이브·로비 / 경제·상점으로 분리 |
| Phase 6 | **Phase 13** | 오디오·VFX |
| Phase 7 | **Phase 14** | 최적화·출시 |
| *(신설)* | **Phase 8** | 색상 게이트 (USP) |
| *(신설)* | **Phase 9** | 동적 기믹 |
| *(신설)* | **Phase 10** | 레벨 콘텐츠 · 난이도 · FTUE |

---
---

# Part I. 완료된 기반 (Phase 1~6)

## Phase 1: 기본 물리 및 조작 프로토타입 ✅
> **선행**: 없음 **완료 조건**: 공이 중력으로 굴러가고 드래그로 기울일 수 있다

- [x] `[Code]` **프로젝트 세팅**: 타겟 프레임 60 · 화면 꺼짐 방지 (`Bootstrapper.cs`)
- [x] `[Editor]` **환경 구성**: 해상도 대응 Orthographic 카메라 + Tilemap 뼈대
- [x] `[Editor]` **Ball 프리팹**: Dynamic RB2D · `Continuous` · `Interpolate` — 2026-08-07 실측 확인
- [x] `[Code]` **드래그 회전 입력**: `Mathf.Atan2` 각도 기반, 스냅 방지 수식 적용
- [x] `[Doc]` **저장소 최적화**: Unity 6 표준 `.gitignore`

## Phase 2: 물리 아키텍처 — B방식 전환 ✅ *(구 Phase 1.5)*
> **선행**: Phase 1 **완료 조건**: 빠른 회전에도 공이 튀지 않고, 중력이 항상 화면 아래를 향한다
>
> **배경**: Kinematic 미로 회전 시 Box2D 표면 속도 전달로 공이 의도치 않게 튀어오르는 버그.
> **해결**: 미로는 Transform(0,0,0) 고정 · 카메라 역회전 · `Physics2D.gravity` 동기화.

- [x] `[Editor]` Maze 루트 `Rigidbody2D` = **Static** (Composite 유지 위해 컴포넌트 존치)
- [x] `[Code]` `MazeRotator` → `WorldRotationController` 리팩토링 (`MoveRotation()` 삭제, `FixedUpdate` 각도 보간 → 중력 갱신)
- [x] `[Code]` `CameraController.LateUpdate()`에서 `-angle` 역회전 (Jittering 방지)
- [x] `[Code]` `InputController` 참조 전환 + `[SerializeField]` 인스펙터 연결
- [x] `[QA]` 빠른 회전 시 공 튀어오름 없음 · 중력 방향이 화면 "아래"와 일치 확인
- [x] `[QA]` 2026-08-07 재검증: Maze `pos=(0,0,0) rot=0`, RB2D `Static`, Composite `shapeCount=21`

## Phase 3: 인게임 로직 & Fast Retry ⚠️ *(구 Phase 2 — 일부 미완)*
> **선행**: Phase 2 **완료 조건**: 골인/실패가 판정되고, 실패 시 씬 재로드 없이 즉시 복원된다

- [x] `[Code]` **Goal 판정**: `OnCollisionEnter2D` + `ColorType` 일치 시에만 골인, 불일치는 물리 벽
      *(구 문서의 "트리거 구현" 표현은 오기 — 오답=벽 요구(GDD §3.4) 때문에 non-trigger가 옳다)*
- [x] `[Code]` **장애물 / DeadZone**: `Obstacle`(범용) + 장외 이탈 `DeadZone` 트리거 → 실패 판정
- [x] `[Code]` **Fast Retry**: `LoadScene` 없이 Transform·`linearVelocity`·`angularVelocity`·중력·시점 복원
- [x] `[QA]` 2026-08-07 배선 실측: `GameManager`의 3개 참조 + `InputController`·`WorldRotationController` 참조 전부 non-null
- [ ] `[Code]` **⚠️ 피드백 배선** — *구 문서에서 완료로 잘못 표기됨(개편 배경 ③)*
      실체는 쿨타임 가드가 든 `PlayHaptic()` 스텁 하나뿐이고 **호출처가 0건**이다.
      - [ ] `[Code]` `PlayerBall` 충돌 → `FeedbackManager.PlayHaptic()` 호출 배선
      - [ ] `[Code]` 충돌 상대속도(충격량)에 비례한 세기 스케일 (TDD §8) — 현재 `intensity` 인자를 받아만 두고 쓰지 않음
      - [ ] `[Code]` `Debug.LogWarning` 상시 출력 제거 (`FeedbackManager.cs:30`)
      - → SFX/파티클은 에셋이 없어 **Phase 13**으로 분리
- [ ] `[Doc]` **승/패 동시 발생 우선순위 결정** (GDD §3.3 = 패배 우선)
      `TDD.md §7.6`이 현 구현을 *first-event-wins(콜백 순서 비결정적)* 한계로 기록해 두었으나 **Task에 결정 항목이 없었다.**
      엄격 적용(`WaitForFixedUpdate` 보류 후 확정)을 할지 현행 유지할지 판단하고 기록할 것.

## Phase 4: 색상 매칭 기반 & 공-공 충돌 정책 ⚠️ *(구 Phase 3 + 3.5)*
> **선행**: Phase 3 **완료 조건**: 색이 맞는 Goal에만 골인하고, 모든 공이 서로 충돌한다
>
> ⚠️ **색상 게이트는 이 Phase에 포함되지 않는다 → Phase 8.** 구 문서가 이 둘을 한 항목에 뭉쳐 사고 ①을 만들었다.

- [x] `[Code]` `enum ColorType { Default, Red, Blue, Green, Yellow }` 정의 (`Goal.cs`)
- [x] `[Code]` `PlayerBall.ColorType` ↔ `Goal._goalColorType` 매칭 판정
- [x] `[Code]` 멀티볼 승리 카운터 (`_reachedBallsCount >= _totalBallsCount`)
- [x] `[Editor]` **색 레이어 4종 "정의"**: `Ball_Yellow(6)` `Ball_Green(7)` `Gate_Yellow(8)` `Gate_Green(9)`
      ⚠️ **정의만 완료. 오브젝트 할당은 Phase 8** — 현재 공·Goal 전부 `Default(0)` (TDD §5.2:114와 일치)
- [x] `[Editor]` **공-공 충돌 Matrix**: 전 교차 ON — 2026-07-01 검증(밀림 0.30→0.595), 2026-08-07 재확인
- [x] `[QA]` 씬 실측: `PlayerBall` 2개(color=Yellow/Green), `Goal` 2개(color=Yellow/Green), 색 일치 골인 동작
- [ ] `[QA]` **좁은 통로 교착(끼임) 플레이테스트** — *구 문서는 `[x]`였으나 주석에 "인터랙티브 플레이테스트 필요"라 적혀 있었다.* 새 DoD 기준 미완으로 되돌림 (GDD §5.3 레벨 설계 유의)

## Phase 5: 레벨 오소링 파이프라인 ✅ *(구 Phase 4)*
> **선행**: Phase 2 **완료 조건**: 곡선·비정형 지형을 표준 절차로 만들어 Composite에 병합할 수 있다

- [x] `[Code]` `StageData` ScriptableObject (제한 시간 · 별점 기준)
- [x] `[Editor]` `Assets/Data/Stage 1.asset` 생성 + `GameManager._currentStageData` 배선
      → 다중 스테이지 레지스트리 / `StageLoader`는 **Phase 11**로 이월
- [x] `[Editor]` 2D SpriteShape 13.0.0 도입, 프로필을 `Assets/SpriteShapes/`로 정규화
- [x] `[QA]` **동적 기믹 독립 콜라이더 워크플로우 검증** (2026-07-30): `Used By Composite` 미사용 시 Composite `shapeCount` 불변 확인, **자체 Kinematic `Rigidbody2D` 필수** 조건 발견 (TDD §6.2)
- [x] `[Editor]` **곡선 벽 실제 저작** (2026-08-06): `Collider Offset = 0.5`로 시각/충돌 정렬, `Assets/Prefabs/RoundWall_0.prefab` 프리팹화
- [x] `[QA]` **Composite 병합 실측**: `MazeGrid/Maze/Wall_SpriteShape_01` — `shapeCount` 15→16, `pathCount` 2→3, `pointCount` 32→37
- [ ] `[Editor]` **⚠️ 씬 카메라 `orthographicSize` 정정**: 컴포넌트 저장값이 `6`인데 런타임은 `Awake`가 `defaultOrthoSize = 10.5`로 덮어쓴다.
      → **에디터에서 플레이어보다 75% 좁은 화면을 보고 레벨을 만들게 된다.** `TDD.md:265`가 `6`을 "과거 오설정"으로 명시. 레벨 양산(Phase 10) 전 반드시 정리.

## Phase 6: UI 기반 셋업 ✅ *(구 Phase 5.0)*
> **선행**: Phase 3 **완료 조건**: 화면에 UI를 띄우고 클릭을 받을 수 있다

- [x] `[Editor]` `UICanvas` 신설: `Screen Space - Overlay` · Scale With Screen Size · `1080×1920` · Match `0.5` · Layer `UI`
- [x] `[Editor]` `EventSystem` 활성화 (기존 `activeSelf = false`)
- [x] `[Editor]` 입력 모듈 교체: 레거시 `StandaloneInputModule` → `InputSystemUIInputModule`
      *`activeInputHandler = 1`(New 전용)이라 교체 없이 활성화했다면 런타임 예외가 났을 상태*
- [x] `[Code]` **⚠️ [Bug] `InputController` 터치 UI 무시 경로 수정** (2026-08-06):
      `IsPointerOverGameObject()` 의존 제거 → `IsPointerOverUI(screenPos)`(캐시된 `PointerEventData` + `RaycastAll`).
      구 코드 결함 ① `finger.index`를 넘겨 매칭 자체가 불가, ② `IsPointerOverGameObject`는 `EventSystem.Update()` 결과를 읽는데 `EventSystem`에 실행 순서 지정이 없어 터치 시작 프레임에 상태가 비어 있을 수 있음.
- [x] `[QA]` 플레이 모드 검증: `currentInputModule` 확인, `RaycastAll` 중앙 1히트 / 모서리 0히트, GC ≈ 0.8 B/call
- [x] `[Editor]` TMP Essential Resources 임포트 (2026-08-07) — `Assets/TextMesh Pro/` 확인
- [ ] `[QA]` **실기기 터치 검증** → **Phase 14**로 이관 (에디터가 Game View 포커스 없이 터치를 폐기해 인게임 시뮬레이션 불가)

---
---

# Part II. 앞으로 (Phase 7~14)

> **순서 원칙** (GDD §11.1 MVP): 코어 루프 → **레벨 콘텐츠** → 메타/경제 → 폴리싱.
> 팔 물건(레벨)이 없는 상태에서 상점부터 만들지 않는다.

## Phase 7: 인게임 UI — HUD · 결과 · 일시정지 ← **현재**
> **선행**: Phase 6 완료 (Canvas · EventSystem · TMP)
> **완료 조건**: 클리어/실패가 UI로 표시되고, 유저가 버튼으로 재시작·일시정지할 수 있다

- [ ] `[Code]` **`GameManager` 상태 이벤트 노출**: `event Action<GameState>` + `OnDisable`/`OnDestroy` 해제 (CLAUDE.md §4)
- [ ] `[Code]` **`GameState.Pause` 실구현** — 현재 enum에 선언만 되고 set/read 하는 코드가 0건이라 GDD §8.1의 `[인게임] ⇄ [일시정지]`가 성립 불가
- [ ] `[Code]` **UI 주도 플로우로 전환**: `GameOver()`의 즉시 `FastRetry()`(`GameManager.cs:75`)와 `ClearDelayRoutine()`의 자동 `FastRetry()`(`:85`)를 제거하고 UI 버튼이 호출하도록
      ※ 전환 중 재시작 수단이 사라지는 공백이 생기므로, 팝업 완성 전까지 임시 트리거를 유지할 것
- [ ] `[Code]` HUD: 타이머 · 일시정지 버튼 (GDD §8.2 — 최소 UI로 몰입 유지)
- [ ] `[Code]` 결과 팝업: 최종 타임 · 별 1~3 · `[재시작]` `[다음]` `[로비]` (`[다음]`/`[로비]`는 Phase 11에서 활성)
- [ ] `[Code]` `CalculateStars()` 결과를 `Debug.LogWarning`(`:81`) 대신 UI로 전달
- [ ] `[Editor]` HUD · 결과 팝업 · 일시정지 팝업 프리팹 제작 및 `UICanvas` 배치
- [ ] `[Editor]` 각 UI 컴포넌트 ↔ 스크립트 인스펙터 배선
- [ ] `[Code]` **Safe Area 대응** 스크립트 (CLAUDE.md §6)
- [ ] `[Editor]` Safe Area 스크립트를 최상단 UI 패널에 부착
- [ ] `[QA]` 클리어/실패 → 팝업 표시 → 버튼 재시작까지 플레이 검증, 콘솔 에러 0건
- [ ] `[QA]` 일시정지 중 타이머·물리가 실제로 멈추는지 검증

## Phase 8: 색상 게이트 (USP) 🔴 **최우선 게임플레이**
> **선행**: Phase 4 **완료 조건**: 같은 색 공만 게이트를 통과하고 다른 색은 튕겨 나온다
>
> GDD §1이 **"주요 차별점(USP)"**, §11.1이 MVP 3단계로 규정한 기믹. 구 문서에서 완료로 잘못 닫혀 누락돼 있었다(개편 배경 ①).

- [x] `[Editor]` **Layer Collision Matrix 배선** (2026-08-07, 커밋 `d23ec99`)
      `Ball_X × Gate_X` = 무시(통과) / `Ball_X × Gate_Y` = 충돌(차단) / 공-공 전 교차 ON 유지
- [x] `[QA]` 저장된 `m_LayerCollisionMatrix` 비트 직접 디코딩 검증 (런타임 API 반환값이 아닌 파일 실측)
- [ ] `[Editor]` **공 오브젝트를 색 레이어로 이동**: `PlayerBall`(Yellow) → `Ball_Yellow(6)`, `PlayerBall (1)`(Green) → `Ball_Green(7)`
      ⚠️ 프리팹 단위로 처리할지 인스턴스 단위로 할지 먼저 결정 (색이 늘면 프리팹 배리언트가 유리)
- [ ] `[Editor]` **Gate 오브젝트/프리팹 제작** — 현재 씬·프리팹에 0개. 콜라이더 + `Gate_X` 레이어. 스크립트 불필요(TDD §5.2: 통과/차단은 코드가 아닌 레이어)
- [ ] `[Editor]` **Gate 비주얼**: GDD §5.1.1 필수 조건 — *통과 가능해 보이는* 형태(에너지 장막 / 반투명 커튼 / 뚫린 색 프레임). ❌ 불투명 색 블록은 "벽"으로 오독됨
- [ ] `[Editor]` Gate는 동적 기믹이 아니므로 Composite 병합 여부 판단 (정적이면 병합 가능, TDD §6.2)
- [ ] `[QA]` 같은 색 통과 / 다른 색 차단 플레이 검증
- [ ] `[QA]` 공-공 충돌이 레이어 이동 후에도 보존되는지 재검증 (TDD §5.2:120 경고)
- [ ] `[Doc]` 색 확장 시 레이어 예산 점검 (공4+게이트4=8, 6~7색 초과 시 `IgnoreCollision` 폴백 — TDD §5.2:113)

## Phase 9: 동적 기믹 (Switch/Door · Fragile · Bumper)
> **선행**: Phase 8 **완료 조건**: 런타임에 상태가 변하는 기믹이 동작하고, Fast Retry가 그 상태까지 되돌린다
>
> CLAUDE.md §7과 TDD §6.2가 한 절씩 할애한 주제인데 **구 Task.md에 구현 항목이 하나도 없었다.** Phase 5에서 워크플로우만 검증하고 실물은 만들지 않은 채 Phase가 닫혔다.

- [ ] `[Code]` `Switch` + `Door` (스위치를 친 뒤 막힌 경로가 열림 — GDD §6.1)
- [ ] `[Code]` `FragileBlock` (일정 속도 이상 충돌 시 파괴)
- [ ] `[Editor]` `Bumper` (강한 반발 — `Physics Material 2D`로 처리, 코드 금지 CLAUDE.md §4)
- [ ] `[Editor]` **동적 기믹은 `Used By Composite` 금지 + 자체 Kinematic `Rigidbody2D` 필수** (TDD §6.2 실측 조건)
- [ ] `[Code]` **Fast Retry 초기화 범위 확장** (GDD §3.5 / TDD §7.5): 열린 문 · 부서진 블록 · 눌린 스위치 상태 복원
- [ ] `[QA]` 기믹 상태가 재시작 시 100% 복원되는지 검증
- [ ] `[QA]` 런타임 콜라이더 Rebuild로 인한 렉 스파이크가 없는지 프로파일 확인 (CLAUDE.md §7)

## Phase 10: 레벨 콘텐츠 · 난이도 곡선 · FTUE
> **선행**: Phase 8, 9 (레벨에 쓸 기믹이 갖춰져야 함) + Phase 5의 카메라 정정
> **완료 조건**: 1개 월드 분량의 플레이 가능한 레벨이 있고, 새 유저가 설명 없이 첫 판을 깬다
>
> GDD §11.1 MVP 2단계. **구 Task.md에 레벨 제작 항목이 아예 없었다** — 현재 스테이지 1개뿐.

- [ ] `[Code]` **`GameSettings` ScriptableObject** (CLAUDE.md §4 요구사항, 현재 부재)
      회전 보간·각속도 상한·중력 배율이 `WorldRotationController.cs:12-16` SerializeField에 산재 → 난이도 튜닝과 모션 완화(Phase 14)의 공통 참조점
- [ ] `[Doc]` **메커닉 도입 스케줄 표** (GDD §6.3 `[TBD]`): 한 번에 하나씩 — 단일 공 → 색 게이트 → 멀티볼 → 동적 기믹 조합
- [ ] `[Editor]` **1개 월드 분량 레벨 제작** (GDD §6.2 잠정 10~15레벨, 확정 아님)
- [ ] `[Editor]` 레벨별 `StageData` 에셋 생성 + 별점 기준 시간 튜닝
- [ ] `[Code]` **FTUE / 튜토리얼** (GDD §6.4): 무텍스트 지향, 첫 1~3레벨에서 행동으로 학습, 새 기믹 첫 등장 시 1회 비차단 툴팁
- [ ] `[QA]` 각 레벨 클리어 가능성 검증 + 별 3개 기준 시간이 달성 가능한지 실측
- [ ] `[QA]` GDD §5.4 멀티볼 원칙(단일 해법의 긴장 / 게이트 분기 / 순서 설계 / 안전 마진) 대조 리뷰

## Phase 11: 세이브 · 씬 플로우 · 로비/월드맵
> **선행**: Phase 10 (표시할 레벨과 별 기록이 있어야 로비가 성립)
> **완료 조건**: 게임을 껐다 켜도 진행이 남고, 로비에서 레벨을 골라 들어갔다 나올 수 있다

- [ ] `[Code]` **세이브 시스템**: 별 기록 · 클리어 타임 · 해금 · 화폐 · 스킨을 로컬 저장. **스키마 버전 필드 + 체크섬/간단 암호화** 포함 (GDD §7.5, TDD §10)
- [ ] `[Code]` **스테이지 레지스트리 / `StageLoader`** (Phase 5에서 이월)
- [ ] `[Editor]` **씬 정리**: `SampleScene` → 의미 있는 이름으로 변경, `Lobby` 씬 신설, **Build Settings 등록** (현재 씬 1개만 등록)
- [ ] `[Code]` 씬 전환 플로우 (GDD §8.1): `Splash → 로비/월드맵 → 스테이지 선택 → 인게임 → 결과 → (다음/재시작/로비)`
- [ ] `[Code]` **로비 / 월드맵**: 스테이지 선택 UI, 누적 별·달성도 표시 (GDD §8.2)
- [ ] `[Code]` 결과 팝업의 `[다음]` `[로비]` 버튼 활성화 (Phase 7에서 껍데기만 만들어둔 것)
- [ ] `[Code]` **환경설정 팝업**: 사운드 볼륨 · 햅틱 ON/OFF · 색약 모드 · 모션 완화 토글
- [ ] `[QA]` 앱 재시작 후 별 기록·해금이 보존되는지 검증
- [ ] `[QA]` 세이브 파일 손상/구버전 스키마 시 크래시 없이 복구되는지 검증

## Phase 12: 경제 · 상점 · 광고
> **선행**: Phase 11 (세이브 없이는 소유 개념이 성립하지 않음)
> **완료 조건**: 화폐를 벌어 코스메틱을 사고, 그 상태가 저장된다

- [ ] `[Code]` **별/화폐 분리**: 성취 기록용 **별(불변)** ↔ 소비용 **소프트 화폐**를 별도 필드로 (GDD §7.1~7.2 — 별을 소모하면 "광고로 등급을 사는" 모순 발생)
- [ ] `[Code]` 화폐 획득 경로: 레벨 클리어 보상 등
- [ ] `[Code]` **상점 & 스킨**: 공 Trail · 골인 파티클 · 미로 테마 해금 (순수 코스메틱, 능력 영향 없음 — GDD §7.3)
- [ ] `[Asset]` 스킨 리소스 제작
- [ ] `[Code]` **리워드 광고**: 강제 전면광고 지양, "화폐 부족 시 시청 → **화폐**(별 아님) 획득" (GDD §7.4)
- [ ] `[QA]` 구매 → 저장 → 재시작 후 보유 유지 검증

## Phase 13: 오디오 · VFX 폴리싱
> **선행**: Phase 10 (폴리싱 대상 콘텐츠 존재) + **오디오 에셋 확보**
> **완료 조건**: 충돌·골인·BGM이 들리고, 골인에 파티클이 뜬다

- [ ] `[Asset]` **오디오 소스 확보** — 소스가 없으면 착수 불가. BGM 톤·레퍼런스는 GDD §9 `[TBD]`
- [ ] `[Code]` **SFX 재생 배선**: 충돌음 · 성공음 (Phase 3의 피드백 미완분 회수, 쿨타임 0.1초 공유 — TDD §8)
- [ ] `[Code]` 충돌 상대속도에 비례한 SFX 볼륨 · 햅틱 세기 스케일
- [ ] `[Editor]` **골인 파티클** (GDD §3.1 "파티클과 함께 즉시 사라진다" — 현재 미구현)
- [ ] `[Editor]` 공 Trail Renderer 보강
- [ ] `[Code]` BGM 루프 + 회전/속도에 따른 미묘한 변화
- [ ] `[QA]` 연속 충돌 시 진동 모터 폭주 / 사운드 깨짐이 없는지 검증

## Phase 14: 최적화 · 접근성 · 출시
> **선행**: Phase 13 **완료 조건**: 최저 사양 기기에서 60FPS로 돌고, 접근성 옵션이 동작한다

- [ ] `[Code]` **오브젝트 풀링(`PoolManager`)**: 파티클·SFX를 `Instantiate/Destroy` 대신 풀 재사용 (TDD §8). 도입 시 Fast Retry(§7.5)의 "풀 반환"과 연결
- [ ] `[Code]` **`Debug.Log` 스트립**: 릴리스 빌드에서 제거 — 현재 5곳 상시 출력 (`Bootstrapper:13`, `CameraController:65`(보간 문자열), `GameManager:81`, `DeadZone:11`, `FeedbackManager:30`)
- [ ] `[QA]` **프로파일링**: GC 튜닝 · 메모리 누수 점검
- [ ] `[QA]` **실기기 터치 테스트**: 조작감 튜닝, **UI 버튼 위 터치 시 미로가 회전하지 않는지 확인**(Phase 6 수정분 — 에디터에서 검증 불가), Safe Area 노치 실물 확인
- [ ] `[Asset]` **색맹 대응 문양 각인** (GDD §8.3): 공·Goal·게이트에 색+모양 이중 전달 (예: Red=별, Blue=세모). 현재 토글만 계획돼 있고 문양 에셋 작업이 누락돼 있었다
- [ ] `[Code]` **모션 완화(Reduce Motion)** 실동작: 회전 감속/폭 축소 — GDD §11.2가 **회전 멀미를 최상위 리스크**로 지목, "조기 프로토타입·실기 테스트" 권고
- [ ] `[Code]` **분석/KPI 연동** (GDD §10): `level_start` `level_clear(time,stars)` `level_fail(reason)` `retry_count` `rewarded_ad_view` `skin_purchase`
- [ ] `[Doc]` **최소 기기 사양 확정 & 성능 예산**: 60FPS · 메모리 · 드로우콜 목표 (TDD §1)
- [ ] `[Code]` 현지화: 한국어/영어, 텍스트 외부 테이블 분리 (GDD §8.4)

---

## 📌 문서 정합성 미결 (즉시 처리 가능)

- [ ] `[Doc]` `TDD.md` 스테일 참조 3건 정정: PoolManager "Task Phase 7"→**14**, BGM "Task Phase 6"→**13**, Safe Area "Task Phase 7"→**7**
- [ ] `[Doc]` `TDD.md §11` 모듈 현황표의 `FeedbackManager` 상태를 `✅` → `⚠️ 스텁(호출처 0건)`으로 정정
- [ ] `[Doc]` `GDD.md` 헤더 "현재 진척: Phase 1~3 구현 완료" 갱신

## 🔮 추후 결정 (Deferred)

| 항목 | 결정 시점 |
|---|---|
| 콘텐츠 총량(월드/레벨 수) — 데이터 주도 점진 확장 (GDD §6.2) | Phase 10 |
| 최소 지원 기기 사양 / 메모리 예산 (TDD §1) | Phase 14 |
| 지원 언어 & 현지화 범위 (GDD §8.4) | Phase 14 |
| BGM 톤 · 레퍼런스 (GDD §9) | Phase 13 |
| 레퍼런스 경쟁작 벤치마크 (GDD §1) | 상시 |
| 추가 IAP(광고 제거/화폐 팩) (GDD §7.4) | Phase 12 |
| "같은 색 **차단**" 역방향 게이트 도입 여부 (GDD §5.1.1 후순위 도구) | Phase 10 |
