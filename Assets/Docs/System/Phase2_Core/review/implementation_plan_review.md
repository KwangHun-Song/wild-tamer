# Phase 2: 코어 시스템 - 구현 계획 리뷰

## 리뷰 대상 문서

| 문서 | 경로 |
|------|------|
| `implementation_plan.md` | `Assets/Docs/System/Phase2_Core/implementation_plan.md` |

---

## 구현 컨벤션 충족도

| 항목 | 충족 | 비고 |
|------|------|------|
| 1. 기술 스택 | X | 섹션 자체가 없음 |
| 2. 시스템 설계 (클래스 목록, 책임, 의존성) | O | 41개 파일 목록 + 단계별 파일 테이블 + 의존성 명시 |
| 3. 구현 순서 (병렬 가능 여부 명시) | O | Wave A~I 병렬 실행 계획 상세 기술 |
| 4. 테스트 계획 (유닛/수동 검증) | O | 단계별 테스트 항목 + EditMode/PlayMode 전략 테이블 |

---

## 긍정적인 점

- **Wave 기반 병렬 계획**: 단순 순서 나열을 넘어 의존성 분석으로 병렬 가능 웨이브를 도출하고 게이트 스텝 이유까지 명시한 점이 실용적이다.
- **단계별 리스크 사전 식별**: 각 스텝마다 리스크와 대응 방안을 함께 기술하여 구현 중 막히는 지점을 예측하고 준비할 수 있다.
- **체크포인트 정의**: Layer 완료 시점마다 플레이 가능한 상태를 체크포인트로 정의하여 진행 상황 판단이 명확하다.
- **설계 리뷰 반영 완료**: design.md 리뷰에서 도출된 수정 사항(EntitySpawner 이름, UnitCombat.Tick(dt), Squad 이벤트 등록, HitStop isActive 등)이 구현 계획에 모두 반영되어 있다.

---

## 이슈

### 1. 기술 스택 섹션 누락 (중요도: 높음)

구현 컨벤션 필수 항목 #1인 기술 스택이 문서에 없다. 어떤 Unity 패키지나 라이브러리에 의존하는지 명시되어야 한다.

포함되어야 할 항목:

| 항목 | 내용 |
|------|------|
| Unity 버전 | 프로젝트 타겟 버전 명시 |
| Tilemap | 2D Tilemap (월드맵 생성에 사용) |
| Input System | Legacy Input (GetAxisRaw) 사용 여부 명시 |
| UniTask | HitStop/CameraShake Coroutine 대체 여부 |
| Phase 1 모듈 | Base (Facade, Notifier), FiniteStateMachine 버전 |

---

### 2. 관련 설계 파일 링크 깨짐 (중요도: 높음)

각 스텝의 **관련 설계** 항목이 분리된 설계 파일을 참조하지만, 해당 파일들이 존재하지 않는다.

| 참조 경로 | 실제 존재 여부 |
|-----------|:---:|
| `design/entity_common.md` | X |
| `design/player.md` | X |
| `design/world_map.md` | X |
| `design/game_controller.md` | X |
| `design/squad.md` | X |
| `design/monster.md` | X |
| `design/combat.md` | X |
| `design/taming.md` | X |
| `design/vfx.md` | X |
| `design/fog_of_war.md` | X |
| `design/minimap.md` | X |

현재 설계 문서는 단일 `design.md`에 통합되어 있다. 링크가 모두 깨져 있으므로 수정이 필요하다.

**수정 방향 (2가지 선택):**
- 단일 파일 유지: 링크를 `[design.md](design.md)` + 앵커(`#25-플레이어-이동-및-입력`)로 교체
- 분리 파일로 전환: 설계 문서를 `design/` 하위로 시스템별 분리 (문서량이 많아지면 이쪽이 유리)

---

### 3. Step 11 ↔ Step 12 병렬 처리와 의존성 충돌 (중요도: 중간)

**Wave H에서 Step 11(GameSnapshot)과 Step 12(FogOfWar)를 병렬 실행**하도록 계획되어 있다. 그런데 Step 11 본문에 아래 내용이 있다.

> **의존성**: Step 7, 8, 9, 12 (FogOfWar는 Step 12에서 완성. 이 단계에서는 FogGrid를 null 허용 또는 stub으로 처리)
> Step 4에서 fogGrid 통합

병렬 실행 후 FogGrid를 통합하는 서브스텝이 Wave H 이후에 필요한데, 이것이 병렬 계획 다이어그램에 표현되어 있지 않다. Wave H 완료 후 실제로는 추가 통합 작업이 존재한다.

**수정 방향**: Wave H 뒤에 "Step 11 FogGrid 통합 (단독)" 서브스텝을 명시하거나, 또는 Step 11을 Wave H에서 제외하고 Wave I 이전 단독 단계로 이동한다.

---

### 4. MonsterAI UnitGrid 주입 방식 미확정 (중요도: 중간)

Step 7 구현 순서에서 다음을 언급하고 있다.

> `MonsterAI` 생성 시 `combatSystem.UnitGrid` 주입 (Step 8 이후 완성이나 인터페이스 먼저 정의)

그런데 `design.md`에서 Monster 생성자는 `ai = new MonsterAI(this)`로 내부에서 MonsterAI를 생성한다. 이 방식이라면 UnitGrid를 Monster 생성자 시점에 주입할 수 없다.

해결 방법이 명확히 정의되어야 한다.

**선택지:**
1. `Monster` 생성자에 `SpatialGrid<IUnit>` 파라미터 추가 → MonsterAI에 전달 (EntitySpawner가 주입 책임)
2. `MonsterAI`를 Monster 외부에서 생성하고 `Monster.SetAI(ai)` 등으로 주입
3. MonsterAI가 CombatSystem의 UnitGrid를 직접 참조하지 않고, GameController가 매 프레임 갱신된 목록을 전달

Step 6(Monster 구현)과 Step 7(MonsterAI 구현) 사이의 Monster 생성자 수정 여부가 확정되지 않으면 Step 6 산출물을 Step 7에서 다시 수정해야 한다.

---

### 5. 구현 순서 체크박스 형식 미사용 (중요도: 낮음)

컨벤션이 권장하는 `- [ ]` 체크박스 형식이 사용되지 않고 테이블 형식으로 대체되어 있다. 컨벤션의 의도는 진행 상황 추적이므로, 테이블로도 목적을 달성할 수 있다. 단, 실제 구현 진행 중 완료 표시를 어떤 방식으로 할지 팀 내 합의가 필요하다.

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 컨벤션 충족도 | **B+** | 핵심 구조(시스템 설계·순서·테스트)는 모두 충족. 기술 스택 섹션 누락이 유일한 필수 항목 미충족 |
| 계획 구체성 | **A** | Wave 병렬화, 게이트 스텝 정의, 단계별 리스크 대응이 구체적이고 실행 가능한 수준 |
| 설계 문서 반영도 | **A** | concept_design_review.md의 10개 이슈가 모두 반영됨 |
| 링크 정합성 | **C** | 11개 설계 서브파일 링크가 모두 깨짐 |

### 구현 시작 전 수정 권장 사항 3가지

1. **기술 스택 섹션 추가** — Unity 버전, 사용 패키지, Phase 1 모듈 버전 명시
2. **관련 설계 링크 수정** — 단일 `design.md` 앵커 링크 또는 파일 분리 중 방향 결정
3. **MonsterAI UnitGrid 주입 방식 확정** — Step 6 Monster 생성자 수정 여부 결정 후 Step 6/7 구현 순서 반영
