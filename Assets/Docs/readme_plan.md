# README.md 포트폴리오 작성 계획

## 목적

이 문서는 프로젝트 루트의 `README.md`를 **포트폴리오용 기술 문서**로 작성하기 위한 계획이다.
프로젝트의 설계 철학, 기술적 깊이, 최적화 역량을 체계적으로 어필하는 것이 목표이다.

---

## 전체 구성 (목차)

| # | 섹션 | 핵심 목적 |
|---|------|-----------|
| 1 | 프로젝트 개요 | 첫인상 — 무엇을, 왜, 어떻게 만들었는지 |
| 2 | 기술 스택 | 사용 도구·프레임워크 한눈에 파악 |
| 3 | 프로젝트 구조 | 모듈 분리 설계와 폴더 아키텍처 |
| 4 | 핵심 시스템 설계 | 게임플레이를 이루는 주요 시스템 소개 |
| 5 | 디자인 패턴 | 적용된 패턴과 실제 활용 사례 |
| 6 | 군집 알고리즘 (Boid/Flock) | 가장 큰 기술적 차별점 — 심층 소개 |
| 7 | 성능 최적화 | Job System, SpatialGrid, 렌더링 최적화 |
| 8 | 모듈화 및 재사용성 | asmdef 기반 독립 모듈 설계 |
| 9 | 테스트 | 유닛 테스트 전략 |
| 10 | 개발 프로세스 | 6단계 워크플로우, 문서화 문화 |

---

## 섹션별 작성 계획

---

### 1. 프로젝트 개요

**목적:** 리크루터·동료 개발자가 README 첫 화면에서 프로젝트의 성격과 수준을 즉시 파악하도록 한다.

**포함할 내용:**
- 프로젝트 이름 + 한 줄 요약
- 스크린샷 또는 GIF (플레이 화면) — `Assets/Screenshots/` 활용
- 코어 루프 다이어그램: `탐험 → 전투 → 테이밍 → 부대 확장 → 탐험`
- 주요 특징 bullet list (3~5개)
  - NavMesh 없이 직접 구현한 군집 알고리즘 (Boid)
  - Unity Job System + Burst Compiler로 200+ 유닛 프레임드랍 없이 처리
  - 모듈화된 프레임워크 (FSM, Event Bus, Facade, UI 시스템)
  - 배치 최적화로 Set Pass Call 70~150 유지
  - 74개 설계 문서와 체계적 개발 프로세스

**참조 파일:**
- `Assets/Docs/concept.md` — 게임 컨셉, 코어 루프
- `Assets/Docs/milestone.md` — 구현 범위 확인
- `Assets/Screenshots/` — 스크린샷 3장

---

### 2. 기술 스택

**목적:** 사용된 기술을 표 형태로 깔끔하게 정리하여 기술 키워드 매칭과 역량 파악을 돕는다.

**포함할 내용:**

| 카테고리 | 기술 | 설명 |
|---------|------|------|
| 엔진 | Unity 2022 LTS | 2D 쿼터뷰 RPG |
| 렌더링 | URP (Universal Render Pipeline) | 2D Sprite 렌더링, SRP Batcher |
| 병렬 처리 | Unity Job System + Burst Compiler | 군집 계산 병렬화 |
| 비동기 | UniTask | async/await 기반 비동기 처리 |
| 애니메이션 | DOTween | 트윈 기반 연출 |
| 직렬화 | Newtonsoft.Json | JSON 기반 데이터 직렬화 |
| 고성능 컬렉션 | Unity.Collections (NativeArray 등) | Job System용 비관리 메모리 |
| 스프라이트 최적화 | SpriteAtlas + Custom Sort Axis | 배치 콜 최소화 |
| 데이터 관리 | ScriptableObject | 데이터 테이블화, Inspector 편집 |
| 테스트 | NUnit (Unity Test Framework) | 모듈 단위 테스트 |
| 녹화 | Unity Recorder | 게임플레이 영상 촬영 |

**참조 파일:**
- `Packages/manifest.json` — 패키지 의존성 확인
- `Assets/Settings/URP-Default.asset` — URP 설정
- `Assets/Plugins/DOTween/` — DOTween 플러그인

---

### 3. 프로젝트 구조

**목적:** 폴더 구조와 모듈 분리 철학을 보여주어 설계 역량을 어필한다.

**포함할 내용:**
- 상위 폴더 트리 다이어그램 (Assets/ 하위 1~2뎁스)
- `Scripts/` 폴더의 번호 기반 계층 구조 설명
  ```
  Scripts/
  ├── 01.Scene/     — Scene 진입 FSM (PlayStates)
  ├── 02.Page/      — UI 페이지 (PlayPage 등)
  ├── 03.Popup/     — UI 팝업 (Setting, Collection 등)
  └── 04.Game/      — 핵심 게임 로직
      ├── 01.Entity/ — 엔티티 (Player, Monster, Squad, Boss)
      ├── 02.System/ — 시스템 (Combat, Spatial, Squad, Map, VFX)
      ├── 03.Data/   — 데이터 클래스 (ScriptableObject)
      └── 05.Utility/ — 유틸리티
  ```
- `Modules/` 폴더의 asmdef 기반 독립 모듈 구조
  ```
  Modules/
  ├── Base/                — 핵심 프레임워크 (Facade, Notifier, PageChanger, PopupManager 등)
  └── FiniteStateMachine/  — 범용 FSM 프레임워크
  ```
- 모듈 간 의존성 다이어그램 (텍스트)
  ```
  Assembly-CSharp (Game) → Base.Runtime → UniTask, Newtonsoft.Json
                         → FiniteStateMachine.Runtime → Base.Runtime
  ```
- 스크립트 통계: C# 126개, 문서 74개, 프리팹 30+개

**참조 파일:**
- `Assets/Modules/Base/Runtime/Base.Runtime.asmdef`
- `Assets/Modules/FiniteStateMachine/Runtime/FiniteStateMachine.Runtime.asmdef`

---

### 4. 핵심 시스템 설계

**목적:** 게임을 구성하는 주요 시스템을 소개하여 설계 역량과 시스템 분리 능력을 어필한다.

**포함할 내용:**

#### 4-1. 게임 컨트롤러 (중앙 오케스트레이터)
- `GameController` — 순수 C# 클래스, MonoBehaviour 아님
- 모든 시스템을 소유하고 Update 순서를 제어
  ```
  입력 처리 → 부대 이동 → 몬스터 AI → 몬스터 스폰 → 보스 → 전투
  ```
- 게임 페이즈(Play/Pause) 기반 게이팅
- 이벤트 기반 시스템 간 연결 (OnMemberAdded → CombatSystem.RegisterUnit 등)

**참조:** `Assets/Scripts/04.Game/02.System/Game/GameController.cs`

#### 4-2. 엔티티 시스템 (Model-View 분리)
- **Model 계층** (순수 C#): `Player`, `SquadMember`, `Monster`, `BossMonster`
  - MonoBehaviour 의존 없음, 테스트 용이
- **View 계층** (MonoBehaviour): `PlayerView`, `SquadMemberView`, `MonsterView`, `BossMonsterView`
  - 렌더링, 애니메이션, 물리 담당
- **프리팹 구조**: Root(Animator + UnitMovement) → Visual(SpriteRenderer)
  - Animator가 루트에 있어야 Animation Clip 경로 바인딩이 올바르게 동작

**참조:** `Assets/Scripts/04.Game/01.Entity/` 전체

#### 4-3. 자동 전투 시스템
- `CombatSystem` — SpatialGrid 기반 적 탐지, 자동 타겟팅
- `DamageProcessor` — 데미지 계산 및 적용
- `TamingSystem` — 확률 기반 테이밍 판정, 부대 합류 처리
- Notifier를 통한 전투 이벤트 브로드캐스트 (피격, 사망, 테이밍 등)

**참조:** `Assets/Scripts/04.Game/02.System/Combat/`

#### 4-4. 월드맵 시스템
- `MapGenerator` — 타일맵 기반 프로시저럴 맵 생성
- `ObstacleGrid` — 장애물 그리드 (이동 가능 여부 판정)
- `FogOfWar` — 전장의 안개 (플레이어 시야 기반 공개)
- `MapDecorationGenerator` — 환경 오브젝트 (나무, 덤불) 배치

**참조:** `Assets/Scripts/04.Game/02.System/Map/`

#### 4-5. 보스 시스템
- `BossMonster` / `BossFSM` — 보스 전용 상태 머신
- **8종 공격 패턴**: Charge, CrossZone, ProjectileBarrage, SummonMinions, TrackingZone 등
- 패턴별 `ZoneIndicator` 프리팹 (Circle, Cross, Line, X)으로 위험 지역 시각화
- `BossSpawnSystem` — 타이머 기반 보스 등장, 등장 시 일반 몬스터 제거

**참조:** `Assets/Scripts/04.Game/01.Entity/Boss/Patterns/`

#### 4-6. 데이터 영속성
- `GameSaveManager` — JSON 기반 로컬 Save/Load
- `GameSaveData` — 직렬화 가능한 게임 스냅샷 (플레이어 위치, HP, 부대원, 보스 타이머)
- `GameController.CreateSaveData()` / `RestoreFrom()` — 저장/복원 통합 인터페이스

**참조:** `Assets/Scripts/04.Game/03.Data/Save/`

#### 4-7. UI 시스템 (Page / Popup / Scene)
- `PageChanger` — 비동기 페이지 전환, Stack 기반 히스토리 & 뒤로가기
- `PopupManager` — 팝업 스택 관리, 정렬 순서 자동 계산, 커튼(반투명 오버레이)
- `SceneStateMachine` — Scene 진입 시 FSM으로 초기화 흐름 관리
- UniTask 기반 비동기 전환 + 팝업 결과 반환 (`UniTask<T>`)

**참조:** `Assets/Modules/Base/Runtime/Scripts/PageChanger/`, `PopupManager/`, `SceneFlow/`

#### 4-8. 전투 연출 (VFX)
- `HitStop` — 피격 시 일시 정지 효과 (역경직)
- `CameraShake` — 화면 흔들림 (AnimationCurve 기반)
- `HitEffectPlayer` — 타격 이펙트 재생
- 오브젝트 풀링(`Facade.Pool`)과 `AutoDespawn`으로 이펙트 수명 관리

**참조:** `Assets/Scripts/04.Game/02.System/VFX/`

---

### 5. 디자인 패턴

**목적:** 실제 코드에서 활용한 디자인 패턴을 나열하고 각각의 적용 사례를 보여준다.

**포함할 내용:**

| 패턴 | 적용 위치 | 설명 |
|------|-----------|------|
| **FSM (유한 상태 머신)** | 유닛 AI, Scene 흐름 | 제네릭 `StateMachine<TEntity, TEnumTrigger>` — Player, Monster, Squad, Boss, Scene에 활용. 조건/트리거 기반 전이 지원 |
| **Facade** | Base 모듈 | `Facade` 정적 클래스 — Logger, DB, Pool, Sound 등 14개 서비스를 단일 진입점으로 제공. 인터페이스 기반이라 구현체 교체 가능 |
| **Observer (Event Bus)** | Notifier 시스템 | `Notifier` / `GlobalNotifier` — 타입 기반 이벤트 구독/발행. 시스템 간 느슨한 결합 달성 |
| **Object Pool** | 엔티티, 이펙트 | `DefaultObjectPool` — Dictionary 기반 풀, Preload/Spawn/Despawn 지원. 빈번한 생성/파괴 GC 부하 제거 |
| **Strategy** | 보스 공격 패턴 | 8종 `BossPattern` — 각 공격 패턴이 독립 클래스로 캡슐화, BossFSM이 전략 선택 |
| **Model-View 분리** | 엔티티 전체 | 순수 C# Model(Player, Monster) + MonoBehaviour View(PlayerView, MonsterView). 로직과 렌더링 완전 분리 |
| **Singleton** | 부트스트래퍼 | `Singleton<T>` — DontDestroyOnLoad 기반 전역 오브젝트 (Bootstrapper) |
| **데이터 주도 설계** | 전체 | 모든 게임 수치를 ScriptableObject로 테이블화. 코드 수정 없이 Inspector에서 밸런스 조정 가능 |

**각 패턴에 대해:**
- 패턴 이름 + 간단한 설명
- 프로젝트에서의 구체적 적용 위치
- 코드 스니펫 또는 구조 다이어그램 (간결하게)
- 이 패턴으로 얻은 이점

**참조 파일:**
- `Assets/Modules/FiniteStateMachine/Runtime/StateMachine.cs` — FSM
- `Assets/Modules/Base/Runtime/Scripts/Facade/Facade.cs` — Facade (14개 서비스)
- `Assets/Modules/Base/Runtime/Scripts/Notifier/Notifier.cs` — Observer/Event Bus
- `Assets/Modules/Base/Runtime/Scripts/Facade/Defaults/DefaultObjectPool.cs` — Object Pool
- `Assets/Scripts/04.Game/01.Entity/Boss/Patterns/` — Strategy (8종 패턴)
- `Assets/Scripts/04.Game/01.Entity/Common/Character.cs` — Model-View 분리

---

### 6. 군집 알고리즘 (Boid/Flock) — 심층 소개

**목적:** 이 프로젝트의 가장 큰 기술적 차별점을 심층적으로 소개한다. NavMesh를 사용하지 않고 직접 구현한 점을 강조한다.

**포함할 내용:**

#### 6-1. 알고리즘 개요
- Craig Reynolds의 Boid 알고리즘을 기반으로 5가지 힘 벡터를 합산하여 이동 방향을 결정
- NavMesh 미사용 — 순수 수학 기반 이동 처리, 동적 환경에 즉시 반응 가능
- 두 가지 구현: FlockBehavior(메인 스레드, 디버그 지원) + FlockJob(병렬, Burst 컴파일)

#### 6-2. 5가지 힘 벡터 다이어그램

```
┌─────────────────────────────────────────────────────────┐
│                    5가지 힘 벡터                          │
│                                                         │
│  1. Separation (분리)  — 이웃과 최소 거리 유지            │
│     역제곱 법칙: diff × (sqrMinSep - sqrDist) / (sqrDist × sqrMinSep)
│     ✦ sqrt 완전 제거 — sqrMagnitude만 사용              │
│                                                         │
│  2. Cohesion (응집)    — 이웃 무게중심 방향으로 이동       │
│     최소 유지 거리 이내 이웃은 계산에서 제외               │
│                                                         │
│  3. Alignment (정렬)   — 이웃과 같은 방향으로 이동 (스텁)  │
│                                                         │
│  4. Follow (추종)      — 리더(플레이어) 방향으로 이동      │
│     ArrivalRadius 이내: 거리 비례 감속 → Separation과 평형 │
│                                                         │
│  5. Avoidance (장애물 회피) — 4방향 인접 셀 장애물 검사   │
│     ObstacleGrid.IsWalkable() 기반                      │
└─────────────────────────────────────────────────────────┘
```

#### 6-3. 가중치 시스템
- ScriptableObject(`FlockSettingsData`)로 모든 가중치와 반경 관리
- 기본값: Separation 1.5, Cohesion 1.0, Follow 2.0, Avoidance 2.0
- NeighborRadius, ArrivalRadius, MinSeparationDistance 파라미터

#### 6-4. 최적화 기법
- 이웃 위치 사전 캐싱 (`neighborPosCache`) — Transform 접근 1회로 제한
- static 배열 재사용 (`AvoidDirections`) — 매 호출 할당 제거
- `sqrMagnitude` 전용 — 모든 거리 비교에서 sqrt 완전 배제
- Burst 컴파일 병렬 버전 (`FlockJob`) — 200+ 유닛도 프레임 내 처리

#### 6-5. Job System 병렬화
- `FlockJob : IJobParallelFor` + `[BurstCompile]`
- **3단계 파이프라인:**
  1. Main Thread: Transform → NativeArray 복사
  2. Worker Threads: Separation + Cohesion + Follow 병렬 계산 (Burst SIMD)
  3. Main Thread: 결과 적용 + Avoidance 후처리 (관리 코드 의존)
- `innerloopBatchCount: 8` (4~16 적응적 조정)
- `Allocator.Persistent` — NativeArray 풀링, 스쿼드 크기 변경 시만 재할당

#### 6-6. 두 가지 구현의 역할 분담

| | FlockBehavior (C#) | FlockJob (Burst) |
|---|---|---|
| 용도 | 플레이어 부대 (에디터 디버그) | 야생 몬스터 부대 (성능 우선) |
| 장애물 회피 | 직접 처리 | 메인 스레드 후처리 |
| 디버그 | Gizmo 시각화 지원 | 미지원 |
| 성능 | 단일 스레드 | 멀티 스레드 + SIMD |

**참조 파일:**
- `Assets/Scripts/04.Game/01.Entity/Squad/FlockBehavior.cs` — 5가지 힘 벡터 구현
- `Assets/Scripts/04.Game/02.System/Squad/FlockJob.cs` — Burst 병렬 구현
- `Assets/Scripts/04.Game/02.System/Squad/FlockJobRunner.cs` — NativeArray 생명주기 관리
- `Assets/Scripts/04.Game/02.System/Squad/Squad.cs` — 부대 이동 오케스트레이션
- `Assets/Scripts/04.Game/03.Data/FlockSettingsData.cs` — 가중치 SO
- `Assets/Docs/System/Optimization/flock_job_design.md` — Job System 설계 문서

---

### 7. 성능 최적화

**목적:** 200+ 유닛 환경에서 프레임드랍 없이 동작하기 위해 적용한 최적화 기법을 체계적으로 정리한다.

**포함할 내용:**

#### 7-1. SpatialGrid — 공간 해시 그리드

- **구조:** `SpatialGrid<T>` — 2D 공간 해시 그리드, O(1) 셀 룩업
- **셀 키:** `long` 비트 패킹 `((x << 32) | (uint)y)` — struct 생성 없이 해시 단순화
- **프레임 캐시:** `(cx, cy, range)` 동일 쿼리 결과 공유
  - 200유닛 밀집 시 TryGetValue 호출 85% 절감
  - `candidatePool` 공유 풀 + 슬롯 기반 인덱싱
- **코너 컬링:** 셀 간 AABB 최솟값 거리로 불필요한 셀 사전 배제
  - `cdx = max(0, |Δx| - 1) × cellSize`
  - range ≥ 4 부터 효과 발생 (탐지 범위 등 대형 쿼리에 유효)
- **GC-free 오버로드:** 호출자가 결과 List를 제공하여 할당 제거

**참조:** `Assets/Scripts/04.Game/02.System/Spatial/SpatialGrid.cs`

#### 7-2. Job System + Burst Compiler

- Flock 계산을 `IJobParallelFor`로 병렬 처리
- `[BurstCompile]` — SIMD 자동 벡터화
- 200유닛 브루트포스(N²=40,000 연산) < 0.1ms 이내 처리
- NativeArray를 Persistent 할당자로 관리, 크기 변경 시에만 재할당
- 기존 대비 예상 8~16배 성능 향상 (4코어 병렬 + Burst SIMD)

**참조:** `Assets/Scripts/04.Game/02.System/Squad/FlockJob.cs`, `FlockJobRunner.cs`

#### 7-3. 렌더링 최적화 — 배치 콜 최소화

- **URP (Universal Render Pipeline)** — SRP Batcher 활성화
- **SpriteAtlas** — 동일 텍스처 참조로 드로우 콜 합산
- **Custom Sort Axis** — Camera의 `TransparencySortMode.CustomAxis`를 Y축으로 설정
  - Z 좌표를 조작하지 않아 동적 배칭 가능
  - 레이어별 sortingOrder 간격 1000으로 안전한 분리
    ```
    Water=0, Ground=1000, Obstacle/Unit=2000, Fog=3000
    ```
- **결과:** 유닛 200+ 환경에서 Set Pass Call 70~150 유지

**참조:**
- `Assets/Scripts/04.Game/01.Entity/Common/SortingOrder.cs` — 레이어 상수
- `Assets/Scripts/04.Game/01.Entity/Player/QuarterViewCamera.cs` — CustomAxis 설정
- `Assets/Docs/System/Optimization/sprite_batching.md` — 배칭 최적화 문서

#### 7-4. GC 최적화

- **LINQ 제거:** `Aggregate()` 등 매 프레임 할당 코드 제거
- **for 루프 전환:** foreach 열거자 → for 인덱스 루프 (enumerator 오버헤드 제거)
- **위치 캐싱:** `context.MemberPositions[]` 배열로 Transform.position 접근 1회 제한
- **오브젝트 풀링:** `Facade.Pool`로 빈번한 Instantiate/Destroy 제거
- **FSM 조건 캐싱:** `SquadMemberFSM.EnemyInAttackRange()` — 프레임 내 중복 계산 방지

**참조:**
- `Assets/Docs/System/Optimization/gc_analysis.md` — GC 분석 문서
- `Assets/Docs/System/Optimization/optimization_status.md` — 최적화 현황

#### 7-5. 최적화 성과 요약 표

| 항목 | 최적화 전 | 최적화 후 |
|------|-----------|-----------|
| Flock 계산 | 단일 스레드, 40명/프레임 | Burst 병렬, 200+명/프레임 |
| SpatialGrid 쿼리 | 매 쿼리 셀 순회 | 프레임 캐시로 85% 절감 |
| Set Pass Call | (미측정) | 70~150 (200+ 유닛) |
| GC Alloc/Frame | LINQ·foreach 할당 | for 루프 + 캐싱으로 제거 |

---

### 8. 모듈화 및 재사용성

**목적:** asmdef 기반 모듈 분리와 인터페이스 설계를 통해 재사용 가능한 프레임워크를 구축한 점을 어필한다.

**포함할 내용:**

#### 8-1. Assembly Definition 기반 모듈 분리
- `Base.Runtime` — 핵심 프레임워크 (다른 프로젝트에 그대로 이식 가능)
  - Facade, Notifier, PageChanger, PopupManager, ObjectPool, SceneFlow 등
  - 외부 의존: UniTask, Newtonsoft.Json만
- `FiniteStateMachine.Runtime` — 범용 FSM 프레임워크
  - 제네릭 설계로 어떤 엔티티·트리거 타입이든 적용 가능
  - Base.Runtime에만 의존

#### 8-2. Facade 패턴의 재사용성
- 인터페이스 기반 14개 서비스 (`ILogger`, `IDatabase`, `IObjectPool` 등)
- 기본 구현체 제공 → 프로젝트별로 구현체만 교체하면 동작
- 씬 종속 서비스와 전역 서비스의 생명주기 분리

#### 8-3. 이식 가능한 UI 시스템
- `PageChanger` — 페이지 전환 + 히스토리 (UniTask 기반 비동기)
- `PopupManager` — 팝업 스택 + 정렬 순서 자동 관리
- `EscapeHandler` — 뒤로가기 처리 체인
- 어떤 프로젝트든 Facade에 등록만 하면 즉시 사용 가능

**참조 파일:**
- `Assets/Modules/Base/Runtime/Base.Runtime.asmdef`
- `Assets/Modules/FiniteStateMachine/Runtime/FiniteStateMachine.Runtime.asmdef`
- `Assets/Modules/Base/Runtime/Scripts/Facade/Facade.cs`

---

### 9. 테스트

**목적:** 독립 모듈에 대한 유닛 테스트가 작성되어 있음을 보여준다.

**포함할 내용:**
- 테스트 대상 모듈:
  - **Notifier (Event Bus):** 구독/발행, 중복 구독, 순회 중 구독 해제 등 엣지 케이스
  - **EnumLike:** 커스텀 열거형 유틸리티 테스트
  - **StateMachine (FSM):** 상태 전이, 커맨드 실행, 초기 상태 설정 등
- asmdef 기반 테스트 프로젝트 분리 (Base.Tests, FiniteStateMachine.Tests)
- NUnit 프레임워크 사용

**참조 파일:**
- `Assets/Modules/Base/Tests/NotifierTests.cs`
- `Assets/Modules/Base/Tests/EnumLikeTests.cs`
- `Assets/Modules/FiniteStateMachine/Tests/StateMachineTests.cs`

---

### 10. 개발 프로세스

**목적:** 체계적인 개발 프로세스와 문서화 문화를 보여주어 협업 역량을 어필한다.

**포함할 내용:**

#### 10-1. 6단계 워크플로우
```
컨셉(Concept) → 설계(Design) → 리뷰(Review) → 구현 계획(Plan) → 구현(Implementation) → TRD
```
- 각 시스템마다 이 흐름을 따라 설계 문서부터 작성
- 74개의 마크다운 문서가 이 프로세스의 산출물

#### 10-2. 문서 구조
```
Assets/Docs/System/Phase2_Core/
├── {시스템}/
│   ├── concept.md              — 시스템 개요
│   ├── design/design.md        — 아키텍처 설계
│   ├── review/review.md        — 설계 리뷰
│   ├── implementation_plan.md  — 구현 단계별 계획
│   └── trd_*.md                — 기술 참조 문서
```

#### 10-3. 코딩 컨벤션
- Allman(BSD) 스타일 중괄호
- 접근 제한자 항상 명시
- 클래스명 = 파일명 (PascalCase)
- 한 클래스 한 파일 원칙
- 비동기 메서드 Async 접미사

#### 10-4. 커밋 컨벤션
- 요약 한 줄 + 빈 줄 + 상세 bullet
- 관련 파일만 선별 스테이징 (git add . 금지)
- 커밋 전 조건: 컴파일 OK + 테스트 통과 + 에디터 실행 확인

---

## 작성 가이드라인

### 톤 & 스타일
- **간결하고 기술적인 톤** — 포트폴리오 리뷰어가 빠르게 핵심을 파악할 수 있도록
- **한국어 주 언어**, 기술 용어는 영어 병기 (예: 군집 알고리즘(Boid/Flock))
- **코드 스니펫은 최소한**으로 — 핵심 구조만 보여주는 짧은 코드 블록
- **다이어그램은 텍스트 기반** — Mermaid 또는 ASCII 아트

### 분량 가이드
- 전체 README 예상 분량: 400~600줄
- 각 섹션은 독립적으로 읽을 수 있게 구성
- 상세한 내용은 Assets/Docs/ 문서로 링크

### 시각 자료
- 스크린샷: `Assets/Screenshots/` 폴더의 이미지 활용
- 필요 시 추가 스크린샷 캡처 안내 (게임플레이 GIF 권장)

---

## 구현 순서

1. 스크린샷/GIF 자료 확인 및 준비
2. 섹션 1~3 작성 (개요, 기술 스택, 프로젝트 구조)
3. 섹션 4~5 작성 (핵심 시스템, 디자인 패턴)
4. 섹션 6 작성 (군집 알고리즘 — 가장 중요한 섹션)
5. 섹션 7 작성 (성능 최적화)
6. 섹션 8~10 작성 (모듈화, 테스트, 개발 프로세스)
7. 전체 리뷰 및 다듬기
