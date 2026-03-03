# FlockBehavior Job System 구현 가이드

## 1. 사전 준비 — 패키지 확인

`Window → Package Manager`에서 아래 패키지가 설치되어 있는지 확인한다.

| 패키지 | 최소 버전 |
|--------|-----------|
| Unity.Burst | 1.8+ |
| Unity.Collections | 2.1+ |
| Unity.Jobs | 0.70+ |

설치 안 된 경우: Package Manager → `+` → `Add by name` → 패키지명 입력.

---

## 2. FlockJob.cs — 핵심 병렬 연산

**경로**: `Assets/Scripts/04.Game/02.System/Squad/FlockJob.cs`

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// FlockBehavior 계산을 워커 스레드에서 병렬로 수행하는 Job.
/// Separation + Cohesion + Follow 세 힘을 계산한다.
/// Avoidance(ObstacleGrid)는 관리 코드 의존으로 메인 스레드에서 후처리한다.
/// </summary>
[BurstCompile]
public struct FlockJob : IJobParallelFor
{
    // ---- 입력 (ReadOnly) ----
    [ReadOnly] public NativeArray<Vector2> Positions;     // 전체 멤버 위치
    [ReadOnly] public NativeArray<bool>    StoppedFlags;  // 정지 여부 (정지 멤버는 zero 출력)
    [ReadOnly] public Vector2  LeaderPos;

    [ReadOnly] public float NeighborRadius;
    [ReadOnly] public float MinSeparationDistance;
    [ReadOnly] public float ArrivalRadius;
    [ReadOnly] public float SeparationWeight;
    [ReadOnly] public float CohesionWeight;
    [ReadOnly] public float FollowWeight;

    // ---- 출력 (WriteOnly) ----
    [WriteOnly] public NativeArray<Vector2> OutDirections;

    public void Execute(int index)
    {
        if (StoppedFlags[index])
        {
            OutDirections[index] = Vector2.zero;
            return;
        }

        Vector2 selfPos   = Positions[index];
        float sqrRadius   = NeighborRadius * NeighborRadius;
        float sqrMinSep   = MinSeparationDistance * MinSeparationDistance;

        Vector2 separation  = Vector2.zero;
        Vector2 cohesionSum = Vector2.zero;
        int     cohesionCnt = 0;

        // 이웃 순회 — Burst SIMD 벡터화로 200마리 브루트포스도 충분히 빠름
        for (int i = 0; i < Positions.Length; i++)
        {
            if (i == index) continue;

            Vector2 neighborPos = Positions[i];
            float   dx          = selfPos.x - neighborPos.x;
            float   dy          = selfPos.y - neighborPos.y;
            float   sqrDist     = dx * dx + dy * dy;

            if (sqrDist > sqrRadius) continue;

            // Separation: 역제곱 법칙 (sqrt 없음)
            if (sqrDist > 0f && sqrDist < sqrMinSep)
            {
                float scale = (sqrMinSep - sqrDist) / (sqrDist * sqrMinSep);
                separation.x += dx * scale;
                separation.y += dy * scale;
            }

            // Cohesion: 최소 거리 밖 이웃의 무게중심
            if (sqrDist >= sqrMinSep)
            {
                cohesionSum.x += neighborPos.x;
                cohesionSum.y += neighborPos.y;
                cohesionCnt++;
            }
        }

        // Cohesion 정규화
        Vector2 cohesion = Vector2.zero;
        if (cohesionCnt > 0)
        {
            float cx = cohesionSum.x / cohesionCnt - selfPos.x;
            float cy = cohesionSum.y / cohesionCnt - selfPos.y;
            float sqrMag = cx * cx + cy * cy;
            if (sqrMag > 1f)
            {
                float inv = 1f / Mathf.Sqrt(sqrMag);
                cx *= inv; cy *= inv;
            }
            cohesion = new Vector2(cx, cy);
        }

        // Follow: ArrivalRadius 이내 감속
        float lx     = LeaderPos.x - selfPos.x;
        float ly     = LeaderPos.y - selfPos.y;
        float lSqr   = lx * lx + ly * ly;
        Vector2 follow = Vector2.zero;
        if (lSqr >= ArrivalRadius * ArrivalRadius)
        {
            float dist = Mathf.Sqrt(lSqr);
            if (dist > ArrivalRadius * 2f)
            {
                follow = new Vector2(lx, ly);
            }
            else
            {
                float t = (dist - ArrivalRadius) / ArrivalRadius;
                follow = new Vector2(lx * t, ly * t);
            }
        }

        // 합산
        Vector2 combined = new Vector2(
            separation.x * SeparationWeight + cohesion.x * CohesionWeight + follow.x * FollowWeight,
            separation.y * SeparationWeight + cohesion.y * CohesionWeight + follow.y * FollowWeight
        );

        // 임계값 미만 클램핑
        if (Mathf.Abs(combined.x) < 0.2f) combined.x = 0f;
        if (Mathf.Abs(combined.y) < 0.2f) combined.y = 0f;

        // 정규화
        float mag = combined.x * combined.x + combined.y * combined.y;
        if (mag > 1f)
        {
            float inv = 1f / Mathf.Sqrt(mag);
            combined.x *= inv;
            combined.y *= inv;
        }

        OutDirections[index] = combined;
    }
}
```

---

## 3. FlockJobRunner.cs — NativeArray 수명 관리

**경로**: `Assets/Scripts/04.Game/02.System/Squad/FlockJobRunner.cs`

```csharp
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// FlockJob의 NativeArray를 관리하고 Schedule / Complete를 담당한다.
/// Squad가 소유하며, Squad.Dispose() 시 반드시 Dispose()를 호출해야 한다.
/// </summary>
public class FlockJobRunner : System.IDisposable
{
    private NativeArray<Vector2> positions;
    private NativeArray<bool>    stoppedFlags;
    private NativeArray<Vector2> outDirections;
    private int                  capacity;

    private readonly FlockSettingsData settings;

    public FlockJobRunner(FlockSettingsData settings)
    {
        this.settings = settings;
        Allocate(16); // 초기 용량
    }

    private void Allocate(int size)
    {
        if (positions.IsCreated)    positions.Dispose();
        if (stoppedFlags.IsCreated) stoppedFlags.Dispose();
        if (outDirections.IsCreated) outDirections.Dispose();

        positions     = new NativeArray<Vector2>(size, Allocator.Persistent);
        stoppedFlags  = new NativeArray<bool>   (size, Allocator.Persistent);
        outDirections = new NativeArray<Vector2>(size, Allocator.Persistent);
        capacity      = size;
    }

    /// <summary>
    /// Phase 1+2: 위치/정지 데이터 복사 후 Job 스케줄.
    /// Complete() 전에 results를 읽으면 안 된다.
    /// </summary>
    public JobHandle Schedule(
        IReadOnlyList<SquadMember> members,
        HashSet<SquadMember>       stopped,
        Vector2                    leaderPos)
    {
        int count = members.Count;
        if (count > capacity)
            Allocate(count * 2);

        // Phase 1: 메인 스레드 데이터 복사
        for (int i = 0; i < count; i++)
        {
            positions[i]    = members[i].Transform.position;
            stoppedFlags[i] = stopped.Contains(members[i]);
        }

        var job = new FlockJob
        {
            Positions            = positions,
            StoppedFlags         = stoppedFlags,
            LeaderPos            = leaderPos,
            NeighborRadius       = settings.neighborRadius,
            MinSeparationDistance = settings.minSeparationDistance,
            ArrivalRadius        = settings.arrivalRadius,
            SeparationWeight     = settings.separationWeight,
            CohesionWeight       = settings.cohesionWeight,
            FollowWeight         = settings.followWeight,
            OutDirections        = outDirections,
        };

        // innerloopBatchCount: 4~16 권장 (멤버 수에 따라 튜닝)
        return job.Schedule(count, 8);
    }

    /// <summary>
    /// Phase 3: Job 완료 대기 후 결과를 멤버에 적용한다.
    /// Avoidance 후처리도 여기서 수행한다.
    /// </summary>
    public void Complete(
        JobHandle              handle,
        IReadOnlyList<SquadMember> members,
        ObstacleGrid           obstacleGrid)
    {
        handle.Complete();

        int count = members.Count;
        for (int i = 0; i < count; i++)
        {
            var dir = outDirections[i];

            // Avoidance 후처리 (ObstacleGrid는 관리 코드 → 메인 스레드)
            if (obstacleGrid != null && dir != Vector2.zero)
            {
                Vector2 pos = members[i].Transform.position;
                dir = new Vector2(
                    obstacleGrid.IsWalkable(new Vector2(pos.x + Mathf.Sign(dir.x) * 0.5f, pos.y)) ? dir.x : 0f,
                    obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + Mathf.Sign(dir.y) * 0.5f)) ? dir.y : 0f
                );
            }

            members[i].SetMoveDirection(dir);
        }
    }

    public void Dispose()
    {
        if (positions.IsCreated)     positions.Dispose();
        if (stoppedFlags.IsCreated)  stoppedFlags.Dispose();
        if (outDirections.IsCreated) outDirections.Dispose();
    }
}
```

---

## 4. Squad.cs 수정 — Job 경로로 전환

### 4-1. 필드 추가

```csharp
// 기존 flock 필드 옆에 추가
private FlockJobRunner jobRunner;
```

### 4-2. 생성자 수정

```csharp
public Squad()
{
    var flockSettings = Facade.DB.Get<FlockSettingsData>("PlayerSquadFlock");
    flock      = new FlockBehavior(flockSettings);  // 에디터 디버그용 유지
    jobRunner  = new FlockJobRunner(flockSettings); // Job 경로

    var squadSettings = Facade.DB.Get<SquadSettingsData>("SquadSettings");
    if (squadSettings != null)
    {
        StopRadius       = squadSettings.stopRadius;
        MemberStopRadius = squadSettings.memberStopRadius;
    }
}
```

### 4-3. Update() — 3 Phase 구조

```csharp
public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime)
{
    Vector2 leaderPos = leader.position;

    // ── 1단계: 정지 판정 (기존과 동일) ──────────────────────────
    stopped.Clear();
    foreach (var member in members)
        if (Vector2.Distance((Vector2)member.Transform.position, leaderPos) <= StopRadius)
            stopped.Add(member);

    stopSnapshot.Clear();
    foreach (var s in stopped) stopSnapshot.Add(s);
    foreach (var member in members)
    {
        if (stopped.Contains(member)) continue;
        foreach (var s in stopSnapshot)
            if (Vector2.Distance((Vector2)member.Transform.position, (Vector2)s.Transform.position) <= MemberStopRadius)
            { stopped.Add(member); break; }
    }

    // ── 2단계: Job 스케줄 (Phase 1+2) ────────────────────────────
    var jobHandle = jobRunner.Schedule(members, stopped, leaderPos);

    // ── 3단계: Combat Tick (Job 실행 중 메인 스레드 작업 병행) ────
    foreach (var member in members)
        member.Combat.Tick(deltaTime);

    // ── 4단계: Job 완료 + 방향 적용 (Phase 3) ────────────────────
    jobRunner.Complete(jobHandle, members, obstacleGrid);

    // ── 5단계: FSM 구동 ───────────────────────────────────────────
    foreach (var member in members)
    {
#if UNITY_EDITOR
        if (Time.frameCount % 10 == 0)
        {
            var ctx = new SquadContext(members, memberPosCache, leader, obstacleGrid);
            member.SetFlockDebug(flock.ComputeDebugData(member, in ctx));
        }
#endif
        member.Update();
    }
}
```

> **포인트**: `Combat.Tick()`을 Job.Schedule과 job.Complete() 사이에 배치하면,
> Combat 처리가 진행되는 동안 워커 스레드에서 Flock 계산이 동시에 실행된다.

---

## 5. Squad.cs — Dispose 추가

Squad가 IDisposable을 구현하거나 명시적 정리 메서드를 제공해야 한다.

```csharp
public void Dispose()
{
    jobRunner?.Dispose();
}
```

Squad를 소유하는 클래스(예: `PlayerSystem`, `GameController`)에서 게임 종료/씬 전환 시 `squad.Dispose()`를 호출해야 한다.

---

## 6. 스태거드 업데이트 제거

Job System 전환 후 Squad.Update()의 `i % 5 == frameBucket` 조건은 제거한다. 200마리 전체를 매 프레임 Job으로 계산해도 메인 스레드 비용이 Phase 1+3(데이터 복사 + 결과 적용)으로만 줄어든다.

---

## 7. 검증 방법

### 컴파일 확인

Unity Console에서 오류 없음 확인. Burst Inspector(`Jobs → Burst Inspector`)에서 `FlockJob`이 컴파일됐는지 확인한다.

### 프로파일러 확인

Deep Profile 모드에서:
- `FlockJob` 항목이 **Worker Thread** 레인에 표시되는지 확인
- 메인 스레드에서 `FlockBehavior.CalculateDirection` 호출이 사라졌는지 확인
- `jobRunner.Complete()` 대기 시간이 짧은지 확인 (길면 Job이 메인 스레드와 겹치지 않음)

### 정상 동작 확인

- 200마리 스쿼드가 플레이어를 자연스럽게 추종하는지
- 장애물 회피(Avoidance)가 여전히 작동하는지
- 멤버 사망/추가 시 NativeArray가 올바르게 리사이즈되는지

---

## 8. 주의 사항

| 상황 | 대응 |
|------|------|
| `NativeArray is not created` 예외 | Dispose 후 접근 시 발생. jobRunner.Complete() 후에만 결과 접근 |
| Job이 Complete() 전에 NativeArray 수정 | 경쟁 조건 발생. Phase 1에서 복사 완료 후 Schedule해야 함 |
| 멤버 수 증가 시 배열 재할당 | FlockJobRunner.Schedule()에서 자동 처리 (2× 확장) |
| Play Mode 종료 시 NativeArray 누수 | Squad.Dispose() 미호출 시 발생. Domain Reload 경고 확인 |
| Burst 비활성화 상태 테스트 | `Jobs → Burst → Enable Compilation` 토글로 성능 비교 가능 |
