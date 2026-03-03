# Wild Tamer Like

> **NavMesh 없이 구현한 군집 알고리즘(Boid) 기반 2D 쿼터뷰 테이밍 RPG**

## 코어 루프

```
탐험 → 전투 → 테이밍 → 부대 확장 → 탐험
```

플레이어가 월드를 탐험하며 몬스터와 전투하고, 처치한 몬스터를 일정 확률로 테이밍하여 부대원으로 편입합니다.
부대가 커질수록 더 강한 적과 보스에 도전할 수 있는 순환 구조입니다.

## 핵심 특징

- **NavMesh 없이 직접 구현한 군집 알고리즘** — Craig Reynolds의 Boid 모델 기반, 5가지 힘 벡터 합산
- **Unity Job System + Burst Compiler** — 200+ 유닛 프레임드랍 없이 병렬 처리
- **모듈화된 프레임워크** — FSM, Event Bus, Facade, UI 시스템을 asmdef 기반 독립 모듈로 분리
- **배치 최적화** — SpriteAtlas + Custom Sort Axis로 Set Pass Call 70~150 유지
- **71개 설계 문서와 체계적 개발 프로세스** — 컨셉 → 설계 → 리뷰 → 구현 계획 → 구현 → TRD

---

## 목차

1. [기술 스택](#기술-스택)
2. [프로젝트 구조](#프로젝트-구조)
3. [핵심 시스템 설계](#핵심-시스템-설계)
4. [디자인 패턴](#디자인-패턴)
5. [군집 알고리즘 심층 소개](#군집-알고리즘-심층-소개)
6. [성능 최적화](#성능-최적화)
7. [모듈화 및 재사용성](#모듈화-및-재사용성)
8. [테스트](#테스트)
9. [개발 프로세스](#개발-프로세스)

---

## 기술 스택

| 카테고리 | 기술 | 설명 |
|---------|------|------|
| 엔진 | Unity 2022 LTS | 2D 쿼터뷰 RPG |
| 렌더링 | URP | SRP Batcher 활성화, 2D Sprite 렌더링 |
| 병렬 처리 | Job System + Burst | 군집 계산 멀티스레드 + SIMD 병렬화 |
| 비동기 | UniTask | async/await 기반 비동기 처리 |
| 애니메이션 | DOTween | 트윈 기반 연출 |
| 직렬화 | Newtonsoft.Json | JSON 기반 데이터 직렬화 |
| 고성능 컬렉션 | Unity.Collections | NativeArray 등 Job System용 비관리 메모리 |
| 스프라이트 | SpriteAtlas | 배치 콜 최소화 |
| 데이터 관리 | ScriptableObject | Inspector에서 밸런스 조정 가능한 데이터 테이블 |
| 테스트 | NUnit | Unity Test Framework 기반 유닛 테스트 |

---

## 프로젝트 구조

```
Assets/
├── Scripts/                    — C# 스크립트 127개
│   ├── 00.Common/              — 공통 유틸리티
│   ├── 01.Scene/               — Scene 진입 FSM (PlayStates)
│   ├── 02.Page/                — UI 페이지 (PlayPage 등)
│   ├── 03.Popup/               — UI 팝업 (Setting, Collection 등)
│   ├── 04.Game/
│   │   ├── 01.Entity/          — Player, Monster, Squad, Boss
│   │   ├── 02.System/          — Combat, Spatial, Squad, Map, VFX
│   │   ├── 03.Data/            — ScriptableObject 데이터
│   │   └── 05.Utility/         — 게임 유틸리티
│   └── 05.Utility/             — 범용 유틸리티
├── Modules/                    — asmdef 기반 독립 모듈 (53개 스크립트)
│   ├── Base/                   — 핵심 프레임워크
│   └── FiniteStateMachine/     — 범용 FSM
├── Docs/                       — 설계 문서 71개
└── Screenshots/                — 스크린샷
```

**모듈 의존성:**

```
Assembly-CSharp (Game)
  └─→ Base.Runtime ─→ UniTask, Newtonsoft.Json
  └─→ FiniteStateMachine.Runtime ─→ Base.Runtime
```

> 총 **180개** C# 스크립트, **71개** 설계 문서, **30+** 프리팹

---

## 핵심 시스템 설계

### 게임 컨트롤러 — 중앙 오케스트레이터

`GameController`는 순수 C# 클래스(MonoBehaviour 아님)로, 모든 시스템을 소유하고 Update 순서를 제어합니다.

```
입력 처리 → Player → 부대 이동 → 몬스터 AI → 몬스터 스쿼드 → 보스 스폰 → 전투
```

게임 페이즈(Play/Pause) 기반 게이팅과 이벤트 기반 시스템 간 연결로 느슨한 결합을 유지합니다.

### 엔티티 시스템 — Model-View 분리

| 계층 | 역할 | 클래스 예시 |
|------|------|-------------|
| **Model** (순수 C#) | 로직, 상태, 데이터 | `Player`, `SquadMember`, `Monster`, `BossMonster` |
| **View** (MonoBehaviour) | 렌더링, 애니메이션, 물리 | `PlayerView`, `SquadMemberView`, `MonsterView` |

Model은 MonoBehaviour 의존이 없어 독립적으로 테스트할 수 있습니다.

**프리팹 구조:** `Root(Animator + UnitMovement) → Visual(SpriteRenderer)` — Animator를 루트에 배치하여 Animation Clip 경로 바인딩 보장

### 자동 전투 시스템

- `CombatSystem` — SpatialGrid 기반 적 탐지, 자동 타겟팅
- `DamageProcessor` — 데미지 계산 및 적용
- `TamingSystem` — 확률 기반 테이밍 판정, 부대 합류 처리
- `Notifier`를 통한 전투 이벤트 브로드캐스트 (피격, 사망, 테이밍)

### 월드맵 시스템

- `MapGenerator` — 타일맵 기반 프로시저럴 맵 생성
- `ObstacleGrid` — 장애물 그리드 (이동 가능 여부 판정)
- `FogOfWar` — 플레이어 시야 기반 전장의 안개
- `MapDecorationGenerator` — 환경 오브젝트 배치

### 보스 시스템

`BossMonster` / `BossFSM`이 전용 상태 머신으로 보스를 제어합니다.

**7종 공격 패턴** (각각 독립 클래스로 캡슐화):

`ChargePattern` · `CrossZonePattern` · `CurseMarkPattern` · `ProjectileBarragePattern` · `SummonMinionsPattern` · `TrackingZonePattern` · `XZonePattern`

패턴별 `ZoneIndicator` 프리팹으로 위험 지역을 시각화하며, `BossSpawnSystem`이 타이머 기반으로 보스를 등장시킵니다.

### 데이터 영속성

`GameSaveManager`가 JSON 기반 로컬 Save/Load를 처리합니다. `GameSaveData`에 플레이어 위치, HP, 부대원, 보스 타이머 등 게임 스냅샷을 직렬화합니다.

### UI 시스템

- `PageChanger` — 비동기 페이지 전환, Stack 기반 히스토리 & 뒤로가기
- `PopupManager` — 팝업 스택 관리, 정렬 순서 자동 계산
- `SceneStateMachine` — Scene 진입 시 FSM으로 초기화 흐름 관리
- UniTask 기반 비동기 전환 + 팝업 결과 반환 (`UniTask<T>`)

### 전투 연출 (VFX)

`HitStop`(역경직) · `CameraShake`(화면 흔들림) · `HitEffectPlayer`(타격 이펙트) — 오브젝트 풀링과 `AutoDespawn`으로 이펙트 수명을 관리합니다.

---

## 디자인 패턴

| 패턴 | 적용 위치 | 이점 |
|------|-----------|------|
| **FSM** | 유닛 AI, Scene 흐름, Boss | 제네릭 `StateMachine<TEntity, TEnumTrigger>`로 Player, Monster, Squad, Boss, Scene에 범용 적용 |
| **Facade** | Base 모듈 | 14개 서비스를 단일 정적 진입점으로 제공, 인터페이스 기반 구현체 교체 |
| **Observer** | Notifier 시스템 | `Notifier` / `GlobalNotifier` 타입 기반 이벤트로 시스템 간 느슨한 결합 |
| **Object Pool** | 엔티티, 이펙트 | `DefaultObjectPool` Dictionary 기반 풀로 GC 부하 제거 |
| **Strategy** | 보스 공격 패턴 | 7종 `IBossPattern` 구현체, BossFSM이 전략 선택 |
| **Model-View** | 엔티티 전체 | 순수 C# Model + MonoBehaviour View로 로직/렌더링 완전 분리 |
| **Singleton** | 부트스트래퍼 | `Singleton<T>` DontDestroyOnLoad 기반 전역 오브젝트 |
| **데이터 주도 설계** | 전체 밸런스 | 모든 게임 수치를 ScriptableObject로 테이블화 |

### Facade 서비스 목록

```
Facade
├── Logger        : ILogger            ├── PageChanger  : IPageChanger
├── Json          : IJsonSerializer    ├── PopupManager : IPopupManager
├── Time          : ITimeProvider      ├── Coroutine    : ICoroutineRunner
├── Data          : IDataStore         ├── Sound        : ISoundManager
├── DB            : IDatabase          ├── Escape       : IEscapeHandler
├── Pool          : IObjectPool        ├── Scene        : ISceneChanger
├── Loader        : IInstanceLoader    └── Transition   : ISceneTransition
```

---

## 군집 알고리즘 심층 소개

이 프로젝트의 핵심 기술적 차별점입니다. **NavMesh를 사용하지 않고**, Craig Reynolds의 Boid 알고리즘을 기반으로 5가지 힘 벡터를 합산하여 이동 방향을 결정합니다.

### 5가지 힘 벡터

```
                        ┌── Separation (분리) ─── 이웃과 최소 거리 유지
                        │                         역제곱 법칙으로 가까울수록 강한 반발
                        │
                        ├── Cohesion (응집) ───── 이웃 무게중심 방향으로 이동
                        │
  최종 이동 벡터 = Σ ── ├── Alignment (정렬) ──── 이웃과 같은 방향으로 이동
                        │
                        ├── Follow (추종) ─────── 리더(플레이어) 방향으로 이동
                        │                         ArrivalRadius 이내 거리 비례 감속
                        │
                        └── Avoidance (회피) ──── ObstacleGrid 기반 장애물 회피
```

### 역제곱 Separation 수식

sqrt 연산을 완전히 제거한 핵심 수식입니다:

```csharp
separationSum += diff * ((sqrMinSep - sqrDist) / (sqrDist * sqrMinSep));
```

경계(`sqrDist == sqrMinSep`)에서 0, 거리 0에서 최대값 — 모든 거리 비교를 `sqrMagnitude`로 처리하여 **sqrt 완전 배제**.

### 가중치 시스템

`FlockSettingsData`(ScriptableObject)로 모든 가중치와 반경을 관리합니다:

| 파라미터 | 기본값 | 설명 |
|---------|--------|------|
| Separation | 1.5 | 분리 가중치 |
| Cohesion | 1.0 | 응집 가중치 |
| Follow | 2.0 | 추종 가중치 |
| Avoidance | 2.0 | 회피 가중치 |
| NeighborRadius | - | 이웃 감지 반경 |
| ArrivalRadius | - | 감속 시작 반경 |
| MinSeparationDistance | - | 최소 분리 거리 |

### Job System 병렬화 — 3단계 파이프라인

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Main Thread     Transform → NativeArray 복사                  │
├─────────────────────────────────────────────────────────────────┤
│ 2. Worker Threads  Separation + Cohesion + Follow 병렬 계산      │
│                    FlockJob : IJobParallelFor + [BurstCompile]   │
│                    innerloopBatchCount: 8                        │
├─────────────────────────────────────────────────────────────────┤
│ 3. Main Thread     결과 적용 + Avoidance 후처리 (관리 코드 의존)   │
└─────────────────────────────────────────────────────────────────┘
```

`Allocator.Persistent`로 NativeArray를 풀링하고, 부대 크기 변경 시에만 재할당합니다.

### 두 가지 구현의 역할 분담

| | FlockBehavior (C#) | FlockJob (Burst) |
|---|---|---|
| **용도** | 플레이어 부대 (에디터 디버그) | 야생 몬스터 부대 (성능 우선) |
| **장애물 회피** | 직접 처리 | 메인 스레드 후처리 |
| **디버그** | Gizmo 시각화 지원 | 미지원 |
| **성능** | 단일 스레드 | 멀티 스레드 + SIMD |

---

## 성능 최적화

### SpatialGrid — 공간 해시 그리드

`SpatialGrid<T>`는 O(1) 셀 룩업을 제공하는 2D 공간 해시 그리드입니다.

**셀 키 — struct 생성 없는 `long` 비트 패킹:**

```csharp
private static long PackKey(int x, int y) => ((long)x << 32) | (uint)y;
```

**프레임 캐시:** 동일 `(cx, cy, range)` 쿼리 결과를 공유하여 200유닛 밀집 시 TryGetValue 호출 **85% 절감**

**코너 컬링:** 셀 간 AABB 최솟값 거리로 불필요한 셀을 사전 배제 (range ≥ 4 대형 쿼리에 유효)

### Job System + Burst Compiler

- Flock 계산을 `IJobParallelFor`로 멀티스레드 병렬 처리
- `[BurstCompile]`로 SIMD 자동 벡터화
- 200유닛 브루트포스(N²=40,000 연산)를 **0.1ms 이내** 처리
- 기존 대비 **8~16배 성능 향상** (4코어 병렬 + Burst SIMD)

### 렌더링 최적화 — 배치 콜 최소화

- **SRP Batcher** 활성화 (URP)
- **SpriteAtlas** — 동일 텍스처 참조로 드로우 콜 합산
- **Custom Sort Axis** — Camera의 `TransparencySortMode.CustomAxis`를 Y축으로 설정하여 Z 조작 없이 동적 배칭 유지
- 레이어별 sortingOrder 간격 1000으로 안전한 분리 (`Water=0, Ground=1000, Unit=2000, Fog=3000`)
- **결과:** 200+ 유닛 환경에서 **Set Pass Call 70~150** 유지

### GC 최적화

- LINQ 제거 → for 인덱스 루프 전환
- 위치 캐싱 → Transform.position 접근 1회 제한
- 오브젝트 풀링 → `Facade.Pool`로 Instantiate/Destroy 제거
- static 배열 재사용 → 매 호출 할당 제거

### 최적화 성과 요약

| 항목 | Before | After |
|------|--------|-------|
| Flock 계산 | 단일 스레드, 40명/프레임 | Burst 병렬, 200+명/프레임 |
| SpatialGrid 쿼리 | 매 쿼리 셀 순회 | 프레임 캐시로 85% 절감 |
| Set Pass Call | 미최적화 | 70~150 (200+ 유닛) |
| GC Alloc/Frame | LINQ·foreach 할당 | for 루프 + 캐싱으로 제거 |

---

## 모듈화 및 재사용성

### Assembly Definition 기반 모듈 분리

**`Base`** — 다른 프로젝트에 그대로 이식 가능한 핵심 프레임워크

- Facade, Notifier, PageChanger, PopupManager, ObjectPool, SceneFlow 등
- 외부 의존: UniTask, Newtonsoft.Json만

**`FiniteStateMachine`** — 범용 FSM 프레임워크

- 제네릭 설계로 어떤 엔티티·트리거 타입이든 적용 가능
- Base 의존

### Facade 패턴의 재사용성

14개 서비스를 인터페이스 기반으로 제공하여 프로젝트별로 **구현체만 교체**하면 동작합니다.
씬 종속 서비스(`PageChanger`, `PopupManager`)와 전역 서비스의 생명주기를 분리합니다.

### 이식 가능한 UI 시스템

- `PageChanger` — 페이지 전환 + 히스토리 (UniTask 비동기)
- `PopupManager` — 팝업 스택 + 정렬 순서 자동 관리
- `EscapeHandler` — 뒤로가기 처리 체인
- Facade에 등록만 하면 어떤 프로젝트에서든 사용 가능

---

## 테스트

asmdef 기반으로 테스트 프로젝트를 분리하여 NUnit 프레임워크로 유닛 테스트를 수행합니다.

| 테스트 대상 | 주요 테스트 항목 |
|------------|----------------|
| **Notifier** (Event Bus) | 구독/발행, 중복 구독, 순회 중 구독 해제 등 엣지 케이스 |
| **StateMachine** (FSM) | 상태 전이, 커맨드 실행, 초기 상태 설정 |
| **EnumLike** | 커스텀 열거형 유틸리티 |

```
Modules/
├── Base/Tests/                     ← Base.Tests.asmdef
│   ├── NotifierTests.cs
│   └── EnumLikeTests.cs
└── FiniteStateMachine/Tests/       ← FiniteStateMachine.Tests.asmdef
    └── StateMachineTests.cs
```

---

## 개발 프로세스

### 6단계 워크플로우

```
컨셉(Concept) → 설계(Design) → 리뷰(Review) → 구현 계획(Plan) → 구현(Impl) → TRD
```

각 시스템마다 이 흐름을 따라 설계 문서부터 작성합니다. 71개의 마크다운 문서가 이 프로세스의 산출물입니다.

### 문서 구조

```
Assets/Docs/System/
└── {시스템}/
    ├── concept.md              — 시스템 개요
    ├── design/design.md        — 아키텍처 설계
    ├── review/review.md        — 설계 리뷰
    ├── implementation_plan.md  — 구현 단계별 계획
    └── trd_*.md                — 기술 참조 문서
```

### 코딩 컨벤션

- Allman(BSD) 스타일 중괄호
- 접근 제한자 항상 명시
- 클래스명 = 파일명 (PascalCase)
- 한 클래스 한 파일 원칙
- 비동기 메서드 `Async` 접미사

### 커밋 컨벤션

- 요약 한 줄 + 빈 줄 + 상세 bullet
- 관련 파일만 선별 스테이징 (`git add .` 금지)
- 커밋 전 조건: 컴파일 OK + 테스트 통과 + 에디터 실행 확인

---

## 에셋 크레딧

이미지 에셋은 무료 에셋을 활용하였습니다.

- **Tiny Swords** by Pixel Frog — [pixelfrog-assets.itch.io/tiny-swords](https://pixelfrog-assets.itch.io/tiny-swords)
