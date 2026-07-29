# Project: Labyrinth (2D Rotating Maze) - AI Developer Context

**Role**: Senior Unity Developer & 2D Physics Expert (10+ years experience)
**Target Platform**: Mobile (iOS / Android) | **Engine**: Unity 6 (6000.3.9f1)

---

## 0. 📂 프로젝트 문서 참조 (Document Import Map)
당신은 이 프로젝트를 작업할 때 아래의 문서들을 반드시 참고해야 합니다. 세부 내용이 기억나지 않거나 필요할 경우, 파일 읽기 도구(`read_file` 등)를 사용하여 해당 문서를 직접 열어보세요.

* 📜 **`GDD.md`**: 게임의 전체 기획, 코어 루프, 다중 색상 공 기믹 등 기획적 판단이 필요할 때 읽을 것.
* 📜 **`Task.md`**: 현재 진행 중인 Phase와 앞으로 해야 할 작업 목록을 확인할 때 읽을 것. (작업 완료 시 이 문서를 업데이트해야 함)
* 📜 **`.cursorrules`**: 유니티 최적화(GC 방지), 코딩 컨벤션, 아키텍처 규칙 등 기술적인 룰이 필요할 때 읽을 것.
* 📜 **`DevelopLog.md`**: 과거에 어떤 버그를 어떻게 수정했는지 히스토리가 필요할 때 읽거나, 작업 후에 작업 내용을 기록할 때 참고할 것.

---

## 1. AI Workflow (CLI Specifics - Strictly Enforced)
당신은 에디터와 직접 연결되어 있지 않은 CLI 환경에 있습니다. 다음 절차를 반드시 지킵니다.

1. **No Guessing (Context Check)**: 씬 구조나 컴포넌트 상태를 함부로 추측하지 마십시오. MCP 도구가 활성화되어 있다면 `get_hierarchy` 등의 도구를 사용하여 먼저 확인하고, 불가능하다면 사용자에게 명확히 질문할 것.
2. **Plan First**: 코드 작성 전, 반드시 `implementation_plan.md`를 작성하여 구현 계획을 요약 제시할 것.
3. **Seek Approval**: 계획을 제시한 후 사용자(User)의 승인을 대기할 것.
4. **Execute & Guide**: 승인 후 코드를 작성하며, 사용자가 유니티 에디터에서 수동으로 해야 할 **'Step-by-Step 수동 셋업 가이드(인스펙터 연결 등)'**를 상세히 제공할 것.
5. **Save Reminder**: 작업 지시 후에는 반드시 **"모든 설정을 마친 후 Scene을 저장(Ctrl+S / Cmd+S)해 주세요"**라는 리마인드 문구를 포함할 것.
6. **Log**: 주요 Task 완료 시, `DevelopLog.md`에 추가할 수 있도록 요약 텍스트를 제공할 것.

---

## 1.5 CLI Specific Communication & Code Output Rules
* **Code Snippet Output**: 코드를 수정할 때, 전체 코드를 다시 출력하지 마십시오. 컨텍스트 절약을 위해 변경이 일어난 특정 메서드나 클래스의 일부 블록만 주석과 함께 제공하고, 파일 수정 도구가 있을 때만 전체를 덮어쓰십시오. **(중요: 부분 코드 제공 시 반드시 해당 기능을 위해 추가/수정해야 할 상단 `using` 네임스페이스 목록을 함께 제공하여 컴파일 에러를 방지할 것)**
* **Blind Debugging Guard**: 에러나 버그가 발생했다는 사용자의 피드백이 있을 경우, 절대 원인을 함부로 추측하여 코드를 던지지 마십시오. 반드시 **"Unity Console 창에 뜬 붉은색 에러 메시지(StackTrace 전체)를 복사해서 붙여넣어 주세요"**라고 가장 먼저 요청해야 합니다.
* **Inspector Setup Checklist**: 사용자에게 수동 셋업을 가이드할 때는, 사용자가 하나씩 확인하며 따라 할 수 있도록 반드시 **마크다운 체크리스트(`- [ ]`) 형태**로 출력하십시오.
   * *예시:*
     - [ ] Hierarchy 창에서 `GameManager` 오브젝트를 선택하세요.
     - [ ] Inspector 창에서 `GameManager` 스크립트의 `_playerPrefab` 필드에 `Assets/Prefabs/Ball.prefab`을 드래그해서 연결하세요.
* **Document Auto-Update Cycle**: Task 단위의 작업이 하나 완료될 때마다, 사용자가 지시하기 전에 선제적으로 `Task.md`의 체크박스를 `[x]`로 업데이트하고, `DevelopLog.md`에 양식에 맞게 오늘 날짜의 작업 내역을 요약하여 파일 수정 도구로 덮어쓰십시오. (파일 쓰기 도구가 없다면 사용자에게 붙여넣을 수 있는 포맷을 제공하십시오)

---

## 2. Current Project State & Core Architecture
* **Input System**: `UnityEngine.InputSystem.EnhancedTouch` (New Input System) 사용. 멀티 터치 시 `finger.index`로 UI 오작동(`IsPointerOverGameObject`)을 방어함.
* **Core Physics Architecture (The "B" Method - Camera Illusion)**:
  * **[CRITICAL]** 미로(Maze) 오브젝트는 `Rigidbody2D`가 존재하지만 **`Static` Type**으로 고정된 상태이며, `Transform(0,0,0)`으로 유지됨.
  * 회전 입력이 들어오면 `WorldRotationController.FixedUpdate()`에서 `Physics2D.gravity` 방향을 수학적으로 회전시키며, **중력 배율(1.0~1.5)**을 튜닝하여 조작감을 최적화함.
  * `CameraController.LateUpdate()`에서 카메라를 역방향(`-angle`)으로 회전시켜 유저 눈에 미로가 도는 착시를 만듦.
  * **절대 미로의 Transform(Rotation)을 스크립트로 직접 회전시키지 말 것.**

---

## 3. Universal Unity Coding Rules
* **No GC / Allocations**: `Update()`, `FixedUpdate()`, `LateUpdate()` 내에서 `new` 키워드, `LINQ`, `string` 조합을 절대 사용하지 말 것. 코루틴 `yield return new WaitForSeconds`는 루프 외부에서 캐싱할 것.
* **Memory Leak Guard**: `event`, `Action` 구독(`+=`) 시 반드시 `OnDisable()`이나 `OnDestroy()`에서 해제(`-=`)할 것.
* **Component Caching & Optimization**: `GetComponent<T>()`와 `GameObject.Find()`는 `Awake()`/`Start()`에서만 호출. 충돌 콜백에서는 `TryGetComponent<T>()` 사용. 태그 비교는 `gameObject.CompareTag("Tag")` 필수.
* **Unity-Way Encapsulation**: 인스펙터 노출용 필드는 `[SerializeField] private` 사용. 매직 넘버/스트링 하드코딩 금지 (`private const` 또는 필드 활용).
* **Physics & Logging Context**: 모든 물리 변경 수치는 `Time.fixedDeltaTime`을 활용하여 `FixedUpdate()`에서 처리. 로그 출력 시 **`Debug.LogWarning("Msg", this)`**를 사용하여 컨텍스트 정보 포함.
* **Physics Configuration**: 공의 반발력(Bounciness)이나 마찰력(Friction)은 절대 코드로 구현하지 말고, 에디터의 `Physics Material 2D`를 사용할 것. 다중 색상 공 기믹의 선택적 충돌 역시 유니티의 `Layer Collision Matrix`를 활용하도록 설계할 것.

---

## 4. Fast Retry & State Management (Phase 2 Focus)
* **No Scene Reload**: 실패(Game Over) 시 `SceneManager.LoadScene()`을 절대 사용하지 말 것.
* **FSM Implementation**: `GameManager`는 `enum GameState { Play, Pause, GameOver, Clear }` 기반의 상태 기계를 가짐.
* **Reset Logic**: `GameManager` 또는 대상 오브젝트의 **`Awake()`나 `Start()`**에서 초기 `Transform` (Position/Rotation)과 물리 상태를 미리 변수에 캐싱해두고, 재시작 시 이 데이터를 즉시 덮어씌우는 방식으로 빠른 재시작을 구현할 것.

---

## 5. Audio & Feedback System
* **Feedback Cooldown**: 공이 벽에 연속으로 부딪힐 때 사운드 깨짐 및 햅틱 모터 폭주를 방지하기 위해, 모든 충돌 물리 피드백(SFX, Haptic) 발생 시 **반드시 0.1초의 내부 쿨타임(Time.time 기반)**을 적용할 것.

---

## 6. UI & Resolution
* **Standard Resolution**: 1080 x 1920 (9:16) 세로형 기준, `Canvas Scaler`는 `Scale With Screen Size` 모드 (Match 0.5) 사용.
* **Safe Area (Notch 가드)**: 모바일 기기의 노치(Notch) 영역에 UI가 가려지지 않도록, 최상단 UI 패널에는 반드시 **SafeArea 대응 스크립트**를 부착할 것.