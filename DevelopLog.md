# Development Log (개발 일지)

이 문서는 프로젝트의 개발 타임라인, 주요 변경 사항, 마주친 버그 및 해결 방법(Troubleshooting)을 기록하는 공간입니다. 

## [작성 규칙]
1. 나(AI)는 특정 Task(예: Phase 1)가 완료되거나 중요한 버그를 수정했을 때, 이 문서의 **최상단(가장 위)**에 새로운 로그를 추가합니다.
2. 각 로그는 **날짜(YYYY-MM-DD), 제목, 내용, 해결된 이슈** 포맷을 포함해야 합니다.

---

### 📅 [2026-08-07] Phase 7 — HUD(타이머·일시정지) 구현 및 **GC 측정기 신뢰 불가 판명**

* **작업 배경**: 직전에 만든 `GameManager.OnStateChanged`를 소비하는 첫 구독자로 HUD를 붙였다. 가장 단순한 구독자라 배선이 맞는지 즉시 드러난다.

#### 1. 구성

`Assets/Scripts/UI/HUDController.cs` 신설 + 씬 배치:

```
UICanvas
├── HUD          (Graphic 없음 → 레이캐스트 투명)
│   ├── TimerText    (TMP, raycastTarget = false)
│   └── PauseButton
└── PausePanel   (dim Image, raycastTarget = true, 기본 비활성)
    ├── Title / ResumeButton / RetryButton
```

* **`raycastTarget = false`가 핵심**: HUD가 전체화면을 덮는데 `TimerText`가 레이캐스트를 먹으면 `InputController.IsPointerOverUI`가 `true`를 반환해 **화면 상단에서 시작한 미로 드래그가 통째로 죽는다.** 실측으로 화면 중앙 판정 `False`(통과) / 일시정지 버튼 위 `True`(차단) 확인.
* **타이머 갱신 게이팅**: 소수 첫째 자리가 바뀔 때만 `SetText`. 값이 같아도 호출하면 TMP 메시가 재생성되므로 60fps에서 50/60이 낭비된다.
* **버튼 콜백은 코드 `AddListener`**: 인스펙터 persistent listener는 배선이 빠져도 **콘솔 에러 없이 조용히 통과**한다 — 이 프로젝트가 반복해서 당한 실패 유형이라 코드 연결 + `OnDestroy`에서 `RemoveListener`.
* 인스펙터 참조 5건 전부 non-null 실측 확인.

#### 2. ⚠️ GC 측정기가 신뢰 불가임이 드러났다 (이 세션의 가장 중요한 발견)

`SetText(format, arg)`가 정말 무할당인지 확인하려다 **대조군을 넣은 덕분에** 측정 자체가 깨져 있음을 발견했다.

| 방법 | 대조군 `float.ToString("F1")` (확실히 할당) | 결론 |
|---|---|---|
| `GC.GetTotalMemory` | 5000회 → **0 bytes** | ❌ 신뢰 불가 |
| `GC.GetAllocatedBytesForCurrentThread` | 5000회 → **0 bytes** | ❌ 신뢰 불가 |
| `Profiler.GetMonoUsedSizeLong` | 20000회 → **0 bytes** | ❌ 신뢰 불가 |

세 번째 방법은 `SetText` 20000회에 532,480 bytes를 보고했지만, **대조군이 0으로 나오는 측정기의 숫자는 채택할 수 없다.** (게다가 프레임이 안 흐르는 상태에서 `SetText`를 2만 번 하면 캔버스 리빌드 큐가 계속 쌓여 그 증가분일 가능성이 높다.)

* **소급 정정**: 같은 날 앞선 로그의 *"상태 전이 2000회 실측 0 bytes"* 도 `GetTotalMemory` 기반이었으므로 **철회**하고, `Task.md`와 커밋 메시지의 해당 표현도 근거 없는 주장으로 표시했다. `Action<GameState>`가 박싱하지 않는 것은 제네릭 델리게이트의 런타임 특성이지 측정 결과가 아니다.
* **교훈**: 측정에는 반드시 **양쪽 대조군**(확실히 할당 / 확실히 무할당)을 붙인다. 대조군 없이 "0 bytes"를 본 것은 측정이 아니라 착시다. → 실제 GC 수치는 Phase 14에서 실기기 Profiler로 확인.

#### 3. 판정하지 못한 것 — 일시정지 패널의 레이캐스트 차단

패널을 켠 **직후 같은 프레임**에 레이캐스트하면 `Image.depth == -1`이라 `GraphicRaycaster`가 그 Graphic을 건너뛴다(캔버스 리빌드 전이라 depth 미할당). 프레임을 넘겨 재확인하려 했으나 에디터가 백그라운드에서 플레이어 루프를 돌리지 않아 `Time.frameCount`가 `520`에 고정돼 리빌드가 일어나지 않았다.

→ 다만 **회전 차단의 실제 보증은 레이캐스트가 아니라 `SetInputEnabled(false)`** 이고, 그쪽은 실측으로 `_inputEnabled = False`를 확인했다. `Update()`가 즉시 return하므로 레이캐스트 결과와 무관하게 드래그가 불가능하다. 패널 dim의 레이캐스트는 이중 안전장치.

* **해결된 이슈**:
  * 상태 이벤트를 소비하는 첫 구독자 확보 — 배선 경로가 실제로 동작함을 확인
  * HUD가 전체화면을 덮으면서도 미로 조작을 막지 않는 레이캐스트 구성 확립
  * **GC 측정 결과를 근거 없이 신뢰하던 문제** — 대조군 없는 측정을 채택했던 앞선 기록 정정

---

### 📅 [2026-08-07] Phase 7 — `GameManager` 상태 이벤트 + `GameState.Pause` 구현 (방침 (b))

* **작업 배경**: UI 팝업이 낄 자리를 만들기 위해 상태 변화를 외부에 알릴 수단이 필요했다. 두 가지 진행 방식 중 **(b) 이벤트만 먼저 추가하고 `FastRetry()` 자동 호출은 팝업 완성까지 유지**를 채택 — 이 프로젝트는 콘솔 에러 없이 조용히 깨지는 결함 이력이 있어, 언제든 플레이해서 눈으로 확인할 수 있는 상태를 유지하는 쪽이 안전하다.

#### 1. 상태 변경 이벤트 — `static`을 택한 이유

```csharp
public static event Action<GameState> OnStateChanged;

private void SetState(GameState next)
{
    if (CurrentState == next) return;   // 실제로 달라졌을 때만 1회 발행
    CurrentState = next;
    OnStateChanged?.Invoke(next);
}
```

* **인스턴스 이벤트가 아닌 이유**: 구독자(UI)가 `GameManager.Instance.OnStateChanged += ...`를 `OnEnable`에서 하면 **`GameManager.Awake`보다 먼저 돌 경우 `Instance`가 null이라 구독이 조용히 실패**한다. 이건 직전에 수정한 `IsPointerOverGameObject` 버그와 **정확히 같은 종류의 실행 순서 의존**이다. static이면 인스턴스 존재 여부와 무관하게 구독이 성립한다.
* 구독자 규약(주석에 명시): ① `OnEnable`에서 구독 + `Start`에서 `CurrentState`를 1회 직접 읽어 초기 표시를 맞춘다, ② `OnDisable`/`OnDestroy`에서 반드시 해제(CLAUDE.md §4).
* **도메인 리로드 실측 확인**: `EditorSettings.enterPlayModeOptionsEnabled = False` → static이 플레이 세션마다 초기화되므로 안전. 다만 나중에 Fast Enter Play Mode를 켜면 구독자가 세션을 넘어 살아남으므로, `OnDestroy`에서 `Instance == this`일 때만 `OnStateChanged = null` 처리. **싱글턴 중복으로 파기되는 쪽이 전역 상태를 지우지 않도록** 가드가 필수다.
* **박싱 없음**: `Action<GameState>`는 제네릭이 값 타입으로 인스턴스화되어 enum을 박싱하지 않는다(런타임 특성). ⚠️ 초안에 적었던 "전이 2000회 실측 0 bytes"는 **철회한다** — 같은 날 후속 작업에서 이 환경의 GC 측정기가 신뢰 불가임이 드러났다(아래 HUD 로그 §4).

#### 2. `GameState.Pause` — `timeScale`만으로는 부족했던 점

`Time.timeScale = 0`이면 `FixedUpdate`가 멈춰 물리와 중력 회전이 함께 동결된다. 그런데 **`Update()`는 timeScale 0에서도 계속 돈다.** `InputController.Update()`가 그대로 살아 있어 일시정지 중 드래그가 `_targetAngle`에 누적되고, 재개하는 순간 미로가 그 각도로 튄다.

→ `InputController.SetInputEnabled(bool)`을 신설해 `GameManager`가 명시적으로 잠근다(잠글 때 진행 중인 드래그도 `ResetInput()`으로 해제). `InputController`가 `GameManager`를 역참조하지 않도록 기존 `_inputController` 직렬화 참조 패턴을 그대로 따랐다.

또 하나 — **일시정지 중 재시작하면 `timeScale`이 0인 채로 남는다.** `FastRetry()` 맨 앞에서 `Time.timeScale = 1f`를 먼저 되돌리도록 처리.

#### 3. 검증 (플레이 모드 실측)

| # | 시나리오 | 결과 |
|---|---|---|
| T1 | `Pause()` | `Pause` / `timeScale=0` / `inputEnabled=False` · 이벤트 1회 |
| T2 | `Pause()` 재호출 | **이벤트 추가 발행 없음** (멱등) |
| T3 | Pause 중 `GameOver()` | 무시됨 |
| T4/T5 | `Resume()` / 재호출 | `Play`/`1`/`True` · 재호출은 무발행 |
| T6 | Pause 후 `FastRetry()` | **`timeScale=1` 복원** |
| T7 | Clear 중 `Pause()` | 차단됨 |
| T8 | Play 중 `GameOver()` | `[GameOver, Play]` → 타이머 `7.5`→`0`, 공 `vel=(0,0)` 재활성, 중력 `(0,-14.72)` 복원 |

T8이 (b) 방침의 핵심 — **실패해도 즉시 다시 굴릴 수 있는 상태가 유지된다.**

#### 4. ⚠️ 검증하지 못한 것 (도구 한계)

"일시정지 중 타이머·물리가 **프레임이 흐르는 동안** 멈추는지"는 확인하지 못했다. 에디터가 백그라운드일 때 플레이어 루프를 돌리지 않아 **`Time.frameCount`가 realtime 47초 동안 `3`에 고정**됐다 — `timeScale=1`·`state=Play`로 되돌려도 동일했으므로 내 코드가 아니라 에디터 포커스 게이팅이다(직전 세션의 터치 시뮬레이션을 막았던 것과 같은 원인). 프레임이 아예 안 흐르니 "멈췄다"를 증명할 수 없다.

메커니즘 자체는 이중 보장(`timeScale=0` → `Time.deltaTime==0` + `FixedUpdate` 미호출, 그리고 `Update()`의 `CurrentState == Play` 가드)이지만, **새 DoD 규칙 2에 따라 `Task.md`의 해당 `[QA]` 항목은 `[ ]`로 남겼다.** 인터랙티브 플레이테스트에서 확인할 것.

* **해결된 이슈**:
  * UI가 붙을 상태 변화 훅 부재 → static 이벤트로 실행 순서 의존 없이 해결
  * `GameState.Pause`가 선언만 되고 동작하지 않던 문제 (GDD §8.1 `[인게임] ⇄ [일시정지]` 성립)
  * 일시정지 중 드래그 누적으로 재개 시 미로가 튀는 문제 (구현 전 선제 차단)
  * 일시정지 중 재시작 시 `timeScale`이 0으로 잔류하는 문제 (구현 전 선제 차단)

---

### 📅 [2026-08-07] 프로젝트 전면 점검 — 완료 표기 위조 3건 적발, Layer Matrix 수정, `Task.md` 재작성

* **작업 배경**: "이전 Task들에서 문제가 계속 나온다"는 지적을 받아, 코드가 아니라 **`Task.md` 자체**를 프로젝트 완성 관점에서 감사했다. 모든 `[x]` 항목을 MCP 실측으로 대조.

#### 1. 적발된 "완료 표기 ≠ 실제 상태" 3건

| # | 항목 | 실측 결과 |
|---|---|---|
| ① | **Phase 3 색상 게이트(USP)** | 색 레이어 4종이 **정의만 되고 어떤 오브젝트에도 미할당** (공·Goal 전부 `Default(0)`), 씬·프리팹에 **Gate 오브젝트 0개**. GDD §1이 "주요 차별점(USP)"으로 규정한 기믹이 통째로 없는데 `[x]` |
| ② | **Layer Collision Matrix** | 스펙과 **정반대**로 설정 (아래 상술) |
| ③ | **Phase 2 피드백** | `FeedbackManager` **호출처 0건**, 코드베이스 전체에 `AudioSource`/`AudioClip`/`ParticleSystem` 참조 0건. 실체는 쿨타임 가드가 든 `PlayHaptic()` 스텁 하나 |

* **①의 뼈아픈 점**: `TDD.md §5.2:114`가 *"레이어는 생성돼 있으나 공은 `Default(0)`에 있고 Matrix 미배선(게이트 기믹 미구현)"* 이라고 **정확히 기록해 두고 있었다.** TDD는 맞았고 Task.md만 닫혀 있었다 — 두 문서를 대조하는 절차가 없어 게이트가 문서 사이 틈으로 사라졌다.
* 부수 적발: Phase 3.5 QA 항목이 `[x]`인데 주석에 "인터랙티브 플레이테스트 필요"라 적혀 있었다(자기모순).

#### 2. Layer Collision Matrix 수정 (커밋 `d23ec99`)

* **증상**: 꺼져 있던 유일한 두 칸이 `Gate_Yellow×Gate_Yellow` / `Gate_Green×Gate_Green` — **게이트는 정적이라 서로 충돌 계산 자체가 없으므로 아무 효과가 없는 칸**이었다. 반대로 반드시 꺼야 할 `Ball_X × Gate_X`는 켜져 있었다. 매트릭스 UI에서 대각선을 잘못 클릭한 전형적 오조작.
* **영향**: 이 상태로 게이트를 만들었다면 **모든 공이 모든 게이트에 막혀** "왜 통과가 안 되지"로 수렴했을 것. 게이트 구현 전에 잡아서 다행.
* **수정** (TDD §5.2 / GDD §5.1.1 "같은 색 통과 / 다른 색 차단"):

  | 쌍 | 변경 |
  |---|---|
  | `Ball_Yellow(6) × Gate_Yellow(8)` | ON → **OFF (통과)** |
  | `Ball_Green(7) × Gate_Green(9)` | ON → **OFF (통과)** |
  | `Ball_X × Gate_Y` (이색) | ON 유지 (차단) |
  | `Gate_X × Gate_X` | OFF → **ON (기본값 복원)** — 비기본 상태를 남겨두면 나중에 혼란 |
  | 공-공 전 교차, `Default × Ball_*` | ON 유지 (GDD §5.3 / 벽·Goal·장애물 충돌 보존) |

* **검증 방식**: `Physics2D.GetIgnoreLayerCollision()` 반환값을 믿지 않고, 저장된 `ProjectSettings/Physics2DSettings.asset`의 `m_LayerCollisionMatrix` 256자 헥스를 **직접 비트 디코딩**해 대조.
  ```
  layer 6 Ball_Yellow  mask=0xFFFFFEFF  bit8=0(통과) bit9=1(차단) bit6,7=1(공-공)
  layer 7 Ball_Green   mask=0xFFFFFDFF  bit9=0(통과) bit8=1(차단)
  layer 8 Gate_Yellow  mask=0xFFFFFFBF  bit6=0        bit8=1(복원)
  layer 9 Gate_Green   mask=0xFFFFFF7F  bit7=0        bit9=1(복원)
  ```
  → 영속화는 `EditorApplication.ExecuteMenuItem("File/Save Project")`로 처리(런타임 API 호출만으로는 디스크에 안 남을 수 있음).

#### 3. `Task.md` 전면 재작성 — 근본 원인은 "완료 기준의 부재"

세 사고의 원인이 전부 같았다. 구 문서의 `[x]`는 **"그 방향으로 뭔가 했다"** 를 뜻했다 — 레이어를 *만들었으면* 체크(할당 안 함), 매니저를 *작성했으면* 체크(호출 안 함). Unity의 최빈 실패 모드인 *"코드는 맞는데 인스펙터가 비어 있음"* 을 **구조적으로 잡아낼 수 없는 문서**였다.

* **도입한 완료 기준(DoD)**:
  1. 모든 항목은 `[Code]`/`[Editor]`/`[Asset]`/`[QA]`/`[Doc]` 태그를 하나 갖는다
  2. `[x]`는 **MCP 실측 또는 플레이 검증**으로 확인된 것만
  3. **코드 작성과 씬/프리팹 배선은 반드시 별도 항목으로 분리**
  4. `[QA]` 항목이 없는 Phase는 닫지 않는다
  5. **"정의"와 "할당·배치"는 다른 항목** (①의 재발 방지)
  6. TDD/GDD와 어긋나면 TDD 우선, 발견 즉시 양쪽 동기화
* **Phase 번호 정상화**: 구 체계는 `1 → 1.5 → 2 → 3 → 3.5 → 4 → 5.0 → 5.1 → 5 → 6 → 7`로 **5.0/5.1이 5보다 앞에 오는** 붕괴 상태였다. 순차 1~14로 재배열하고 **구↔신 매핑표**를 문서에 넣어 `DevelopLog`/`TDD`의 과거 서술을 추적 가능하게 유지.
* **Phase 헤더에 `선행` / `완료 조건` 필드 신설**: 의존성이 매번 사고 후에 인라인 노트로 붙던 패턴(`"Phase 5 선행 필수"`, `"세이브 → 로비 순으로"`)을 정규 필드로 승격.
* **GDD 대비 누락 워크스트림 신설**: 색상 게이트(Phase 8) · 동적 기믹(9) · **레벨 콘텐츠·난이도·FTUE(10)** · 씬 플로우/Build Settings(11) · 오디오 에셋 확보(13) · 색맹 문양 각인(14). 구 계획은 **레벨이 1개뿐인 상태에서 상점부터 만들게** 되어 있어 GDD §11.1 MVP 순서와 어긋났다.

#### 4. 함께 발견해 등록한 잠복 이슈

* **카메라 에디터/런타임 불일치**: 컴포넌트 저장값 `orthographicSize = 6`, 런타임은 `Awake`가 `defaultOrthoSize = 10.5`로 덮어씀 → **에디터에서 플레이어보다 75% 좁은 화면을 보고 레벨을 만들게 된다.** `TDD.md:265`가 `6`을 "과거 오설정"으로 명시했는데 컴포넌트 값만 남아 있었다. 레벨 양산(Phase 10) 전 정리 필요
* **`GameState.Pause`가 선언만 되고 set/read 0건** → GDD §8.1의 `[인게임] ⇄ [일시정지]` 성립 불가 (Phase 7 등록)
* **`GameSettings` ScriptableObject 부재** — CLAUDE.md §4 요구사항. 회전 튜닝값이 `WorldRotationController.cs:12-16`에 산재 (Phase 10 등록)
* **승/패 동시 발생 우선순위 미결** — `TDD §7.6`이 현 구현을 *first-event-wins(콜백 순서 비결정적)* 한계로 기록했으나 Task에 결정 항목이 없었다 (Phase 3 등록)
* **`Debug.Log` 5곳 상시 출력** (`CameraController.cs:65`는 보간 문자열) — 릴리스 스트립 항목 없었음 (Phase 14 등록)

* **해결된 이슈**:
  * Layer Collision Matrix 오설정 (게이트 구현 시 100% 발현될 버그를 사전 차단)
  * 완료 표기 위조 3건을 실제 상태로 정정
  * `Task.md`가 사고를 구조적으로 못 잡던 결함 — DoD·태그·선행조건 규약 도입
  * GDD 요구사항 대비 누락 워크스트림 6종 등록 (이대로 갔으면 Task 100% 완료해도 GDD의 게임이 안 나왔다)

---

### 📅 [2026-08-06] Phase 5.1 — 터치 UI 무시 경로 수정: `IsPointerOverGameObject` 의존 제거

* **작업 배경**: 직전 로그에서 발견한 버그(`InputController.cs:53`이 `IsPointerOverGameObject(touch.finger.index)` 호출)를 수정. 당시 조치안은 "`touch.touchId`로 교체(1줄)"였으나, **착수 후 그것만으로는 부족하다는 것이 확인되어 방식을 바꿨다.**
* **왜 1줄로 부족했나 — 두 번째 결함**: `IsPointerOverGameObject`는 `EventSystem.Update()`가 채워둔 상태를 읽는다(패키지 주석 `InputSystemUIInputModule.cs:264-266`이 명시). 그런데 `EventSystem`(ugui 2.0.0 원본 확인)에는 **`DefaultExecutionOrder` 지정이 없어** `InputController`와 같은 order 0 버킷에 있고, 둘의 상대 실행 순서는 보장되지 않는다. `InputController.Update()`가 먼저 돌면 **터치가 시작된 그 프레임에는 아직 상태가 없어 `false`** 가 나오고, `_isDragging`은 래치되므로 **그 드래그 전체가 새어나간다.** ID를 고쳐도 절반만 고치는 셈.
* **채택한 방식 — 직접 레이캐스트**: `IsPointerOverUI(Vector2 screenPos)` 헬퍼 신설. 캐시된 `PointerEventData` + `List<RaycastResult>`로 `EventSystem.current.RaycastAll()`을 직접 호출한다. Input System 패키지가 **같은 상황의 회피책으로 제시하는 방식과 동일**하다(`Samples~/UIvsGameInput/UIvsGameInputHandler.cs:377-389`).
  * 실행 순서 무관 — 그 자리에서 즉시 판정.
  * ID 의미론 문제 소멸 — 애초에 ID를 넘기지 않는다. 터치/마우스 경로가 같은 코드를 쓴다.
  * `[DefaultExecutionOrder]`로 순서를 강제하는 대안도 있었으나 **보이지 않는 전역 결합**이 생긴다(누가 나중에 Script Execution Order를 건드리면 콘솔 에러 없이 버그가 부활). 이 프로젝트가 반복해서 당한 유형이라 기각.
  * 호출 시점이 **드래그 시작 때뿐**(프레임마다 아님)이라 레이캐스트 비용은 무시 가능.
* **검증 (플레이 모드, 임시 프로브 버튼 400×200 중앙)**:

  | 검사 | 결과 |
  |---|---|
  | `IsPointerOverUI(540,960)` 프로브 위 | `True` ✅ |
  | `IsPointerOverUI(100,300)` UI 없음 | `False` ✅ |
  | `IsPointerOverUI(345,960)` 좌경계 **안** | `True` ✅ |
  | `IsPointerOverUI(335,960)` 좌경계 **밖** | `False` ✅ |
  | 구 코드가 넘기던 `IsPointerOverGameObject(0)` (UI 위에서) | `False` ❌ ← 버그 재확인 |
  | GC (5000회, 무할당 메서드 기준선 차감) | `4096 bytes` ≈ 0.8B/call — 사실상 0 |

  경계 10px 안팎에서 판정이 뒤집히는 것으로 프로브 rect를 실제로 맞히고 있음을 확인. 콘솔 에러 0건. 프로브는 검증 후 제거.
* **🔍 도구 한계 (정직하게 기록)**: **실기기와 동일한 "터치로 버튼을 눌러본" 종단 검증은 하지 못했다.** 가상 `Touchscreen`에 `TouchState`를 큐잉해 인게임 시뮬레이션을 시도했으나, 에디터 플레이 모드는 **Game View에 포커스가 없으면 포인터/터치 입력을 폐기**한다(MCP 구동 시 항상 해당). 우회하려면 `editorInputBehaviorInPlayMode`를 바꿔야 해 프로젝트 설정을 건드리게 되므로 중단하고, 대신 **변경된 판정 함수를 리플렉션으로 직접 호출**하는 방식으로 검증했다. 호출부는 2줄(`:58`, `:82`)이라 육안 확인으로 충분하다고 판단.
  * 시행착오 기록: `TouchState.pressure`를 0으로 두면 눌림으로 취급되지 않아 터치가 아예 등록되지 않는다(`InputTestFixture.SetTouch`는 `pressure: 1`). 또 `InputSystem.Update()`를 직접 부르면 이벤트가 그 자리에서 소비돼 **`Began` 페이즈가 MonoBehaviour `Update()` 프레임에 걸리지 않는다** — 이 두 함정 때문에 초기 테스트 2건이 "통과"로 잘못 읽혔다(`_isDragging = False`가 "UI를 걸러냈다"가 아니라 "터치 경로를 아예 안 탔다"였음). `_isUsingTouch`와 `_initialTouchAngle`(EndDrag가 되돌리지 않는 값)을 함께 봐야 구분된다.
* **남은 작업**: 실기기 빌드에서 버튼 위 드래그 시 미로가 돌지 않는지 확인 (Phase 7 디바이스 테스트에 포함).

---

### 📅 [2026-08-06] Phase 5.0 — UI 기반 셋업 3건 완료 및 터치 UI 무시 경로의 잠복 버그 발견

* **작업 내용**: Phase 5 진입 전 선행 결함으로 기록해 둔 3건을 MCP로 처리했다.
  * **`UICanvas` 신설** — `Screen Space - Overlay`, `CanvasScaler` = `ScaleWithScreenSize` / `1080 × 1920` / `MatchWidthOrHeight 0.5`, Layer `UI`. (CLAUDE.md §6 기준값)
  * **`EventSystem` 활성화** — 기존 `activeSelf = false`.
  * **입력 모듈 교체** — 레거시 `StandaloneInputModule` 제거 → `InputSystemUIInputModule` 추가.
* **⚠️ 왜 '체크박스 하나'가 아니었나**: `ProjectSettings.activeInputHandler = 1`(New Input System **전용**)이다. 이 상태에서 레거시 `StandaloneInputModule`이 붙은 `EventSystem`을 그냥 활성화하면 모듈이 `UnityEngine.Input`을 읽으려다 런타임 예외가 난다. 모듈 교체가 활성화와 반드시 세트여야 했던 이유. `actionsAsset`은 추가 시 패키지 기본값(`DefaultInputActions.inputactions`)으로 자동 배선되어 수동 연결이 필요 없었다.
* **검증 (플레이 모드 실측)**: UI 요소가 0개면 `IsPointerOverGameObject`가 항상 `false`라 검증 자체가 성립하지 않으므로, 임시 프로브 버튼(`__UIProbe`, 400×200 중앙)을 넣고 확인 후 제거했다.

  | 확인 항목 | 결과 |
  |---|---|
  | `EventSystem.current.currentInputModule` | `InputSystemUIInputModule` ✅ |
  | `Canvas.renderMode` / `GraphicRaycaster` | `ScreenSpaceOverlay` / 존재 ✅ |
  | `RaycastAll` 화면 중앙 | 1히트 (`__UIProbe`) ✅ |
  | `RaycastAll` 모서리 `(5,5)` | 0히트 ✅ |
  | `GameManager.CurrentState` | `Play` (즉시 게임오버 없음) ✅ |
  | 콘솔 에러 | 0건 ✅ |

* **🔍 신규 발견 — 터치 환경에서 UI 무시 경로가 동작하지 않는다 (미조치)**: 위 검증 중 패키지 원본(`InputSystemUIInputModule.cs:293`, `GetPointerStateIndexFor:1779`)을 직접 읽어 확인한 결과,
  * `IsPointerOverGameObject(id)`는 `id`를 **`pointerId` / `ExtendedPointerEventData.touchId` / `device.deviceId`** 중 하나로만 매칭한다. 게다가 `touchId != 0`인 항목만 deviceId 폴백 대상이라 **`0`은 구조적으로 매칭될 수 없다.**
  * 그런데 `InputController.cs:53`은 **`touch.finger.index`** 를 넘긴다. `Finger.index`는 "화면상 N번째 손가락 슬롯 번호"(0-based)로 `touchId`와 전혀 다른 값이다. → **첫 손가락은 항상 `index 0` → 항상 `false`** → 실기기에서 버튼을 눌러도 미로가 회전한다. 두 번째 손가락(`index 1`)은 우연히 `touchId 1`(= 첫 번째 터치)에 매칭돼 더 나쁘다.
  * 에디터 마우스 경로(`InputController.cs:78`, `-1`)는 문서상 **"any pointer"** 로 해석되므로 정상 동작한다. 그래서 지금까지 에디터 플레이만으로는 드러나지 않았다.
  * **조치안**: `touch.touchId`로 교체(1줄). 드래그 추적용 `_currentTouchId`는 `finger.index` 유지로 무방하다(컨트롤러 내부 일관성만 필요). Phase 5.1 첫 항목으로 등록.
* **의의**: Phase 5의 모든 UI 작업이 올라탈 바닥이 실제로 동작함을 수치로 확인했다. 동시에, 그 바닥 위에서 "UI를 눌렀는데 미로가 도는" 형태로만 드러났을 버그를 **UI를 만들기 전에** 잡아냈다.
* **다음**: Phase 5.1 — 위 터치 버그 수정 → `GameManager` 상태 이벤트 노출(현재 `GameOver()`는 즉시 `FastRetry()`, Clear는 1.5초 뒤 자동 `FastRetry()`라 UI 팝업이 낄 자리가 없음) → HUD/결과 팝업. **로비는 세이브 시스템 이후**로 순서 조정(표시할 별 기록·스테이지 목록이 선행되어야 함).

---

### 📅 [2026-08-06] Phase 4 종료 — 곡선 벽 저작 완료 및 `RoundWall_0` 프리팹화 (재사용 경로 확보)

* **작업 내용**: 사용자가 곡선 벽 형상을 실제로 저작하고 플레이테스트를 마친 뒤, 정렬까지 끝난 상태를 `Assets/Prefabs/RoundWall_0.prefab`으로 프리팹화. 이로써 Phase 4의 마지막 잔여 항목(곡선/대각형 미로 형상 저작)이 종료됨.
* **프리팹 실측 검증 (MCP)**: `colliderOffset 0.5` · `colliderDetail 16` · `splineDetail 16` · `autoUpdateCollider True` · 닫힌 스플라인 6점 · `PolygonCollider2D(usedByComposite True, compositeOperation Merge, sharedMaterial none, 30점)` · `scale (1,1,1)`. 씬의 `MazeGrid/Maze/RoundWall_0`은 프리팹 인스턴스 연결이 유지되고 있으며(`prefabSrc` 확인), `Maze.CompositeCollider2D`는 `shapeCount 21 / pathCount 2 / pointCount 43`, `errorState None`, 콘솔 에러 0건.
* **의의**: 곡선 벽 신규 생성 시 반복해야 했던 6단계 수작업 셋업(부모 배치 → `PolygonCollider2D` 추가 → `Used By Composite` → 머티리얼 비움 → `Collider Offset 0.5` → 스플라인 편집)이 **드래그 1회로 축소**됨. 특히 `Collider Offset`을 빠뜨리면 공이 벽에 파묻히는데(2026-07-31 로그 참조) 콘솔 에러가 나지 않아 놓치기 쉬운 항목이라, 프리팹화의 실익이 크다.
* **Phase 3.5 QA 종결**: 좁은 통로 교착(끼임) 인터랙티브 플레이테스트를 사용자가 수행하여 통과 확인. TDD §5.3.1의 위험 구간(폭 `1.05~1.25`) 회피 설계가 실제로 유효함을 검증.
* **⚠️ 다음 Phase 진입 전 확인된 선행 결함 (미조치)**:
  * 씬에 **`Canvas`가 0개**. Task.md Phase 1.5의 "UI Canvas Render Mode 확인" 체크는 실제 씬 상태와 불일치(스테일).
  * `EventSystem`이 **비활성 상태(`activeInHierarchy=False`)**이며, 모듈이 New Input System이 아닌 **레거시 `StandaloneInputModule`**. 이 상태로는 UI 클릭이 동작하지 않고, `InputController`의 UI 터치 무시(CLAUDE.md §3 / TDD §3) 경로도 검증 불가.
  * → Phase 5(UI/메타) 착수 전 3건(Canvas 신설 · EventSystem 활성화 · `InputSystemUIInputModule` 교체) 선행 필요.

---

### 📅 [2026-07-31] SpriteShape 곡선 벽 — 콜라이더가 시각보다 안쪽에 생성되어 공이 파묻히던 문제 해결

* **증상 (사용자 보고)**: 곡선 벽 자체는 잘 작동하나, **콜라이더가 안쪽에 있어 공이 벽에 파묻히는** 현상.
* **원인 규명 (추측 없이 원본 에셋 실측)**: SpriteShape의 콜라이더는 `Collider Offset = 0`이면 **스플라인 선 위**에 생성되는데, 엣지 스프라이트는 pivot 기준으로 스플라인 **바깥쪽까지** 그려진다. 프로필이 참조하는 원본 `SpriteShapeEdge.png`를 패키지 캐시에서 찾아 확인 — **128px / PPU 256 / pivot.y 0.5** → 스플라인 바깥 돌출 두께 `t = 128/256 × (1−0.5) =` **0.25 로컬 유닛**. 공 월드 반지름이 0.3이므로 파묻힘 깊이 0.149(월드)는 **반지름의 약 절반**, 눈에 띌 수밖에 없는 값이었다.
* **해결 — `Collider Offset = 0.5`**: 계산상 `0.25`면 될 것 같지만 **Unity의 `Collider Offset` 단위가 실제 돌출량의 절반**이라 `2t = 0.5`가 정답. 추정으로 넘기지 않고 `SpriteShapeRenderer.localBounds`(보이는 범위)와 `PolygonCollider2D.points`의 AABB를 비교해 실측:

  | `Collider Offset` | 콜라이더 폭 | 보이는 폭 | 한쪽당 어긋남 |
  |---|---|---|---|
  | `0` | 2.2914 | 2.7884 | 0.2485 (= `t`, 계산값 0.25와 일치) |
  | `0.25` | 2.5404 | 2.7884 | 0.1240 |
  | **`0.5`** | **2.7839** | 2.7884 | **0.0023** ✅ |

  파묻힘이 **공 반지름의 50% → 0.5%** 로 감소.
* **🔍 도구 함정 발견**: `colliderOffset`을 스크립트로 바꾼 뒤 `RefreshSpriteShape()` + `BakeCollider()`만 부르면 **지오메트리 생성이 지연되어 이전 값이 그대로 반환된다.** 실제로 한 호출 안에서 offset을 `0/0.125/0.25/0.5/1.0`으로 스윕했을 때 **5개 결과가 전부 동일**하게 나와 하마터면 "offset이 안 먹는다"고 오판할 뻔했다. **`UpdateSpriteShapeParameters()` → `BakeMesh()` → `BakeCollider()`** 순서로 호출해야 동기 반영된다(인스펙터 수동 조작 시에는 해당 없음). TDD §6.1.1에 기록.
* **부수 확인**: 적용 후에도 `Maze.CompositeCollider2D`는 `pathCount 2` 유지(타일맵 벽과 융합된 단일 외곽선), `shapeCount 22 / pointCount 44`, `errorState 0`, 콘솔 에러 0건. 병합은 온전.
* **해결된 이슈**: 곡선 지형에서 시각과 충돌 지점이 어긋나 조작감이 깨지던 문제 해소. SpriteShape 도입 시 반드시 거쳐야 하는 정렬 절차를 수식(`Collider Offset = 2t`)과 검증법으로 문서화해 다음 곡선 벽부터는 시행착오 없이 적용 가능.
* **⚠️ 후속 주의**: 콜라이더가 사방으로 `t`(월드 0.15)만큼 커졌으므로 **인접 통로가 그만큼 좁아졌다.** 병목 폭 설계 시 §5.3.1 끼임 위험 구간(`1.05~1.25`)과 함께 재확인 필요.

---

### 📅 [2026-07-31] 씬 셋업 결함 2건 수정: DeadZone 이탈 감지 복구 & 세로 화면 카메라 클리핑 해소

* **작업 배경**: "에디터에서 내가 할 작업이 무엇이냐"는 질문에 답하기 위해 씬 값을 MCP로 훑던 중, 코드가 아니라 **씬 셋업 쪽에 조용히 죽어 있던 기능 2건**을 발견해 수정함. 둘 다 콘솔에 에러를 남기지 않아 플레이만으로는 드러나지 않는 유형.
* **결함 ① — DeadZone이 미로와 완전히 분리되어 장외 이탈 감지가 무력**:
  * **증상/원인**: `DeadZone` 오브젝트가 `(100, 0, 0)`에 100×100 크기로 홀로 있어 실제 커버 범위가 `x 50~150 / y −50~50`이었다. 미로는 `±4`, 공 시작점은 `(0.5, 1.5)`·`(1.5, −2.5)` → **완전히 동떨어져** 공이 벽을 뚫고 나가도 `GameOver()`가 호출되지 않는 상태. TDD §7.4의 "플레이 영역과 겹치면 안 된다"는 제약을 지키려다 **반대편으로 과도하게 밀어낸** 형태로 보인다.
  * **조치**: `DeadZone`을 `(0,0,0)`으로 옮기고 `BoxCollider2D` **4개**로 사방 킬 프레임 구성 — 상 `Offset(0,11)/Size(32,10)`, 하 `(0,−11)/(32,10)`, 좌 `(−11,0)/(10,32)`, 우 `(11,0)/(10,32)`, 전부 `Is Trigger`. 한 GameObject의 여러 콜라이더가 동일한 `OnTriggerEnter2D`를 호출하므로 **스크립트는 1개로 충분**해 자식 오브젝트 분리 없이 해결.
  * **설계 근거**: 안쪽 경계 ±6(미로 ±4·공 시작점과 미접촉 → 즉시 게임오버 없음), 바깥 ±16(두께 10 → 고속 터널링 관통 불가), 네 박스가 모서리까지 빈틈없이 커버.
* **결함 ② — 세로 화면에서 미로가 잘림 (`defaultOrthoSize = 6`)**:
  * **증상/원인**: 실기기 비율(1080×1920 = 0.5625)에서는 `currentAspect == targetAspect`라 `AdjustCameraViewport()`의 보정 분기를 타지 않고 `6`이 그대로 적용된다. 이때 **가로 반폭 = 6 × 0.5625 = 3.375** 로, 미로 반폭 4보다 작아 **회전을 하지 않아도 좌우가 잘리는** 상태였다. 에디터 Game 뷰가 가로형(비율 1.60)이라 `else` 분기에서 반폭 9.59가 나와 **문제가 가려져 있었음**.
  * **조치**: `defaultOrthoSize` `6 → 10.5`. 구속 조건이 세로가 아니라 **가로 반폭**이라는 점에서 `defaultOrthoSize ≥ R / targetAspect`(R = 외접원 반지름) 수식을 확정 — `5.657 / 0.5625 = 10.06`이 최소값이며 여유를 둬 10.5 적용(가로 반폭 5.906).
  * **트레이드오프 기록**: 세로 반높이가 10.5가 되어 미로 위아래 여백이 커진다. 정사각 미로를 세로 화면에서 회전시키는 구조의 필연적 비용이며, 여백은 HUD로 채우거나 미로를 키워 흡수하기로 함(Phase 5).
* **검증**: 값 적용 후 씬 저장 → 플레이 모드 진입. `[CameraController] Screen: 668x418, Ratio: 1.60, OrthoSize: 10.50` 로그로 값 반영 확인, 런타임 `Camera.orthographicSize = 10.5` 확인, **`GameManager.CurrentState = Play`** 로 데드존 프레임이 공 시작 위치를 침범하지 않음(즉시 게임오버 없음) 확인. 콘솔 에러 0건.
* **해결된 이슈**: 벽 뚫림(터널링) 발생 시 소프트락에 빠지던 경로 차단 — 이제 이탈하면 정상적으로 Fast Retry로 복귀. 실기기 세로 화면에서 미로가 화면 밖으로 잘려나가는 문제 해소. 두 결함 모두 **에디터 Game 뷰가 가로형이면 재현되지 않는다**는 점을 TDD 부록 체크리스트에 명시해 재발 방지.
* **문서**: `TDD.md` **v1.2** — §7.4에 프레임 실제 수치표·설계 근거 추가, §9에 클리핑 방지 수식과 과거 오설정 경위 기록, 부록 셋업 체크리스트 2항 추가.

---

### 📅 [2026-07-30] Phase 4 — SpriteShape ↔ CompositeCollider2D 병합 파이프라인 구축 및 동적 기믹 규칙 실측 확정

* **작업 배경**: Phase 4 진입 시점에 MCP로 프로젝트 상태를 감사한 결과, 문서상 미완이던 항목 상당수가 이미 되어 있었고(아래) 실제 남은 병목은 SpriteShape의 콜라이더 병합 파이프라인이었음.
* **상태 감사 결과(문서 드리프트 정정)**: ① `StageData` 로드 — `Assets/Data/Stage 1.asset`이 이미 존재하고 `GameManager._currentStageData`에 배선되어 별점 산출이 동작 중 → **완료 처리**. ② SpriteShape 패키지 `com.unity.2d.spriteshape 13.0.0` 설치 및 프로필 에셋 존재 확인. ③ 반면 씬의 SpriteShape 오브젝트는 이름이 기본값 `GameObject`인 채 **루트에 방치**되어 있었고 `hasCollider = false`(콜라이더 자체가 없어 물리적으로 존재하지 않는 순수 그림) → 병합 불가 상태.
* **주요 작업 내용**:
  * **SpriteShape 정규화**: `GameObject` → `Wall_SpriteShape_01`로 개명, `MazeGrid/Maze` 자식으로 이동. `PolygonCollider2D` 추가 시 `SpriteShapeController.autoUpdateCollider`가 닫힌 스플라인을 5점 폴리곤으로 자동 베이크(`hasCollider: false → true`).
  * **Composite 병합 실측 검증**: `Used By Composite`(`compositeOperation = Merge`) 적용 후 `Maze.CompositeCollider2D`가 `shapeCount` **15→16**, `pathCount` **2→3**, `pointCount` **32→37**, `bounds` 8×8→12.52×8로 확장됨을 확인. 자식 콜라이더는 `sharedMaterial = null`이라 Composite의 단일 `Wall Physics Material`이 그대로 지배 — 의도한 "맵 전체 마찰·반발 일괄 제어"가 성립.
  * **동적 기믹 워크플로우 검증**: `Maze` 자식에 `Used By Composite` **미체크** 콜라이더를 배치하면 Composite `shapeCount`가 16으로 **불변**이고 자체 `shapeCount = 1`의 독립 도형으로 남는 것을 확인(검증용 프로브는 확인 후 삭제).
  * **에셋 정리**: `Sprite Shape Profile.asset`을 `Assets/Prefabs/` → `Assets/SpriteShapes/`로 이동(GUID 보존, `SpriteShapeController`의 참조 유지 확인).
  * **문서 동기화**: `Task.md` Phase 4의 ①②④를 `[x]`로 갱신하고 ③은 검증분/잔여분을 분리 주석. `TDD.md`를 **v1.1**로 올리며 §6.1(SpriteShape 병합 절차·검증 지표)·§6.2(동적 기믹 규칙) 신설, §10에 StageData 실제 로드 경로와 **`_currentStageData`가 `null`이면 무조건 1별을 반환하는 무음 폴백** 경고 명시, 부록 셋업 체크리스트에 2항 추가. 변경 이력 표에 누락돼 있던 2026-07-06 행도 함께 보정.
  * **씬 저장**: 위 변경은 MCP로 수행 후 `SampleScene.unity`에 저장 완료(+75/−5).
* **부수 관찰(미조치)**: 씬의 `EventSystem`이 **비활성** 상태다. `InputController`가 `EventSystem.current`에 null 가드를 두고 있어(53·78행) 현재는 무해하지만, **Phase 5에서 UI를 붙일 때 활성화하지 않으면 UI 터치 예외 처리(TDD §3)가 통째로 동작하지 않는다.** 지금은 UI가 없어 범위 밖으로 두고 기록만 남김.
* **🔍 신규 발견 (TDD §6.2 반영)**: `Used By Composite`를 끄는 것만으로는 동적 기믹이 안전하지 않다. 자식 콜라이더의 `attachedRigidbody`는 여전히 **부모 `Maze`의 Static 바디**로 잡히므로, 그 상태로 문을 움직이면 결국 **Static Collider Rebuild가 발생**해 병합을 피한 목적이 무산된다. **자체 `Rigidbody2D`(Kinematic)** 를 붙이는 순간 `attachedRigidbody`가 자기 자신으로, `composite`가 `null`로 분리되는 것을 실측 확인. → 동적 기믹 3종 세트: **독립 콜라이더 + 자체 Kinematic RB + Fast Retry 상태 복원**.
* **해결된 이슈**: SpriteShape 지형이 물리적으로 존재하지 않던 문제 해소(비정형 지형을 실제 벽으로 사용 가능해짐), Phase 4의 병합 규칙을 추측이 아닌 **수치 검증 기반**으로 확정, 동적 기믹 도입 시 잠복해 있던 Static Rebuild 함정을 사전 제거. 문서(Task/TDD)와 실제 프로젝트 상태의 드리프트 정정.
* **남은 작업**: 곡선/대각형 미로의 **실제 형상 저작**(스플라인 포인트 편집)은 에디터 수작업 레벨 디자인 영역이라 미완으로 남김.

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
