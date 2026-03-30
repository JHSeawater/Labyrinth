# GDD (Game Design Document)
Project: Labyrinth (2D Rotating Maze)

## 1. 게임 개요 (Game Overview)
* **장르**: 2D 물리 퍼즐 (모바일 아케이드)
* **핵심 목표**: 유저가 스마트폰 화면을 드래그하여 미로 전체를 회전시키고, 오직 '중력'에 의존하여 공(Ball)을 무사히 탈출구(Goal)로 이동시키는 게임.
* **주요 차별점 (USP)**: 후반부 스테이지에서 등장하는 **'다중 색상 공(Multi-color Balls)과 전용 기믹'**을 통한 고도의 공간 지각 퍼즐.

## 2. 핵심 게임 루프 및 예외 규칙 (Core Gameplay & Rules)
1. **스테이지 진입**: 맵 전체를 조망할 수 있는 **고정 뷰(Fixed Camera View)** 제공.
2. **조작 및 물리 적용**: 화면 드래그로 미로의 각도를 조정하면 중력에 의해 공이 굴러감.
3. **학습 및 응용 (Level Progression)**:
   - **초반부 (학습 구간, 단일 공)**: 기본 회전 조작과 가속도/마찰력 적응.
   - **후반부 (응용 구간, 다중 색상 공)**: 2개 이상의 서로 다른 색깔 공 등장. 각 색상에 맞는 탈출구와 전용 장애물(특정 색상만 통과 가능/타 색상 닿으면 실패)을 물리/레이어 연산으로 해결하는 두뇌 플레이.
4. **승리 및 패배 조건 (다중 공 포함)**:
   - **승리**: 맵 상의 **모든 공**이 각자의 색상에 맞는 Goal에 들어가야 스테이지 클리어. (먼저 도달한 공은 파티클 이펙트와 함께 **즉시 비활성화(Disable)** 처리하여 물리 연산 및 잔류 경로 방해를 차단함)
   - **패배**: 단 **하나의 공이라도** Hole이나 Spike에 빠지거나 닿으면, 다른 공의 상태와 무관하게 즉시 **Game Over (재시작)** 처리.
   - **오답 처리 (예외 규칙)**: 붉은 공이 푸른 Goal에 닿는 등 색상이 일치하지 않는 Goal에 도달할 경우, 해당 Goal은 단순한 물리적 '벽(Wall)'으로 취급되어 공이 튕겨 나감.
   - **빠른 재시작 (Fast Retry)**: 퍼즐 게임 특유의 쾌적한 반복 플레이를 위해 패배 시 Scene 전체를 다시 로드하지 않고, 오브젝트(공, 미로)의 물리 상태와 Transform을 초기 상태로 즉시 되돌리는(Reset) 방식을 채택. (위치 대입 시 **물리 가속도(`velocity`, `angularVelocity`)를 반드시 0으로 초기화**하여 벽 뚫기 현상을 방지함)
5. **결과 보상**: 클리어 시간(Time)에 따라 별(Star) 1~3개 획득. 다음 레벨 잠금 해제.

## 3. 조작 방식 및 시점 (Controls & Camera)
* **조작 (모바일 터치 드래그)**: 
  - **입력 시스템**: `New Input System (EnhancedTouch)`을 사용하여 화면 빈 공간을 터치한 상태로 원형 또는 횡/종으로 드래그하면, 각도(`Mathf.Atan2`) 변화량을 감지해 미로가 매끄럽게 회전함.
  - **물리 구현 방식 (B방식: 카메라 착시 + 중력 회전)**: 미로 오브젝트는 `Transform(0,0,0)`으로 완전 고정(Static Collider)되며, `Physics2D.gravity` 방향 회전과 카메라 역방향 회전을 동기화하여 유저 눈에는 미로가 도는 것처럼 보이는 착시 효과를 구현. 이 방식은 Kinematic 회전 시 발생하는 Box2D 표면 속도 전달(Surface Velocity Transfer) 버그와 Static Collider Rebuild 성능 저하를 근본적으로 차단함.
* **UI 터치 충돌 방지 (EventSystem)**:
  - 터치 입력 처리 시 유니티의 EventSystem을 활용(`IsPointerOverGameObject`), 특히 멀티터치 상황을 고려해 **`finger.index`**를 매개변수로 전달하여 일시정지 버튼 등 **UI를 터치한 경우에는 미로 회전 조작이 무시**되도록 완벽히 예외 처리함.
* **물리 엔진 폭주 제어 (조작 한계치)**:
  - 유저의 비정상적으로 빠른 스와이프 입력 시 발생하는 터널링(벽 뚫기) 현상을 막기 위해, 미로의 **최대 회전 속도(Max Angular Velocity)에 상한선**을 둠.
  - 드래그 입력값과 실제 미로 오브젝트 사이에 보간(Lerp / Damping)을 적용.
* **시점 (Fixed Camera View) 및 스마트폰 화면 비율 대응 (Aspect Ratio)**:
  - 맵 전체를 한 화면에 보여주는 고정 뷰 방식을 채택. 
  - 단말기마다 다른 화면 비율(예: 아이패드 4:3 ~ 최신 폴드/폰 19.5:9 등)에 대응하기 위해, 기기의 화면 비율을 계산하여 `Camera.main.orthographicSize`를 동적으로 조절함.
  - **[회전 클리핑 방지]**: 직사각형 화면에서 미로가 회전할 때 모서리가 잘리는 현상을 막기 위해, 미로의 가로/세로 길이 대신 **미로의 대각선 길이(외접원 반지름)**를 기준으로 카메라 크기를 넉넉하게 설정하여 모든 각도에서 시야를 확보함.

## 4. 레벨 디자인 맵 요소 (Level Elements)
* **레벨 제작 방식 (Level Authoring Method)**: 
  - 각 스테이지의 미로는 유니티의 **2D Tilemap 시스템**과 **2D SpriteShape**를 병합하여 제작함. 
  - **[Sprite Shape 활용]**: 곡선형 경로, 유기적인 동굴 형태, 대각선 경사면 등 비정형 지형 구현에 적극 활용함.
  - **[CompositeCollider2D 기반 병합]**: 모든 개별 콜라이더(Tilemap, SpriteShape, PolygonCollider)는 최상위 `Maze` 오브젝트의 **`CompositeCollider2D`에 병합(`Used By Composite` 필수 체크)**하여 관리함.
  - **[물리 속성 일괄 제어]**: 병합된 구조 덕분에 최상단 `Maze` 오브젝트에 할당된 **단 하나의 `Physics Material 2D` 수정만으로 전체 맵의 마찰력과 반발력을 일괄적으로 튜닝**할 수 있어 레벨 밸런싱 효율을 극대화함.

* **Player (Ball)**: 중력의 영향을 받는 주인공 오브젝트. 
  - *물리 최적화*: 원활한 Box2D 연산을 위해 크기는 0.5~1 Unity Unit 내외로 설정하며, 고속 회전 시 벽 뚫기(Tunneling) 방지를 위해 `Rigidbody2D.CollisionDetectionMode`를 **Continuous**로 필수 설정.
  - *물리 매터리얼(Physics Material 2D)*: 공이 벽에 닿았을 때 미끄러지는 마찰력(Friction)을 0으로 두어 매끄럽게 구르도록 하고, 퍼즐 게임 특유의 조작감을 위해 반발력(Bounciness)을 세밀하게 튜닝함.
  - *중력 튜닝*: 모바일 아케이드 특유의 '빠릿한' 낙하감을 위해 Physics2D 기본 중력(-9.81) 배율을 더 강하게 조정하여 튜닝.
* **Goal (탈출구)**: 공이 도달해야 하는 지점. 특정 색상 공만 인식하는 전용 Goal 추가.
* **Wall (벽)**: 일반적인 물리 충돌 지형.
* **Color Gate/Filter (색상 선택적 장애물)**: 다중 공 기믹 시 코드 기반 물리 무시(`IgnoreCollision`) 처리를 최소화하고, 유니티의 **Layer Collision Matrix(레이어 충돌 매트릭스)**를 오프라인 세팅하여 각 색상별 공과 게이트 간 물리적 통과/충돌 처리를 엔진 연산 레벨에서 최적화함.
* **데이터 구조 (Enum 활용)**: 다중 색상 공과 Goal, 장애물을 매칭할 때 오타나 버그가 발생하기 쉬운 문자열(String 태그) 비교를 배제하고, 반드시 C#의 **Enum (`public enum ColorType { Red, Blue, Green, Yellow }`)**을 사용하여 속성을 안전하고 확장성 있게 매칭함.
* **Hole, Spike & Dead Zone (함정 및 장외 이탈)**: 닿으면 즉시 게임 오버.
  - **[Dead Zone (OOB)]**: 물리 엔진의 한계로 공이 벽을 뚫고 미로 밖으로 나가는 현상을 대비해, 미로 전체를 감싸는 거대한 트리거(IsTrigger=true) 영역을 배치함. 공이 이 영역에 닿거나 벗어나는 순간 즉시 패배(Fast Retry) 처리하여 소프트락(Soft-lock)을 방지함.
* **확장 기믹 (Level Scaling Gimmicks)**: 퍼즐의 깊이와 변수를 만들기 위해 후반부 맵에 다음 요소를 투입함.
  - **[물리 주의]**: 문(Door), 파괴 블록 등 **런타임에 상태가 변하거나 파괴되는 동적 기믹은 절대 `CompositeCollider2D`에 병합(`Used By Composite`)하지 않음**. 독립적인 `BoxCollider2D` 등을 사용하여 런타임 콜라이더 재구성(Rebuild)으로 인한 렉(Lag Spike)을 방지함.
  - **스위치와 문 (Switch & Door)**: 맵 외곽의 스위치(또는 열쇠)에 먼저 닿아야 막혀있던 핵심 경로가 열리는 동선 설계.
  - **가속 파괴 블록 (Fragile Block)**: 공이 일정 가속도 이상으로 강하게 부딪혀야만 부서지는 벽 (강한 타격감 유발).
  - **범퍼 (Bumper)**: 닿는 순간 반대쪽으로 강하게 튕겨내는 핀볼 방식의 탄성 기믹.

## 5. UI/UX 및 데이터 수집 (UI Flow & Data)
* **주사율 확보 (Target Frame Rate)**: 잦은 시점 회전과 정밀한 물리 퍼즐 조작감이 생명인 만큼 구동 시 `Application.targetFrameRate = 60;` (또는 120 이상)을 코드로 명시하여, 모바일 기본값인 30FPS 제한을 풀고 쾌적한 퍼포먼스를 강제 유지함.
* **UI 해상도 대응 (Canvas Scaler)**: 기기 화면 비율 대응과 발맞추어, UI 캔버스의 **`Canvas Scaler` 컴포넌트를 `Scale With Screen Size` 모드로 설정**하여 다양한 해상도(아이패드~폴드)에 완벽 대응함.
   - *세부 설정치*: `Reference Resolution`은 세로형 모바일 스탠다드인 **1080 x 1920**으로 고정하고, `Match` 항목은 가로/세로 비율에 따라 동적 조절되도록 **0.5**로 설정하여 UI가 찌그러지지 않도록 설계.
* **접근성 (Colorblind 지원)**: 다중 색상 공 기믹 시 색상 구분이 어려운 색약/색맹 유저를 위해, 각 공과 Goal의 스프라이트 내부에 고유한 물리적 문양(예: 붉은공=별, 푸른공=세모)을 각인하거나 별도의 색약 모드 토글을 지원함.
* **메인 로비 (Lobby)**: 스테이지 선택 (월드맵 형태, 달성한 별 개수 표시).
* **인게임 HUD (HUD)**: 흐르는 시간(Timer) 표시, 일시정지(Pause) 버튼.
* **결과 화면 (Results)**: 최종 타임 기록, 획득한 별 랭크 표시, [재시작] / [다음] / [로비] 버튼.
* **메타 게임 및 수익화 (Meta Game & BM)**: 
   - **스킨 시스템 (보상)**: 플레이로 누적한 별(★)을 소모하여 게임의 시각적 재미를 더하는 공의 궤적(Trail), 터지는 파티클, 혹은 미로 테마(스킨)를 해금하는 상점 시스템 구축.
   - **합리적 BM (Rewarded Ad)**: 흐름을 끊는 강제 팝업 광고는 지양하고, "스킨 구매 시 별이 부족할 때 동영상 시청 후 별 추가 획득" 형태의 자발적 보상형 광고를 주력 BM으로 채택하여 유저 경험과 수익을 동시에 잡음.
* **데이터 저장 (Save)**: 별 획득 데이터와 클리어 타임은 로컬 디바이스(PlayerPrefs 또는 JSON)에 암호화하여 저장.

## 6. 오디오 및 피드백 (Audio & Haptics)
* **충돌 피드백 (Collision)**: 공이 벽에 부딪힐 때 충돌 속도(충격량)에 비례하여 기기에 약한 햅틱 진동(Haptic Feedback)을 주고, 둔탁한 효과음을 발생.
* **피드백 쿨타임 (Cooldown)**: 공이 구석의 조밀한 골목에 끼이거나 미로의 고속 회전으로 인해 1초에 수십 번 연속으로 충돌할 때, 사운드가 깨지거나 진동 모터가 폭주(유저에게 불쾌감 유발)하는 것을 막기 위해 충돌 피드백 발생 시 아주 짧은 내부 쿨타임(예: 0.1초)을 두어 쾌적함을 유지함.
* **오브젝트 풀링 (Object Pooling)**: 빈번한 재시작과 충돌이 발생하는 게임 특성상, 파티클(성공/충돌 이펙트)과 효과음(SFX)은 `Instantiate/Destroy` 방식이 아닌 **오브젝트 풀링** 방식을 채택하여 모바일 기기의 가비지 컬렉션(GC) 부하를 최소화하고 안정적인 60FPS를 유지함.
* **클리어 피드백 (Clear)**: 공이 Goal에 무사히 들어갔을 때 경쾌한 성공음(SFX)과 시각적 파티클(Particle) 효과 재생.

## 7. 스크립트 아키텍처 원칙 (Architecture & Modularization)
* **스크립트 역할 분리 (모듈화)**: 하나의 스크립트에 기능이 뭉치는 스파게티 코드를 방지하기 위해 기능별 Manager 구조를 도입하여 책임을 명확히 분리함. (예시: `InputController`, `WorldRotationController`, `CameraController`, `GameManager`, `PoolManager`)
* **물리 회전 구현 방식 (B방식: 카메라 착시)**:
  - 미로의 `Transform`은 항상 `(0,0,0)`으로 고정하여 Static Collider로 유지함. `Rigidbody2D.MoveRotation()` 방식을 사용하지 않음.
  - `WorldRotationController.FixedUpdate()`에서 각도 보간(`LerpAngle`) 및 `Physics2D.gravity` 방향 갱신을 처리함.
  - 카메라 `transform.rotation` 적용은 반드시 **`CameraController.LateUpdate()`**에서 `-angle`로 역회전하여, 모든 물리 연산 이후 Jittering 없이 렌더링되도록 보장함.
* **데이터 구조화 (ScriptableObject)**: 미로 조작감(최대 회전 속도, 보간 수치)이나 중력, 반발력 등 잦은 밸런싱이 필요한 주요 수치들은 스크립트에 하드코딩하지 않고, `ScriptableObject` 형태(`GameSettings.asset` 등)로 분리하여 기획자가 인스펙터 창에서 즉각 튜닝하고 모바일 타겟 플랫폼을 빌드 테스트할 수 있는 기반을 구축함.
