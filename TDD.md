# TDD (Technical Design Document)
Project: Labyrinth (2D Rotating Maze)

> **이 문서의 역할**: "어떻게(How)" — 엔진·물리·코드·성능 등 **구현 스펙**을 다룬다.
> 게임 디자인(무엇/왜)은 `GDD.md`, AI 작업 규칙은 `CLAUDE.md`를 본다.
> 본 문서는 2026-06-29 기준 **실제 구현 코드를 근거로** 작성되었으며, 추정이 아닌 사항은 파일 경로를 함께 표기한다.

| 항목 | 값 |
|---|---|
| 버전 | v1.1 |
| 최종 수정 | 2026-07-30 |
| 엔진 | Unity 6 (6000.3.9f1) · URP 2D |
| 플랫폼 | Mobile (iOS / Android), TargetFrameRate 60+ |
| 입력 | New Input System (EnhancedTouch) |
| 물리 | Unity Box2D (2D) |

### 변경 이력
| 날짜 | 버전 | 내용 |
|---|---|---|
| 2026-06-29 | v1.0 | GDD에서 기술/구현 영역을 분리해 신설. 코드 기준으로 B방식 수식·Fast Retry 범위·DeadZone 판정·레이어 정책 정정. |
| 2026-07-06 | v1.0.1 | §5.2 색 시스템 구현 방식을 **역할 분담형 하이브리드**(게이트=레이어 / Goal=코드)로 확정, 게이트 방향·레이어 예산 기록. |
| 2026-07-30 | v1.1 | Phase 4 실측 반영: §6.1 SpriteShape 병합 절차·검증 지표, §6.2 동적 기믹 **자체 Kinematic Rigidbody2D 필수** 신규 발견, §10 StageData 로드 경로 명시, 부록 체크리스트 2항 추가. |

---

## 1. 기술 스택 & 타겟 사양

* **엔진**: Unity 6 (6000.3.9f1), URP 2D (Light2D, SpriteShape 사용).
* **타겟 프레임레이트**: `Bootstrapper`에서 `Application.targetFrameRate = 60`(또는 120+) 강제, 모바일 기본 30 제한 해제 및 화면 꺼짐 방지.
* **최소 대상 기기 / 메모리·드로우콜 예산**: **[TBD]** — 출시 전 확정 필요(GDD §11 리스크 참조). 잠정 기준: 중급 안드로이드(3GB RAM)에서 60FPS 유지.

---

## 2. 코어 아키텍처 — B방식 (카메라 착시 + 중력 회전) [CRITICAL]

> 관련 파일: `Assets/Scripts/WorldRotationController.cs`, `Assets/Scripts/CameraController.cs`

### 2.1 핵심 계약 (Perceptual Contract)
* **미로(Maze)는 물리적으로 회전하지 않는다.** `Maze` 루트의 `Rigidbody2D`는 **Static**, `Transform`은 `(0,0,0)`에 영구 고정. 스크립트로 미로 Transform/회전을 절대 변경하지 않으며 `MoveRotation()`도 쓰지 않는다.
* **회전하는 것은 ① `Physics2D.gravity` 벡터와 ② 카메라뿐이다.**
* **유저 체감**: 미로를 돌리는 것처럼 보이지만, **중력은 항상 화면 기준 '아래'로 유지**되어 공은 언제나 화면 아래로 떨어진다(실물 미로판을 기울이는 직관과 동일).

### 2.2 수식 (검증됨)
`θ` = 현재 보간된 월드 각도(`_currentAngle`), `g = 9.81 × gravityScale`.

```
중력(월드):  Physics2D.gravity = ( −sinθ · g ,  −cosθ · g )      // θ=0 → (0, −g) 정확히 아래
카메라:      transform.rotation = Quaternion.Euler(0, 0, −θ)     // LateUpdate
```

* **합성 결과**: 카메라가 `−θ` 회전하면 뷰에서는 월드가 `+θ` 회전해 보인다. 월드 중력 벡터를 뷰로 변환하면
  `R(θ)·(−sinθ, −cosθ) = (0, −1)` → **모든 θ에서 화면 중력은 정확히 `(0, −g)`(화면 아래)**.
* 즉 공은 항상 화면 아래로 떨어지고, Static 미로는 화면에서 `+θ`만큼 회전한 것처럼 보인다. (Phase 1.5 QA에서 "중력 방향이 화면상 아래와 일치" 검증 완료.)

### 2.3 프레임 책임 분리
| 시점 | 처리 | 이유 |
|---|---|---|
| `WorldRotationController.FixedUpdate()` | `Mathf.LerpAngle`로 각도 보간 → 각속도 클램프 → `Physics2D.gravity` 갱신 → 카메라에 각도 전달 | 물리 갱신은 고정 스텝에서 |
| `CameraController.LateUpdate()` | `transform.rotation = Euler(0,0,−θ)` 적용 | 모든 물리·로직 종료 후 렌더 직전 적용 → Jitter 방지 |

### 2.4 입력 한계치 (터널링·폭주 제어)
* `rotationSmoothness`(기본 15): 목표 각도로의 Lerp 보간 계수.
* `maxAngularSpeed`(기본 400 **deg/s**): **중력/카메라 회전 각도(`_currentAngle`)의 초당 변화 상한.** ⚠️ "미로의 각속도"가 아니다(미로는 Static). 빠른 스와이프 시 중력 방향이 급변해 공이 고속화→터널링되는 것을 막는다.
* 공 자체의 터널링은 §4의 `Continuous`로 별도 방어.

### 2.5 이 방식을 택한 이유
* Kinematic 미로 회전 시 Box2D **표면 속도 전달(Surface Velocity Transfer)** 버그(공이 의도치 않게 튀어오름)를 원천 차단.
* 미로가 매 프레임 움직이지 않으므로 **Static Collider Rebuild 성능 저하**가 발생하지 않음.
* **부작용 주의**: `Physics2D.gravity`는 전역이라 씬의 **모든 Dynamic Rigidbody2D**에 영향을 준다. 물리 영향을 받으면 안 되는 연출 오브젝트는 Dynamic 바디를 쓰지 말 것. UI는 회전 카메라와 분리하기 위해 Canvas를 `Screen Space - Overlay`로 둔다(§9).

---

## 3. 입력 시스템 (Input)

> 관련 파일: `Assets/Scripts/InputController.cs`

* **API**: `UnityEngine.InputSystem.EnhancedTouch`. Legacy Input(`Input.GetMouseButton` 등)은 Unity 6 + New Input System 환경에서 무력화되므로 **사용 금지**(DevelopLog 2026-03-26 참조).
* **드래그 회전**: 화면 빈 공간 터치 후 드래그, `Mathf.Atan2` 각도 변화량 → `WorldRotationController.SetTargetAngle()`.
* **멀티터치 가드**: 처음 닿은 `finger.index`만 추적해 추가 손가락이 닿아도 회전 중심이 튀지 않게 한다.
* **UI 터치 예외**: `EventSystem.current.IsPointerOverGameObject(finger.index)`로 각 터치 슬롯의 UI 점유 여부를 판정해, UI(일시정지 버튼 등) 터치는 회전 조작에서 제외.
* **재시작 시**: `InputController.ResetInput()`으로 `_isUsingTouch=false` 초기화(에디터/마우스 경로의 영구 무시 버그 방지).

---

## 4. 물리 & 충돌 (Physics)

> 관련 파일: `Assets/Scripts/Objects/PlayerBall.cs`, 프리팹 `Assets/Prefabs/PlayerBall.prefab`

* **공(Ball) Rigidbody2D**: `CollisionDetectionMode = Continuous`(터널링 방지) + `Interpolate`(렌더 부드러움). `CircleCollider2D` 반지름 0.4, 크기 ≈ 0.5~1 unit.
* **중력 배율**: `gravityScale`(WorldRotationController 필드, 기본 1.5)로 빠릿한 낙하감 튜닝. 실제 중력 크기 = `9.81 × gravityScale`.
* **마찰·반발은 코드 금지** → `Physics Material 2D`로만 제어. 정적 지형은 `Maze` 루트의 단일 머티리얼로 일괄 튜닝(§6).
* **공-공 충돌 정책**: §5.3에서 정의.

---

## 5. 레이어 & 색상 매칭 (Color System)

> 관련 파일: `Assets/Scripts/Objects/Goal.cs`(enum 정의), `PlayerBall.cs`

### 5.1 데이터
* **`public enum ColorType { Default, Red, Blue, Green, Yellow }`** (Goal.cs에 정의). 색상 매칭은 string 태그 비교 대신 이 enum으로만 수행한다.
* `PlayerBall._colorType`, `Goal._goalColorType`을 `[SerializeField]`로 인스펙터 노출.

### 5.2 선택적 통과(Color Gate) — Layer Collision Matrix
* 색상별 통과/차단은 코드 `IgnoreCollision`을 최소화하고 **Layer Collision Matrix**(Project Settings → Physics 2D)로 오프라인 세팅한다.
* **확정: 역할 분담형 하이브리드 (2026-07-06)** — "브로드 물리 필터는 레이어, 파인 정체성 판정은 코드".
  * **Color Gate 통과/차단 → 레이어**: 고체 통과(phase-through)는 브로드페이즈에서만 깨끗이 결정된다(`OnCollisionEnter`는 이미 막힌 뒤 호출되어 되돌릴 수 없음). 런타임 상태 0 → **Fast Retry 무부담**, GC 0, 진짜 강체 손맛.
  * **Goal 정답 판정 → 코드(`ColorType` enum) 유지**: 이건 선택적 충돌이 아니라 "정체성 판정 + 오브젝트 제거"다. Solid 콜라이더가 오답=벽을 무료로 주고 코드는 정답 공 제거(`SetActive(false)`)만 담당(현행 `Goal.cs`가 이미 최적). **레이어로 옮기지 말 것**.
* **게이트 방향 = 같은 색 통과**(GDD §5.1.1): Matrix에서 **`Gate_X`가 `Ball_X`만 무시(통과)**, 나머지 공 레이어와는 **충돌 ON**(차단). 모든 **공-공 교차는 ON 유지**(§5.3).
* ⚠️ **레이어 예산 주의**: Unity 레이어는 32개(일부 예약)뿐이다. `4색 × (공·게이트…)`를 전부 개별 레이어로 분리하면 예산이 빠르게 고갈된다. 계획된 4색이면 공4+게이트4=8레이어로 가용 범위 내(색이 6~7 초과 시 게이트를 `IgnoreCollision` 사전세팅 코드로 폴백 검토).
* **현재 씬(2026-07-06)**: `Ball_Yellow/Ball_Green/Gate_Yellow/Gate_Green` 레이어는 생성돼 있으나 공은 `Default(0)`에 있고 Matrix 미배선(게이트 기믹 미구현). **제거하지 말고 유지** → Phase 4 게이트 구현 시 공을 색 레이어로 이동하고 위 매트릭스로 배선.

### 5.3 공-공 충돌 정책 — **확정: 모든 공이 서로 충돌**
* 색에 관계없이 **모든 공이 물리적으로 충돌**한다(핀볼식 상호 간섭). 레벨 설계 영향은 GDD §5.3.
* **구현 — Layer Collision Matrix 주의**:
  * 가장 단순한 방법: **모든 공을 하나의 `Ball` 레이어**에 둔다 → 공끼리는 항상 충돌. 이때 색상 게이트 통과/차단은 게이트의 충돌 콜백에서 `ColorType` enum으로 판정한다(공↔게이트 필터링을 코드로).
  * 색상별로 공 레이어를 분리(게이트 필터링을 Matrix로 처리)할 경우, **모든 공 레이어 쌍(동색·이색 포함)의 교차를 ON으로 유지**해야 공끼리 충돌이 보존된다. 색 분리를 도입하면서 공-공 교차를 끄지 않도록 주의.

---

## 6. 레벨 오소링 파이프라인 (Level Authoring)

* **지형 제작**: 2D **Tilemap** + 2D **SpriteShape**(곡선·동굴·대각 경사 등 비정형 지형).
* **콜라이더 병합**: 모든 **정적** 콜라이더(Tilemap / SpriteShape / Polygon)는 최상위 `Maze`의 **`CompositeCollider2D`에 병합**(`Used By Composite` 체크). → 공이 이음매 없이 부드럽게 구르고, **단일 `Physics Material 2D`** 로 전체 맵 마찰·반발을 일괄 제어.
* **동적 기믹은 절대 병합 금지**: 문(Door)·파괴 블록 등 런타임에 상태가 변/파괴되는 기믹은 `Used By Composite`에 넣지 않는다(런타임 Collider Rebuild로 인한 Lag Spike 방지). 독립 `BoxCollider2D` 등을 사용.

### 6.1 SpriteShape 병합 절차 — **실측 검증됨 (2026-07-30)**

씬 `SampleScene`의 `MazeGrid/Maze`(Static RB + `CompositeCollider2D`, `geometryType = Polygons`)에서 확인한 순서:

1. SpriteShape 오브젝트를 **`Maze`의 자식**으로 둔다(부모의 Rigidbody2D에 콜라이더가 귀속되어야 병합 대상이 된다).
2. **스플라인을 닫는다**(`isOpenEnded = false`). 닫힌 스플라인은 `PolygonCollider2D`, 열린 스플라인은 `EdgeCollider2D`로 베이크되는데 **`Polygons` 지오메트리 Composite는 Polygon만 병합**한다.
3. `PolygonCollider2D`를 추가한다. `SpriteShapeController.autoUpdateCollider = true`면 스플라인이 콜라이더 `points`로 자동 베이크되고 `hasCollider`가 `true`가 된다.
4. 콜라이더의 `Used By Composite`(= `compositeOperation = Merge`)를 켠다.
5. 자식 콜라이더에는 **`Physics Material 2D`를 할당하지 않는다**(`sharedMaterial = null`). 마찰·반발은 Composite에 걸린 단일 `Wall Physics Material`이 일괄 지배한다.

* **검증 지표(성공 판정)**: 병합 성공 시 `Maze.CompositeCollider2D`의 `shapeCount` **15 → 16**, `pathCount` **2 → 3**, `pointCount` **32 → 37**로 증가하고 `bounds`가 해당 도형을 포함하도록 확장된다. 자식 콜라이더는 `compositeCapable = true`, `attachedRigidbody = Maze`가 된다.
* ⚠️ **`Assets/Prefabs/`에 두지 말 것**: SpriteShape 프로필은 `Assets/SpriteShapes/`에 둔다(2026-07-30 정규화).

### 6.2 동적 기믹 콜라이더 규칙 — **실측 검증됨 (2026-07-30)**

* `Maze` 자식 + `Used By Composite` **미사용**이면 Composite `shapeCount`는 **불변**(16 유지)이고, 해당 콜라이더는 자체 `shapeCount = 1`의 독립 도형으로 남는다. → 병합 회피는 의도대로 동작한다.
* ⚠️ **단, 이것만으로는 부족하다 (신규 발견)**: `Used By Composite`를 꺼도 자식 콜라이더의 `attachedRigidbody`는 여전히 **부모 `Maze`의 Static 바디**로 잡힌다. 이 상태로 문을 움직이면 **Static 바디의 콜라이더가 변형되어 Static Collider Rebuild가 발생** — 병합을 피한 목적 자체가 무산된다.
* **필수 조건**: 동적 기믹에는 **자체 `Rigidbody2D`(Body Type = `Kinematic`)** 를 붙인다. 붙이는 즉시 `attachedRigidbody`가 자기 자신으로, `composite`가 `null`로 분리되는 것을 확인했다.
* **정리 — 동적 기믹 3종 세트**: ① 독립 콜라이더(`Used By Composite` 미체크), ② 자체 `Rigidbody2D` = `Kinematic`, ③ Fast Retry 시 상태 복원(§7.5).

---

## 7. 게임 상태 & Fast Retry (State Management)

> 관련 파일: `Assets/Scripts/Managers/GameManager.cs`, `Objects/Goal.cs`, `Objects/DeadZone.cs`, `Objects/PlayerBall.cs`

### 7.1 상태 머신
* **`enum GameState { Play, Pause, GameOver, Clear }`**. `GameManager`가 단일 인스턴스(Singleton)로 보유.
* 실패 시 **`SceneManager.LoadScene()` 절대 금지** → Fast Retry로 상태만 초기화.

### 7.2 승리 판정 (멀티볼 카운터)
* `Awake`에서 `FindObjectsByType<PlayerBall>`로 전체 공 수 `_totalBallsCount` 캐싱.
* 정답 색 공이 Goal에 닿으면 `Goal.OnCollisionEnter2D` → `GameManager.OnBallReachedGoal()`로 `_reachedBallsCount++`, 직후 해당 공 `SetActive(false)`(파티클과 함께 즉시 비활성화 → 잔여 물리/경로 방해 차단).
* **`_reachedBallsCount >= _totalBallsCount`** 일 때만 `Clear` → 1.5초 코루틴(`CLEAR_DELAY_TIME`) 후 분기.

### 7.3 Goal 충돌 처리
* `Goal`은 **Trigger가 아닌 Solid 콜라이더**, `OnCollisionEnter2D` 사용.
  * 정답 색: 골인 처리.
  * **오답 색: 골인 무시 → 물리적으로 그냥 튕겨나감(벽처럼 동작).**

### 7.4 DeadZone (장외 이탈) — **판정 확정**
* `DeadZone.OnTriggerEnter2D` + 태그 `"Player"` → `GameManager.GameOver()`.
* **확정 의미**: 공이 데드존 트리거에 **진입(Enter)하면 즉시 실패.** (구 GDD의 "닿거나 *벗어나는*"이라는 모순 표현을 **'진입'으로 단일화.**)
* ⚠️ **배치 제약**: enter 기반이므로 데드존 콜라이더는 **공의 시작/정상 플레이 영역과 겹치면 안 된다**(겹치면 시작과 동시에 즉시 게임오버). 미로 **바깥을 두르는 외곽 킬 영역**(예: 사방 프레임)으로 배치해, 공이 벽을 뚫고 이탈했을 때만 진입하도록 구성.
  * → 에디터 검증 항목은 부록 체크리스트 참조.

### 7.5 Fast Retry 초기화 범위 — **완전 정의**
`GameManager.FastRetry()`가 **반드시** 되돌려야 하는 상태 전부:

| 대상 | 처리 | 구현 |
|---|---|---|
| 코루틴 | `StopAllCoroutines()` | ✅ 구현됨 |
| 상태/카운터/타이머 | `State=Play`, `_reachedBallsCount=0`, `_playTimer=0` | ✅ |
| 모든 공 | `PlayerBall.FastReset()` — 위치/회전 복원, **`linearVelocity`·`angularVelocity`를 0으로** 강제(벽 뚫기 방지), Goal로 비활성화된 공 **재활성화** | ✅ |
| 입력 | `InputController.ResetInput()` | ✅ |
| 월드 회전 | `WorldRotationController.FastReset()` — `_targetAngle=_currentAngle=0`, `ApplyGravity(0)`, 카메라 0도 | ✅ |
| **동적 기믹 상태** | 열린 문 닫기 / 부서진 Fragile Block 복원 / 눌린 Switch 해제 | ⛔ **미구현(Phase 4+ 추가 시 필수)** |
| **풀 오브젝트** | 잔여 파티클·SFX를 PoolManager로 반환 | ⛔ **미구현(§8)** |

* **Jitter 방지 규칙**: 활성 상태 공은 `transform`이 아닌 `_rb.position`/`_rb.rotation`에 직접 대입. **비활성 상태**(골인 후) 공은 `_rb.position`이 무시되므로 먼저 `transform`으로 이동→`SetActive(true)` 순서(코드 주석 참조).

### 7.6 승/패 동시 발생 우선순위 — **규칙 정의**
* **규칙: 패배 우선.** 동일 물리 스텝에서 "마지막 공 골인(승)"과 "다른 공 데드존/Spike 진입(패)"이 함께 일어나면 **패배로 처리**한다(공을 끝까지 지키지 못한 것으로 간주).
* **현재 구현 한계**: `OnBallReachedGoal()`·`GameOver()` 모두 `if (State != Play) return` 가드로 보호되어 **먼저 호출된 콜백이 이긴다(first-event-wins)** — 두 콜라이더의 콜백 순서는 비결정적.
* **엄격 적용이 필요할 때**: 골인 판정을 즉시 Clear로 확정하지 말고 해당 프레임 종료(`yield return new WaitForFixedUpdate` 또는 다음 스텝)까지 보류 후 패배 이벤트 부재를 확인하고 확정. (현재는 단순 가드로 충분하다고 판단되면 보류 가능.)

---

## 8. 피드백 & 풀링 (Feedback & Pooling)

> 관련 파일: `Assets/Scripts/Managers/FeedbackManager.cs`

* **충돌 피드백 쿨타임**: `Time.time` 기반 **0.1초 내부 쿨타임**을 강제(`PlayHaptic(intensity)`). 조밀한 골목·고속 회전 시 1초에 수십 회 충돌해도 진동 모터 폭주/사운드 깨짐을 방지.
* **충돌 강도 연동**: 충돌 상대속도(충격량)에 비례해 햅틱 세기·SFX 볼륨 스케일.
* **오브젝트 풀링(PoolManager)**: 파티클·SFX는 `Instantiate/Destroy` 대신 풀 활성/비활성으로 재사용(GC 부하↓, 60FPS 유지). → **현재 미구현, Task Phase 7 예정.** 도입 시 Fast Retry(§7.5)의 "풀 반환"과 연결.

---

## 9. 카메라 & 해상도 (Camera & Resolution)

> 관련 파일: `Assets/Scripts/CameraController.cs`

* **고정 뷰(Orthographic)**. `Awake`의 `AdjustCameraViewport()`가 `Screen.width/height` 비율을 기준 비율(`targetWidth/targetHeight`, 기본 1080×1920)과 비교해, 세로로 더 긴(좁은) 화면에서 `orthographicSize`를 확대해 맵이 잘리지 않게 보정.
* **[회전 클리핑 방지]**: 직사각형 화면에서 미로가 (시각적으로) 회전할 때 모서리가 잘리지 않도록, 가로/세로가 아니라 **미로의 대각선(외접원 반지름)** 기준으로 `orthographicSize`를 넉넉히 설정. → `defaultOrthoSize`를 해당 기준으로 세팅.
* **UI 분리**: Canvas `Render Mode = Screen Space - Overlay`(회전 카메라에 HUD가 함께 돌아가지 않도록). 회전 배경 연출이 필요하면 해당 스프라이트를 Main Camera **자식**으로 둔다.
* **UI 스케일러**: `Canvas Scaler = Scale With Screen Size`, `Reference Resolution 1080×1920`, `Match = 0.5`.
* **Safe Area**: 최상단 UI 패널에 노치 대응 스크립트 부착(Task Phase 7).

---

## 10. 데이터 영속화 (Persistence)

> 관련 파일: `Assets/Scripts/Data/StageData.cs`

* **스테이지 데이터(`StageData : ScriptableObject`)**: `LevelID`, `TimeLimitFor3Stars`(기본 15s), `TimeLimitFor2Stars`(기본 30s). 별점은 `GameManager.CalculateStars()`가 `_playTimer`와 비교해 산출(이하 3별 / 2별 / 그 외 1별). 메뉴: `Labyrinth/StageData`.
  * **현재 로드 경로(2026-07-30)**: 인스턴스 `Assets/Data/Stage 1.asset`(LevelID 1 / 15s / 30s)을 `GameManager._currentStageData`에 **인스펙터 주입**한다. 1씬 = 1스테이지 구조이므로 이것이 곧 로드 시스템이다. `_currentStageData`가 `null`이면 `CalculateStars()`는 **무조건 1별**을 반환하므로(무음 폴백) 씬 셋업 시 참조 연결을 반드시 확인할 것.
  * **확장 시점**: 스테이지 목록·해금 상태를 다루는 레지스트리/`StageLoader`는 로비·월드맵이 생기는 **Phase 5**에서 도입한다. 그 전에 만들면 사용처 없는 추상화가 된다.
* **세이브 데이터**: 별 기록·클리어 타임·해금 진행·보유 화폐·구매 스킨을 로컬(PlayerPrefs 또는 JSON)에 저장. **[구현 예정]**
  * 스키마 버전 필드 포함(마이그레이션 대비), 간단 암호화/체크섬으로 변조 방지.
  * 별(성취 기록)과 소프트 화폐는 **별도 필드**로 저장(분리 근거는 GDD §7).
  * 클라우드 세이브: **[TBD]**.

---

## 11. 스크립트 아키텍처 & 현황 (Modules)

* **모듈 분리 원칙**: 기능을 한 스크립트에 뭉치지 않고 책임별 Manager/Controller로 분리. 코딩 컨벤션(캐싱·GC 금지·이벤트 해제 등)은 **`CLAUDE.md §4`** 를 단일 출처로 따른다.

| 스크립트 | 경로 | 역할 | 상태 |
|---|---|---|---|
| `Bootstrapper` | `Scripts/` | 60FPS·화면 꺼짐 방지 초기화 | ✅ |
| `InputController` | `Scripts/` | EnhancedTouch 입력 → 목표 각도 | ✅ |
| `WorldRotationController` | `Scripts/` | 각도 보간 + `Physics2D.gravity` 갱신 + 카메라 통지 | ✅ |
| `CameraController` | `Scripts/` | 해상도 보정 + LateUpdate 역회전 | ✅ |
| `GameManager` | `Scripts/Managers/` | FSM, 승/패, Fast Retry, 별점 | ✅ |
| `FeedbackManager` | `Scripts/Managers/` | 햅틱/SFX 쿨타임 피드백 | ✅ |
| `PlayerBall` | `Scripts/Objects/` | 색상·초기상태·FastReset | ✅ |
| `Goal` | `Scripts/Objects/` | 색상 매칭 골인/벽 처리 | ✅ |
| `DeadZone` | `Scripts/Objects/` | 장외 이탈 실패 | ✅ |
| `Obstacle` | `Scripts/Objects/` | 장애물(Spike 류) 트리거 | ✅ |
| `StageData` | `Scripts/Data/` | 스테이지 SO 데이터 | ✅ |
| `PoolManager` | (미정) | 파티클·SFX 풀링 | ⛔ 예정 |

---

## 부록. 에디터 셋업 검증 체크리스트

코드만으로 보장되지 않는 에디터 설정. 씬 작업 후 확인:

- [ ] `Maze` 루트 `Rigidbody2D` = **Static**, Transform = `(0,0,0)`.
- [ ] 정적 지형 콜라이더 전부 `Used By Composite` 체크, 동적 기믹은 **미체크**.
- [ ] SpriteShape 지형: `Maze` 자식 배치 + **스플라인 닫힘** + `PolygonCollider2D` + `Used By Composite` 체크 + 자체 머티리얼 **미할당** (§6.1).
- [ ] 동적 기믹: 독립 콜라이더 + **자체 `Rigidbody2D` = `Kinematic`** (§6.2 — 없으면 Static Rebuild 발생).
- [ ] `Maze`에 단일 `Physics Material 2D` 할당(마찰/반발 일괄).
- [ ] PlayerBall: `Continuous` + `Interpolate`, `Physics Material 2D` 할당, `ColorType` 지정.
- [ ] **DeadZone 콜라이더가 공의 시작/플레이 영역과 겹치지 않음**(겹치면 즉시 게임오버 — §7.4).
- [ ] Goal: Solid 콜라이더(Trigger 아님), `ColorType` 지정.
- [ ] Layer Collision Matrix: 색상별 공-게이트/공-공 정책 설정(§5).
- [ ] UI Canvas = `Screen Space - Overlay`, Scaler 1080×1920 / Match 0.5.
- [ ] `GameManager`에 `WorldRotationController`·`InputController`·`StageData` 레퍼런스 연결.
- [ ] **모든 설정을 마친 후 Scene을 저장(Ctrl+S)** 한다.
