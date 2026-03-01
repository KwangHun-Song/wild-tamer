# MonsterView / SquadMemberView 프리팹 구현 계획

## 개요

MonsterView(적 유닛)와 SquadMemberView(테이밍 후 아군 유닛)의 스프라이트·Animator·프리팹·MonsterData를 구성한다.
코드(MonsterView.cs, SquadMemberView.cs)는 이미 존재하나 Animator 연동이 없으므로, PlayerView와 동일한 패턴으로 추가한다.

### Phase 2 몬스터 종류

| 종류 | 역할 | 몬스터 색상 | 부대원 색상 |
|------|------|------------|------------|
| Warrior | 근접 기본 몬스터 | Red | Blue |
| Archer | 원거리 몬스터 | Red | Blue |
| Lancer | 중거리 창병 몬스터 | Red | Blue |

> Monk는 Phase 3 이후 추가 검토. Boss 몬스터는 별도 계획으로 분리한다.

### 사용 스프라이트 (Phase 2)

| 유닛 | 파일 | 경로 | 용도 |
|------|------|------|------|
| Red Warrior | `Warrior_Idle.png` | `Assets/Graphic/Sprites/Units/Red Units/Warrior/` | 몬스터 Idle |
| Red Warrior | `Warrior_Run.png` | 위 동일 | 몬스터 Run |
| Red Archer | `Archer_Idle.png` | `Assets/Graphic/Sprites/Units/Red Units/Archer/` | 몬스터 Idle |
| Red Archer | `Archer_Run.png` | 위 동일 | 몬스터 Run |
| Red Lancer | `Lancer_Idle.png` | `Assets/Graphic/Sprites/Units/Red Units/Lancer/` | 몬스터 Idle |
| Red Lancer | `Lancer_Run.png` | 위 동일 | 몬스터 Run |
| Blue Warrior | `Warrior_Idle.png` | `Assets/Graphic/Sprites/Units/Blue Units/Warrior/` | 부대원 Idle |
| Blue Warrior | `Warrior_Run.png` | 위 동일 | 부대원 Run |
| Blue Archer | `Archer_Idle.png` | `Assets/Graphic/Sprites/Units/Blue Units/Archer/` | 부대원 Idle |
| Blue Archer | `Archer_Run.png` | 위 동일 | 부대원 Run |
| Blue Lancer | `Lancer_Idle.png` | `Assets/Graphic/Sprites/Units/Blue Units/Lancer/` | 부대원 Idle |
| Blue Lancer | `Lancer_Run.png` | 위 동일 | 부대원 Run |

> Attack 애니메이션(`Warrior_Attack1`, `Archer_Shoot`, `Lancer_*_Attack` 등)은 Phase 2에서 사용하지 않는다.
> Lancer는 방향별 스프라이트가 있으나 Phase 2에서는 Idle/Run만 사용한다.

---

## 클래스 역할 요약

| 클래스 | 역할 |
|--------|------|
| `MonsterView` | Monster 이벤트 구독, Movement 구동, Idle/Run 애니메이션 제어, 피격/사망/테이밍 연출 |
| `SquadMemberView` | SquadMember 이벤트 구독, Movement 구동, Idle/Run 애니메이션 제어 |
| `MonsterData` | 몬스터 종별 스탯 + prefab(MonsterView) + squadPrefab(SquadMemberView) 참조 |

---

## 단계별 구현 순서

### Step 1 — MonsterView.cs 코드 수정 (코드 작업)

Animator SerializeField와 isMoving 제어를 추가한다.

```csharp
using UnityEngine;

public class MonsterView : CharacterView
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private Monster subscribedMonster;

    public void Subscribe(Monster monster)
    {
        subscribedMonster = monster;
        monster.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedMonster != null)
        {
            subscribedMonster.OnMoveRequested -= OnMoveRequested;
            subscribedMonster = null;
        }
    }

    private void OnMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        Movement.Move(direction);
    }

    private void OnDestroy() => Unsubscribe();

    public void PlayHitEffect() { }
    public void PlayDeathEffect() { }
    public void PlayTamingEffect() { }
}
```

변경 사항:
- `[SerializeField] private Animator animator` 추가
- `subscribedMonster` 필드, `Unsubscribe()`, `OnDestroy()` 추가 (메모리 누수 방지)
- `OnMoveRequested`에서 `SetBool(IsMoving, ...)` 호출

---

### Step 2 — SquadMemberView.cs 코드 수정 (코드 작업)

Animator SerializeField와 isMoving 제어를 추가한다.
(SquadMemberView는 이미 subscribedMember/Unsubscribe/OnDestroy 패턴이 구현되어 있으므로 Animator만 추가)

```csharp
using UnityEngine;

public class SquadMemberView : CharacterView
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private SquadMember subscribedMember;

    public void Subscribe(SquadMember member)
    {
        subscribedMember = member;
        member.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedMember != null)
        {
            subscribedMember.OnMoveRequested -= OnMoveRequested;
            subscribedMember = null;
        }
    }

    private void OnMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        Movement.Move(direction);
    }

    private void OnDestroy() => Unsubscribe();
}
```

변경 사항:
- `[SerializeField] private Animator animator` 추가
- `OnMoveRequested`에서 `SetBool(IsMoving, ...)` 호출 추가

---

### Step 3 — 스프라이트 슬라이싱 (Unity MCP / 에디터 작업)

Red Units 3종(Warrior, Archer, Lancer)과 Blue Units 3종의 Idle·Run을 슬라이싱한다.

#### 공통 설정 (모든 파일 동일)

| 설정 항목 | 값 |
|----------|----|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | **Multiple** |
| Pixels Per Unit | 16 |
| Filter Mode | **Point (no filter)** |
| Compression | None |

#### 슬라이싱 방법

1. Sprite Editor 열기 → Slice → Type: **Grid By Cell Count**
2. 행·열 수를 확인하여 입력 후 Apply

#### 슬라이싱 대상 파일 목록 (12개)

| 파일 | 경로 |
|------|------|
| `Warrior_Idle.png` | `Red Units/Warrior/` |
| `Warrior_Run.png` | `Red Units/Warrior/` |
| `Archer_Idle.png` | `Red Units/Archer/` |
| `Archer_Run.png` | `Red Units/Archer/` |
| `Lancer_Idle.png` | `Red Units/Lancer/` |
| `Lancer_Run.png` | `Red Units/Lancer/` |
| `Warrior_Idle.png` | `Blue Units/Warrior/` |
| `Warrior_Run.png` | `Blue Units/Warrior/` |
| `Archer_Idle.png` | `Blue Units/Archer/` |
| `Archer_Run.png` | `Blue Units/Archer/` |
| `Lancer_Idle.png` | `Blue Units/Lancer/` |
| `Lancer_Run.png` | `Blue Units/Lancer/` |

---

### Step 4 — Animator Controller 생성 (Unity MCP / 에디터 작업)

유닛 종류별로 Animator Controller를 생성한다. 몬스터용(Red)과 부대원용(Blue)을 분리한다.

#### 생성할 컨트롤러 (6개)

| 파일명 | 경로 | 사용 스프라이트 색상 |
|--------|------|-------------------|
| `WarriorAnimator.controller` | `Assets/Animations/Monster/` | Red |
| `ArcherAnimator.controller` | `Assets/Animations/Monster/` | Red |
| `LancerAnimator.controller` | `Assets/Animations/Monster/` | Red |
| `WarriorAnimator.controller` | `Assets/Animations/Squad/` | Blue |
| `ArcherAnimator.controller` | `Assets/Animations/Squad/` | Blue |
| `LancerAnimator.controller` | `Assets/Animations/Squad/` | Blue |

#### 각 컨트롤러 공통 구성

**파라미터:**

| 파라미터 | 타입 |
|---------|------|
| `isMoving` | Bool |

**상태:**

| 상태 | 기본 상태 |
|------|----------|
| `Idle` | ✅ |
| `Run` | - |

**전환 조건:**

| 전환 | 조건 | Has Exit Time |
|------|------|---------------|
| Idle → Run | `isMoving = true` | false |
| Run → Idle | `isMoving = false` | false |

**각 상태 클립 설정**: 슬라이싱된 스프라이트 시퀀스를 타임라인에 배치, Loop Time ✅

---

### Step 5 — MonsterView 프리팹 생성 (Unity MCP / 에디터 작업)

3종 각각 동일한 구조로 생성한다.

#### 프리팹 구조

```
MonsterWarriorView (root GO)     [MonsterView 컴포넌트, UnitMovement 컴포넌트]
└── Visual (child GO)            [SpriteRenderer, Animator]
```

#### 생성할 프리팹 (3개)

| 프리팹명 | 경로 | Animator Controller |
|---------|------|-------------------|
| `MonsterWarriorView.prefab` | `Assets/Prefabs/Monster/` | `Animations/Monster/WarriorAnimator` |
| `MonsterArcherView.prefab` | `Assets/Prefabs/Monster/` | `Animations/Monster/ArcherAnimator` |
| `MonsterLancerView.prefab` | `Assets/Prefabs/Monster/` | `Animations/Monster/LancerAnimator` |

#### 각 프리팹 SerializeField 연결

| 필드 | 연결 대상 |
|------|----------|
| `movement` (CharacterView) | 루트 GO의 `UnitMovement` |
| `animator` (MonsterView) | `Visual` 자식의 `Animator` |

#### UnitMovement 기본값

- `MoveSpeed`: `3` (런타임에 MonsterData.moveSpeed로 덮어씀)

---

### Step 6 — SquadMemberView 프리팹 생성 (Unity MCP / 에디터 작업)

Step 5와 동일한 구조. Blue 스프라이트 사용.

#### 생성할 프리팹 (3개)

| 프리팹명 | 경로 | Animator Controller |
|---------|------|-------------------|
| `SquadWarriorView.prefab` | `Assets/Prefabs/Squad/` | `Animations/Squad/WarriorAnimator` |
| `SquadArcherView.prefab` | `Assets/Prefabs/Squad/` | `Animations/Squad/ArcherAnimator` |
| `SquadLancerView.prefab` | `Assets/Prefabs/Squad/` | `Animations/Squad/LancerAnimator` |

---

### Step 7 — MonsterData ScriptableObject 생성 (Unity MCP / 에디터 작업)

`Assets/Data/Monsters/` 폴더를 생성하고 3개의 MonsterData 에셋을 만든다.

#### MonsterData 에셋 목록

| 에셋명 | id | prefab | squadPrefab |
|--------|-----|--------|-------------|
| `MonsterData_Warrior.asset` | `warrior` | `MonsterWarriorView` | `SquadWarriorView` |
| `MonsterData_Archer.asset` | `archer` | `MonsterArcherView` | `SquadArcherView` |
| `MonsterData_Lancer.asset` | `lancer` | `MonsterLancerView` | `SquadLancerView` |

#### 기본 스탯 초기값 (플레이 테스트 후 밸런스 조정)

| 필드 | Warrior | Archer | Lancer |
|------|---------|--------|--------|
| `displayName` | `전사` | `궁수` | `창병` |
| `grade` | Normal | Normal | Normal |
| `maxHp` | 80 | 50 | 60 |
| `attackDamage` | 15 | 10 | 12 |
| `attackRange` | 1.5f | 4.0f | 2.5f |
| `attackCooldown` | 1.0f | 1.5f | 1.2f |
| `detectionRange` | 5.0f | 6.0f | 5.5f |
| `moveSpeed` | 2.5f | 2.0f | 3.0f |
| `tamingChance` | 0.3f | 0.3f | 0.3f |

---

### Step 8 — EntitySpawner 연결 확인 (에디터/코드 검증)

MonsterData 에셋이 GameController에서 EntitySpawner로 전달되어 스폰이 동작하는지 확인한다.

GameController에서 EntitySpawner.SpawnMonster()를 호출할 때 MonsterData를 넘기면 자동으로 MonsterData.prefab을 스폰하고 MonsterView를 가져온다.

---

## 검증 체크리스트

### 코드 수정
- [ ] `MonsterView.cs`에 `Animator` SerializeField 및 `SetBool` 호출 추가됨
- [ ] `MonsterView.cs`에 `subscribedMonster` / `Unsubscribe()` / `OnDestroy()` 추가됨
- [ ] `SquadMemberView.cs`에 `Animator` SerializeField 및 `SetBool` 호출 추가됨

### 스프라이트 슬라이싱 (Red Units)
- [ ] `Red Warriors/Warrior_Idle.png` Multiple 모드 슬라이싱 완료
- [ ] `Red Warriors/Warrior_Run.png` Multiple 모드 슬라이싱 완료
- [ ] `Red Archer/Archer_Idle.png` Multiple 모드 슬라이싱 완료
- [ ] `Red Archer/Archer_Run.png` Multiple 모드 슬라이싱 완료
- [ ] `Red Lancer/Lancer_Idle.png` Multiple 모드 슬라이싱 완료
- [ ] `Red Lancer/Lancer_Run.png` Multiple 모드 슬라이싱 완료

### 스프라이트 슬라이싱 (Blue Units)
- [ ] `Blue Warrior/Warrior_Idle.png` Multiple 모드 슬라이싱 완료
- [ ] `Blue Warrior/Warrior_Run.png` Multiple 모드 슬라이싱 완료
- [ ] `Blue Archer/Archer_Idle.png` Multiple 모드 슬라이싱 완료
- [ ] `Blue Archer/Archer_Run.png` Multiple 모드 슬라이싱 완료
- [ ] `Blue Lancer/Lancer_Idle.png` Multiple 모드 슬라이싱 완료
- [ ] `Blue Lancer/Lancer_Run.png` Multiple 모드 슬라이싱 완료

### Animator Controllers (Monster)
- [ ] `Animations/Monster/WarriorAnimator.controller` — Idle/Run 상태, isMoving 파라미터 설정됨
- [ ] `Animations/Monster/ArcherAnimator.controller` — 동일
- [ ] `Animations/Monster/LancerAnimator.controller` — 동일

### Animator Controllers (Squad)
- [ ] `Animations/Squad/WarriorAnimator.controller` — Idle/Run 상태, isMoving 파라미터 설정됨
- [ ] `Animations/Squad/ArcherAnimator.controller` — 동일
- [ ] `Animations/Squad/LancerAnimator.controller` — 동일

### 프리팹 (Monster)
- [ ] `Prefabs/Monster/MonsterWarriorView.prefab` — MonsterView + UnitMovement + Visual(SpriteRenderer + Animator) 존재
- [ ] `Prefabs/Monster/MonsterArcherView.prefab` — 동일
- [ ] `Prefabs/Monster/MonsterLancerView.prefab` — 동일
- [ ] 각 프리팹의 `movement` → 루트 UnitMovement 연결됨
- [ ] 각 프리팹의 `animator` → Visual Animator 연결됨

### 프리팹 (Squad)
- [ ] `Prefabs/Squad/SquadWarriorView.prefab` — SquadMemberView + UnitMovement + Visual(SpriteRenderer + Animator) 존재
- [ ] `Prefabs/Squad/SquadArcherView.prefab` — 동일
- [ ] `Prefabs/Squad/SquadLancerView.prefab` — 동일
- [ ] 각 프리팹의 `movement` → 루트 UnitMovement 연결됨
- [ ] 각 프리팹의 `animator` → Visual Animator 연결됨

### MonsterData ScriptableObjects
- [ ] `Data/Monsters/MonsterData_Warrior.asset` — prefab/squadPrefab 연결됨
- [ ] `Data/Monsters/MonsterData_Archer.asset` — prefab/squadPrefab 연결됨
- [ ] `Data/Monsters/MonsterData_Lancer.asset` — prefab/squadPrefab 연결됨

### 동작 확인 (Play Mode)
- [ ] EntitySpawner.SpawnMonster(warriorData, pos) → MonsterWarriorView 스폰됨
- [ ] 스폰된 몬스터가 플레이어 감지 후 이동 시 Run 애니메이션 재생됨
- [ ] 정지 시 Idle 애니메이션으로 전환됨
- [ ] 몬스터 처치 후 테이밍 성공 시 SquadWarriorView가 스폰되어 부대에 합류됨
- [ ] 부대원이 이동/정지 시 애니메이션이 올바르게 전환됨

---

## 작업 분류

| Step | 방법 | 선행 조건 |
|------|------|----------|
| Step 1 — MonsterView.cs 수정 | 코드 편집 | 없음 (즉시 가능) |
| Step 2 — SquadMemberView.cs 수정 | 코드 편집 | 없음 (Step 1과 병렬 가능) |
| Step 3 — 스프라이트 슬라이싱 | Unity MCP / 에디터 | Unity MCP 연결 |
| Step 4 — Animator Controllers 생성 | Unity MCP / 에디터 | Step 3 완료 |
| Step 5 — MonsterView 프리팹 생성 | Unity MCP / 에디터 | Step 1, 4 완료 |
| Step 6 — SquadMemberView 프리팹 생성 | Unity MCP / 에디터 | Step 2, 4 완료 |
| Step 7 — MonsterData 에셋 생성 | Unity MCP / 에디터 | Step 5, 6 완료 |
| Step 8 — EntitySpawner 연결 확인 | 에디터/테스트 | Step 7 완료 |

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/04.Game/01.Entity/Monster/MonsterView.cs` | (수정 대상) Animator 제어 추가 |
| `Assets/Scripts/04.Game/01.Entity/Squad/SquadMemberView.cs` | (수정 대상) Animator 제어 추가 |
| `Assets/Scripts/04.Game/01.Entity/Common/CharacterView.cs` | UnitMovement SerializeField 보유 |
| `Assets/Scripts/04.Game/02.System/Entity/EntitySpawner.cs` | SpawnMonster / SpawnSquadMember |
| `Assets/Graphic/Sprites/Units/Red Units/` | 몬스터 스프라이트 |
| `Assets/Graphic/Sprites/Units/Blue Units/` | 부대원 스프라이트 |
| `Assets/Animations/Monster/` | (생성 대상) 몬스터 Animator Controllers |
| `Assets/Animations/Squad/` | (생성 대상) 부대원 Animator Controllers |
| `Assets/Prefabs/Monster/` | (생성 대상) MonsterView 프리팹 3종 |
| `Assets/Prefabs/Squad/` | (생성 대상) SquadMemberView 프리팹 3종 |
| `Assets/Data/Monsters/` | (생성 대상) MonsterData ScriptableObject 3종 |
