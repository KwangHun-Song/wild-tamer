# 테스트용 스쿼드(Purple) · 몬스터(Red) 초기화 구현 계획

## 개요

테이밍 시스템 없이 게임 시작 시 Purple 유닛을 부대원(SquadMember)으로, Red 유닛을 몬스터(Monster)로 미리 스폰하여 이동·전투·애니메이션 동작을 검증한다.

| 색상 | 역할 |
|------|------|
| Purple | SquadMember (플레이어 편 부대원) |
| Red | Monster (적 유닛) |

### 현재 이미 완료된 것

| 항목 | 경로 | 상태 |
|------|------|------|
| Base Animator Controller | `Assets/Animations/Base/UnitMovement.controller` | ✅ 존재 |
| Red 애니메이션 클립 (3종) | `Assets/Animations/Clips/Red/{Warrior,Archer,Lancer}_Idle/Run.anim` | ✅ 존재 |
| Red Override Controllers (3종) | `Assets/Animations/Override/Red/{Warrior,Archer,Lancer}_Red.overrideController` | ✅ 존재 |
| Purple 애니메이션 클립 (3종) | `Assets/Animations/Clips/Purple/{Warrior,Archer,Lancer}_Idle/Run.anim` | ✅ 존재 |
| Purple Override Controllers (3종) | `Assets/Animations/Override/Purple/{Warrior,Archer,Lancer}_Purple.overrideController` | ✅ 존재 |
| EntitySpawner.SpawnMonster / SpawnSquadMember | `Assets/Scripts/04.Game/02.System/Entity/EntitySpawner.cs` | ✅ 구현됨 |
| Squad.AddMember / Update | `Assets/Scripts/04.Game/02.System/Squad/Squad.cs` | ✅ 구현됨 |

---

## 단계별 구현 순서

### Step 1 — MonsterView.cs Animator 추가 (코드 작업)

현재 MonsterView는 람다로 이벤트를 구독하여 Unsubscribe가 불가능하고, Animator가 없다. PlayerView/SquadMemberView 패턴에 맞춰 수정한다.

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
- 람다 구독 → `subscribedMonster` + `Unsubscribe()` + `OnDestroy()` 패턴으로 교체 (메모리 누수 방지)
- `OnMoveRequested`에서 `SetBool(IsMoving, ...)` 호출

---

### Step 2 — SquadMemberView.cs Animator 추가 (코드 작업)

Unsubscribe 패턴은 이미 구현되어 있다. Animator SerializeField와 SetBool 호출만 추가한다.

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
- `OnMoveRequested` 람다 → 메서드로 변경, `SetBool` 추가

---

### Step 3 — GameController에 테스트 스폰 메서드 추가 (코드 작업)

`entitySpawner`는 private이므로, InPlayState가 초기 스폰을 요청할 수 있는 공개 메서드를 GameController에 추가한다.

```csharp
/// <summary>테스트용: 게임 시작 시 부대원과 몬스터를 초기 배치한다.</summary>
public void SpawnTestEntities(MonsterData[] squadData, MonsterData[] monsterData, Vector2 origin)
{
    for (int i = 0; i < squadData.Length; i++)
    {
        var pos = origin + new Vector2((i + 1) * 1.5f, 0f);
        var member = entitySpawner.SpawnSquadMember(squadData[i], pos);
        Squad.AddMember(member);
    }

    for (int i = 0; i < monsterData.Length; i++)
    {
        var pos = origin + new Vector2((i - monsterData.Length / 2f) * 2.5f, 6f);
        entitySpawner.SpawnMonster(monsterData[i], pos);
    }
}
```

---

### Step 4 — InPlayState에 테스트 초기화 추가 (코드 작업)

```csharp
[SerializeField] private MonsterData[] initialSquadData;   // Purple 부대원 데이터
[SerializeField] private MonsterData[] initialMonsterData; // Red 몬스터 데이터
```

OnExecuteAsync() 내 GameController 생성 직후:

```csharp
// 테스트용: 초기 부대원(Purple) · 몬스터(Red) 스폰
var spawnOrigin = playPage.WorldMap.PlayerSpawn != null
    ? (Vector2)playPage.WorldMap.PlayerSpawn.position
    : Vector2.zero;
gameController.SpawnTestEntities(initialSquadData, initialMonsterData, spawnOrigin);
```

---

### Step 5 — MonsterView 프리팹 생성 (에디터 작업)

Red 3종 각각 동일한 계층 구조로 생성한다.

#### 프리팹 계층 구조

```
MonsterWarriorView (root GO)     ← MonsterView 컴포넌트, UnitMovement 컴포넌트
└── Visual (child GO)            ← SpriteRenderer, Animator
```

#### 생성할 프리팹 목록

| 프리팹명 | 저장 경로 | Animator Controller |
|---------|----------|-------------------|
| `MonsterWarriorView.prefab` | `Assets/Prefabs/Monster/` | `Override/Red/Warrior_Red.overrideController` |
| `MonsterArcherView.prefab` | `Assets/Prefabs/Monster/` | `Override/Red/Archer_Red.overrideController` |
| `MonsterLancerView.prefab` | `Assets/Prefabs/Monster/` | `Override/Red/Lancer_Red.overrideController` |

#### 각 프리팹 SerializeField 연결

| 필드 | 연결 대상 |
|------|----------|
| `movement` (CharacterView) | 루트 GO의 `UnitMovement` |
| `animator` (MonsterView) | `Visual` 자식의 `Animator` |

#### UnitMovement 기본값

- `MoveSpeed`: `3` (런타임에 MonsterData.moveSpeed로 덮어씀)

---

### Step 6 — SquadMemberView 프리팹 생성 (에디터 작업)

Step 5와 동일한 구조. Purple Override Controller 사용.

#### 생성할 프리팹 목록

| 프리팹명 | 저장 경로 | Animator Controller |
|---------|----------|-------------------|
| `SquadWarriorView.prefab` | `Assets/Prefabs/Squad/` | `Override/Purple/Warrior_Purple.overrideController` |
| `SquadArcherView.prefab` | `Assets/Prefabs/Squad/` | `Override/Purple/Archer_Purple.overrideController` |
| `SquadLancerView.prefab` | `Assets/Prefabs/Squad/` | `Override/Purple/Lancer_Purple.overrideController` |

---

### Step 7 — MonsterData ScriptableObject 생성 (에디터 작업)

`Assets/Data/Monsters/` 폴더를 생성하고 3종의 MonsterData 에셋을 만든다.

#### MonsterData 에셋 목록

| 에셋명 | prefab | squadPrefab |
|--------|--------|-------------|
| `MonsterData_Warrior.asset` | `MonsterWarriorView` | `SquadWarriorView` |
| `MonsterData_Archer.asset` | `MonsterArcherView` | `SquadArcherView` |
| `MonsterData_Lancer.asset` | `MonsterLancerView` | `SquadLancerView` |

#### 기본 스탯 초기값

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

### Step 8 — InPlayState 에디터 설정 (에디터 작업)

Play.unity에서 `InPlayState` 컴포넌트를 선택하고 두 배열 필드에 에셋을 할당한다.

| 필드 | 할당할 에셋 (순서대로) |
|------|----------------------|
| `initialSquadData` | MonsterData_Warrior, MonsterData_Archer, MonsterData_Lancer |
| `initialMonsterData` | MonsterData_Warrior, MonsterData_Archer, MonsterData_Lancer |

---

## 검증 체크리스트

### 코드 수정
- [ ] `MonsterView.cs` — Animator SerializeField 추가, Unsubscribe 패턴 추가, SetBool 호출 추가
- [ ] `SquadMemberView.cs` — Animator SerializeField 추가, SetBool 호출 추가
- [ ] `GameController.cs` — `SpawnTestEntities()` 메서드 추가
- [ ] `InPlayState.cs` — `initialSquadData`, `initialMonsterData` SerializeField 추가, 호출 추가

### 프리팹 (Monster — Red)
- [ ] `Prefabs/Monster/MonsterWarriorView.prefab` — MonsterView + UnitMovement + Visual(SpriteRenderer + Animator)
- [ ] `Prefabs/Monster/MonsterArcherView.prefab` — 동일
- [ ] `Prefabs/Monster/MonsterLancerView.prefab` — 동일
- [ ] 각 프리팹: `movement` → 루트 UnitMovement 연결됨
- [ ] 각 프리팹: `animator` → Visual Animator 연결됨
- [ ] 각 프리팹: Animator Controller → Red Override Controller 연결됨

### 프리팹 (Squad — Purple)
- [ ] `Prefabs/Squad/SquadWarriorView.prefab` — SquadMemberView + UnitMovement + Visual(SpriteRenderer + Animator)
- [ ] `Prefabs/Squad/SquadArcherView.prefab` — 동일
- [ ] `Prefabs/Squad/SquadLancerView.prefab` — 동일
- [ ] 각 프리팹: `movement` → 루트 UnitMovement 연결됨
- [ ] 각 프리팹: `animator` → Visual Animator 연결됨
- [ ] 각 프리팹: Animator Controller → Purple Override Controller 연결됨

### MonsterData ScriptableObjects
- [ ] `Data/Monsters/MonsterData_Warrior.asset` — prefab/squadPrefab 연결됨
- [ ] `Data/Monsters/MonsterData_Archer.asset` — prefab/squadPrefab 연결됨
- [ ] `Data/Monsters/MonsterData_Lancer.asset` — prefab/squadPrefab 연결됨

### 에디터 설정
- [ ] InPlayState.initialSquadData — MonsterData 3종 할당됨
- [ ] InPlayState.initialMonsterData — MonsterData 3종 할당됨

### 동작 확인 (Play Mode)
- [ ] 게임 시작 시 Purple 부대원 3명이 플레이어 우측에 스폰됨
- [ ] 게임 시작 시 Red 몬스터 3마리가 플레이어 위쪽에 스폰됨
- [ ] 플레이어 이동 시 부대원이 Flock 패턴으로 따라옴
- [ ] 부대원 이동 중 Run 애니메이션, 정지 시 Idle 애니메이션 재생됨
- [ ] 몬스터가 플레이어/부대원 감지 후 추격하며 Run 애니메이션 재생됨
- [ ] 몬스터 정지 시 Idle 애니메이션으로 전환됨
- [ ] 몬스터와 부대원/플레이어 간 전투가 발생함

---

## 작업 분류

| Step | 방법 | 선행 조건 |
|------|------|----------|
| Step 1 — MonsterView.cs 수정 | 코드 편집 | 없음 |
| Step 2 — SquadMemberView.cs 수정 | 코드 편집 | 없음 (Step 1과 병렬 가능) |
| Step 3 — GameController 수정 | 코드 편집 | 없음 (Step 1·2와 병렬 가능) |
| Step 4 — InPlayState 수정 | 코드 편집 | Step 3 완료 |
| Step 5 — MonsterView 프리팹 생성 | 에디터 | Step 1 코드 컴파일 완료 |
| Step 6 — SquadMemberView 프리팹 생성 | 에디터 | Step 2 코드 컴파일 완료 |
| Step 7 — MonsterData 에셋 생성 | 에디터 | Step 5·6 완료 |
| Step 8 — InPlayState 에디터 설정 | 에디터 | Step 4 컴파일 완료, Step 7 완료 |

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/04.Game/01.Entity/Monster/MonsterView.cs` | (수정) Animator 추가, Unsubscribe 패턴 추가 |
| `Assets/Scripts/04.Game/01.Entity/Squad/SquadMemberView.cs` | (수정) Animator 추가, SetBool 추가 |
| `Assets/Scripts/04.Game/02.System/Game/GameController.cs` | (수정) SpawnTestEntities() 추가 |
| `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs` | (수정) 초기화 필드·호출 추가 |
| `Assets/Animations/Override/Red/{Warrior,Archer,Lancer}_Red.overrideController` | (기존) 몬스터 Animator |
| `Assets/Animations/Override/Purple/{Warrior,Archer,Lancer}_Purple.overrideController` | (기존) 부대원 Animator |
| `Assets/Prefabs/Monster/` | (신규) MonsterView 프리팹 3종 |
| `Assets/Prefabs/Squad/` | (신규) SquadMemberView 프리팹 3종 |
| `Assets/Data/Monsters/` | (신규) MonsterData ScriptableObject 3종 |
