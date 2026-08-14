---
name: qa-scene
description: TDD.md 부록 "에디터 셋업 검증 체크리스트"를 UnityMCP로 전수 실측하고 통과/실패 표를 보고한다. 읽기 전용 — 문제는 보고만 하고 수정하지 않는다. 씬 작업을 마친 날 또는 Phase QA 시점에 실행.
---

# /qa-scene — 씬 셋업 전수 실측

## 원칙

- **체크리스트의 단일 출처는 `TDD.md` 부록이다.** 이 스킬에 항목을 복제해 두지 않는다 — 실행 시점에 TDD.md 부록을 다시 읽어 최신 목록을 기준으로 검증한다(문서가 갱신되면 이 스킬은 자동으로 따라간다).
- **읽기 전용**: `manage_scene`(get_hierarchy) · `manage_components`(조회) · `execute_code`(조회성 코드) · `read_console` 등 조회만 사용한다. 문제를 발견해도 수정하지 않고 보고만 한다(CLAUDE.md §2 승인 대기 원칙). 씬을 dirty 상태로 만들지 않는다.

## 절차

1. `TDD.md` 부록 체크리스트를 읽는다.
2. UnityMCP로 에디터 상태를 확인한다. 플레이 모드 중이면 중단하고 사용자에게 알린다.
3. 각 항목을 MCP로 실측한다. 아래 "검증 레시피"에 있는 항목은 그 방법을 따른다.
4. 결과를 표로 보고한다: `| 항목 | 기대값 | 실측값 | 판정 |`
5. 실패 항목은 실측 근거와 함께 요약하고, 수정 계획은 별도로 제시해 승인을 기다린다.

## 검증 레시피 (까다로운 항목)

- **Maze 고정**: Maze 루트 `Rigidbody2D.bodyType == Static`, `transform.position == (0,0,0)`, `rotation == 0`.
- **Composite 병합**: 정적 지형 자식 콜라이더는 `usedByComposite == true` + `attachedRigidbody == Maze` + `sharedMaterial == null`. 동적 기믹은 `usedByComposite == false` **그리고 자체 Kinematic `Rigidbody2D` 보유**(TDD §6.2 — 없으면 Static Rebuild 발생).
- **DeadZone 프레임**: 콜라이더 4개 전부 `isTrigger == true`, Offset/Size가 TDD §7.4 표와 일치, 안쪽 경계가 미로 반폭보다 큰지(겹침 시 즉시 게임오버) 계산으로 확인.
- **카메라**: `defaultOrthoSize ≥ R / targetAspect`(TDD §9 수식, R = 미로 외접원 반지름). 컴포넌트 저장값과 런타임 덮어쓰기 값의 괴리(Task Phase 5 미결 항목)도 함께 보고.
- **Layer Collision Matrix**: 런타임 API 대신 `ProjectSettings/DynamicsManager.asset`의 `m_LayerCollisionMatrix` 비트를 직접 디코딩해 같은 색 `Ball_X × Gate_X`만 OFF인지 확인(Task.md Phase 8 QA에서 쓴 방식).
- **인스펙터 배선 non-null**: `GameManager`의 참조 필드들과 HUD/ResultPopup의 `[SerializeField]` 배선을 `execute_code`로 조회.

## 한계 (보고서에 명시할 것)

플레이 모드 상호작용이 필요한 항목(터치 입력, 일시정지 실동작, 충돌 경로 등)은 이 스킬로 판정할 수 없다. 해당 항목은 "사용자 실플레이 필요"로 분류하고, 사용자가 직접 확인할 시나리오를 `- [ ]` 체크리스트로 만들어 보고서 끝에 붙인다.
