# 보스 시스템 TRD (Technical Release Document)

> 설계 문서: [boss_system.md](design/boss_system.md)
> 구현 계획: [implementation_plan_boss_system.md](implementation_plan_boss_system.md)
> 작성일: 2026-03-03

---

## 1. 구현 완료 현황

### 1-1. 코드 (모두 완료)

| 파일 | 경로 | 상태 |
|------|------|------|
| `BossMonsterData.cs` | `ScriptableObjects/` | ✅ |
| `BossPatternData.cs` | `ScriptableObjects/` | ✅ |
| `IBossPattern.cs` | `01.Entity/Boss/Patterns/` | ✅ |
| `BossPatternUtils.cs` | `01.Entity/Boss/Patterns/` | ✅ |
| `TrackingZonePattern.cs` (P1) | `01.Entity/Boss/Patterns/` | ✅ |
| `ChargePattern.cs` (P2) | `01.Entity/Boss/Patterns/` | ✅ |
| `CrossZonePattern.cs` (P3) | `01.Entity/Boss/Patterns/` | ✅ |
| `XZonePattern.cs` (P4) | `01.Entity/Boss/Patterns/` | ✅ |
| `CurseMarkPattern.cs` (P5) | `01.Entity/Boss/Patterns/` | ✅ |
| `ProjectileBarragePattern.cs` (P6) | `01.Entity/Boss/Patterns/` | ✅ |
| `SummonMinionsPattern.cs` (P7) | `01.Entity/Boss/Patterns/` | ✅ |
| `BossFSM.cs` | `01.Entity/Boss/` | ✅ |
| `BossIdleState.cs` | `01.Entity/Boss/States/` | ✅ |
| `BossChaseState.cs` | `01.Entity/Boss/States/` | ✅ |
| `BossPatternCastState.cs` | `01.Entity/Boss/States/` | ✅ |
| `BossDeadState.cs` | `01.Entity/Boss/States/` | ✅ |
| `BossMonster.cs` | `01.Entity/Boss/` | ✅ |
| `BossMonsterView.cs` | `01.Entity/Boss/` | ✅ |
| `BossProjectile.cs` | `01.Entity/Boss/` | ✅ |
| `ZoneIndicatorView.cs` | `01.Entity/Boss/` | ✅ |
| `BossSpawnSystem.cs` | `02.System/Boss/` | ✅ |
| `BossHpBarView.cs` | `03.UI/Boss/` | ✅ |
| `BossWarningView.cs` | `03.UI/Boss/` | ✅ |

### 1-2. 기존 코드 수정

| 파일 | 변경 내용 | 상태 |
|------|----------|------|
| `EntitySpawner.cs` | `RegisterBoss` / `UnregisterBoss` 추가, `Update()`에 boss 틱 추가 | ✅ |
| `GameController.cs` | `BossSpawnSystem` 생성 및 `Update(dt)` 호출 추가 | ✅ |
| `InPlayState.cs` | `[Header("보스")]` 3개 필드(`bossPool`, `bossWarningView`, `bossHpBarView`) 추가 | ✅ |

### 1-3. ScriptableObject 에셋

| 에셋 | 경로 | 패턴 연결 | 상태 |
|------|------|----------|------|
| `BossMonsterData_Monk.asset` | `Assets/Data/Boss/` | P1,P3,P5,P7 연결됨 | ✅ |
| `BossMonsterData_Pawn.asset` | `Assets/Data/Boss/` | P2,P4,P6,P7 연결됨 | ✅ |
| `MonkP1_TrackingZone.asset` | `Assets/Data/Boss/Patterns/` | — | ✅ |
| `MonkP3_CrossZone.asset` | `Assets/Data/Boss/Patterns/` | — | ✅ |
| `MonkP5_CurseMark.asset` | `Assets/Data/Boss/Patterns/` | — | ✅ |
| `MonkP7_SummonMinions.asset` | `Assets/Data/Boss/Patterns/` | summonData 미설정 | ⚠️ |
| `PawnP2_Charge.asset` | `Assets/Data/Boss/Patterns/` | — | ✅ |
| `PawnP4_XZone.asset` | `Assets/Data/Boss/Patterns/` | — | ✅ |
| `PawnP6_ProjectileBarrage.asset` | `Assets/Data/Boss/Patterns/` | projectilePrefab 미설정 | ⚠️ |
| `PawnP7_SummonMinions.asset` | `Assets/Data/Boss/Patterns/` | summonData 미설정 | ⚠️ |

### 1-4. InPlayState 씬 연결

| 필드 | 상태 |
|------|------|
| `bossPool` (Monk + Pawn 에셋) | ✅ 연결됨 |
| `bossWarningView` | ❌ UI 오브젝트 미생성 |
| `bossHpBarView` | ❌ UI 오브젝트 미생성 |

---

## 2. 남은 작업 (수동 Unity Editor 작업)

아래 작업들은 스프라이트·프리팹 에셋이 필요하거나 Unity Inspector 조작이 필요하므로
코드 구현 단계에서 완료되지 않았다.

---

### Task A — BossWarningView UI 구성 및 연결
**우선순위: 높음** (bossWarningView 없으면 BossSpawnSystem이 null 체크 후 경고 없이 스폰)

1. `Play.unity` 열기
2. `UI > PlayPage` 하위에 새 GameObject `BossWarningView` 생성
3. 다음 UI 계층 구성:
   ```
   BossWarningView          ← CanvasGroup, BossWarningView.cs
   ├── Background           ← Image (어두운 오버레이)
   ├── BossIcon             ← Image
   └── BossNameText         ← TextMeshProUGUI
   ```
4. `BossWarningView.cs` 인스펙터 필드 연결:
   - `bossNameText` → BossNameText
   - `bossIcon` → BossIcon
   - `canvasGroup` → 루트 CanvasGroup
5. `BossWarningView` 오브젝트 기본 비활성화 (SetActive false)
6. `InPlayState` 컴포넌트의 `Boss Warning View` 필드에 드래그 연결

---

### Task B — BossHpBarView UI 구성 및 연결
**우선순위: 높음** (bossHpBarView 없으면 HP 바가 표시되지 않음, 기능은 동작함)

1. `Play.unity` 열기
2. `UI > PlayPage` 상단에 새 GameObject `BossHpBarView` 생성
3. 다음 UI 계층 구성:
   ```
   BossHpBarView            ← BossHpBarView.cs
   ├── NameText             ← TextMeshProUGUI (보스 이름)
   ├── IconImage            ← Image (보스 아이콘)
   └── HpFill               ← Image (fillAmount 방식, ImageType=Filled, FillMethod=Horizontal)
   ```
4. `BossHpBarView.cs` 인스펙터 필드 연결:
   - `nameText` → NameText
   - `iconImage` → IconImage
   - `hpFillImage` → HpFill
5. `BossHpBarView` 오브젝트 기본 비활성화 (SetActive false)
6. `InPlayState` 컴포넌트의 `Boss Hp Bar View` 필드에 드래그 연결

---

### Task C — ZoneIndicator 프리팹 생성
**우선순위: 높음** (없으면 패턴 경고 인디케이터가 전혀 표시되지 않음)

각 프리팹 구조:
```
IndicatorRoot                ← ZoneIndicatorView.cs
└── Visual                   ← SpriteRenderer (IndicatorSprite 할당)
```

필요한 프리팹 4종:

| 프리팹명 | 사용 패턴 | 스프라이트 형태 |
|---------|---------|--------------|
| `CircleIndicator.prefab` | P1 TrackingZone, P5 CurseMark, P6 ProjectileBarrage | 원형 |
| `CrossIndicator.prefab` | P3 CrossZone | 십자(+) |
| `XIndicator.prefab` | P4 XZone | 대각선(×) |
| `LineIndicator.prefab` | P2 Charge | 직사각형 띠 |

저장 위치: `Assets/Prefabs/Boss/BossIndicators/`

---

### Task D — BossMonsterView 프리팹 생성
**우선순위: 중간** (프리팹 없으면 보스가 스폰되지 않음 — viewPrefab이 null이면 NullReferenceException)

1. 스프라이트 준비:
   - `YellowMonk` 스프라이트 시트
   - `YellowPawn` 스프라이트 시트

2. `YellowMonkBossView.prefab` 생성:
   ```
   YellowMonkBossView       ← BossMonsterView.cs, UnitMovement.cs, Animator
   └── Visual               ← SpriteRenderer
   ```
   - `BossMonsterView` 인스펙터 필드 연결:
     - `circleIndicator` → CircleIndicator 프리팹 인스턴스
     - `crossIndicator` → CrossIndicator 프리팹 인스턴스
     - `xIndicator` → XIndicator 프리팹 인스턴스
     - `lineIndicator` → LineIndicator 프리팹 인스턴스
     - `projectilePrefab` → BossProjectile 프리팹

3. `YellowPawnBossView.prefab` 동일하게 생성

4. 각 `BossMonsterData` 에셋의 `viewPrefab` 필드 연결:
   - `BossMonsterData_Monk.asset` → YellowMonkBossView.prefab
   - `BossMonsterData_Pawn.asset` → YellowPawnBossView.prefab

저장 위치: `Assets/Prefabs/Boss/`

---

### Task E — BossProjectile 프리팹 생성
**우선순위: 중간** (없으면 P6 투사체 발사 시 NullReferenceException)

1. 투사체 스프라이트 준비
2. `BossProjectile.prefab` 생성:
   ```
   BossProjectile           ← BossProjectile.cs, CircleCollider2D(IsTrigger=true)
   └── Visual               ← SpriteRenderer
   ```
3. 저장 위치: `Assets/Prefabs/Boss/`
4. 각 BossMonsterView 프리팹의 `projectilePrefab` 필드에 연결

---

### Task F — P7 summonData 설정
**우선순위: 낮음** (P7 패턴이 선택될 경우에만 실제로 문제 발생)

1. `Assets/Data/Boss/Patterns/MonkP7_SummonMinions.asset` 인스펙터 열기
2. `summonData` 필드에 소환할 MonsterData 에셋 드래그 연결
   - 권장: `MonsterData_Warrior.asset` (guid: `a075ca3e4b6b5914587420ab1291ec9f`)
3. `PawnP7_SummonMinions.asset` 동일하게 설정

> **참고**: `MonsterData_Warrior.asset`이 아직 없다면 소환 가능한 다른 MonsterData로 대체 가능.
> summonData가 null이면 P7 Activate 시 NullReferenceException 발생.

---

## 3. 검증 체크리스트

구현 완료 후 플레이 모드에서 다음 항목을 순서대로 확인한다.

### 3-1. 컴파일 및 기본 동작

- [ ] Unity 컴파일 오류 없음
- [ ] `Play.unity` 씬 진입 시 오류 없음
- [ ] `InPlayState` 인스펙터에서 bossPool, bossWarningView, bossHpBarView 모두 연결됨

### 3-2. 보스 스폰

- [ ] 게임 시작 180초 후 BossWarningView가 페이드인/아웃 표시됨
- [ ] 경고 종료 후 플레이어에서 15타일 거리에 보스가 스폰됨
- [ ] 스폰 위치가 장애물 위가 아님
- [ ] 보스 스폰 즉시 BossHpBarView가 상단에 표시됨 (HP 100%)
- [ ] 보스가 랜덤으로 YellowMonk 또는 YellowPawn 중 선택됨

### 3-3. FSM 상태 전이

- [ ] 스폰 직후 BossIdleState → detectionRange(999) 조건으로 즉시 BossChaseState 전환
- [ ] BossChaseState: 보스가 플레이어를 향해 이동함
- [ ] BossChaseState: 패턴 쿨다운 경과 후 BossPatternCastState로 전환됨
- [ ] BossPatternCastState: Warning 단계에서 ZoneIndicator 표시됨
- [ ] BossPatternCastState: Active 단계에서 인디케이터가 빨간색으로 플래시됨
- [ ] 패턴 완료 후 BossChaseState로 복귀

### 3-4. 패턴별 동작

| 패턴 | 확인 항목 |
|------|---------|
| P1 TrackingZone | 원형 인디케이터가 플레이어를 추적하다가 Active에서 확정 |
| P2 Charge | 직선 라인 인디케이터 → 보스가 직선 돌진 → 경로 상 유닛 피해 |
| P3 CrossZone | 십자 인디케이터 4방향 → Active에서 범위 내 유닛 피해 |
| P4 XZone | 대각선 인디케이터 4방향 → Active에서 범위 내 유닛 피해 |
| P5 CurseMark | 소형 원형 인디케이터가 가장 가까운 적 추적 → 확정 후 피해 |
| P6 ProjectileBarrage | 투사체 n발이 부채꼴로 발사, 각 투사체가 충돌 시 피해 |
| P7 SummonMinions | 보스 주변 summonRadius 내에 n마리 몬스터 소환 |

### 3-5. 인레이지 (Enrage)

- [ ] 보스 HP 50% 이하 시 이동 속도가 enrageSpeedMultiplier 만큼 증가함
- [ ] 인레이지는 한 번만 발동됨 (중복 발동 없음)

### 3-6. 보스 사망

- [ ] 보스 HP 0 → BossDeadState 진입 → 사망 연출(PlayDeathSequence) 재생
- [ ] BossHpBarView가 비활성화됨
- [ ] 모든 ZoneIndicator가 숨겨짐
- [ ] EntitySpawner에서 보스가 정상적으로 제거됨
- [ ] 240초 후 보스 재스폰 시작

---

## 4. 알려진 제약사항

1. **P7 소환 한계**: 장애물로 막힌 지역에서 보스가 패턴을 시전할 경우 최대 시도 횟수(20회)를 초과하면 summonCount보다 적은 수가 소환될 수 있다. 의도된 동작.

2. **P6 투사체 풀링 없음**: `Instantiate/Destroy` 방식 사용. 보스당 최대 동시 투사체는 패턴 데이터의 `projectileCount`에 의존하므로 과도한 수로 설정하지 않도록 한다.

3. **BossProjectile 충돌**: 현재 `OnTriggerEnter2D`에서 `CharacterView` 컴포넌트를 찾아 유닛을 식별한다. 벽 타일에 별도 콜라이더가 없다면 투사체가 벽을 통과할 수 있다. 필요 시 장애물 레이어 충돌 설정 추가 필요.

4. **다중 보스 미지원**: `EntitySpawner.activeBoss`는 단일 참조다. 동시에 2마리 이상의 보스를 지원하려면 별도 설계 변경 필요.
