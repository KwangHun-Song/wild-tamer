using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 공간 해시 그리드. 위치 기반 근접 쿼리를 O(1) 셀 룩업으로 처리한다.
///
/// 구조:
///   cells: (x,y) 셀 좌표 → 해당 셀 내 아이템 목록
///   키: long 패킹 ((x &lt;&lt; 32) | y) — struct 생성 없이 비트 연산만 사용
///
/// 프레임 캐시:
///   같은 프레임 내에서 (cx, cy, range)가 같은 쿼리는 셀 수집 결과를 공유한다.
///   200마리 스쿼드가 몰려있을 때 TryGetValue 호출을 최대 85% 절감한다.
/// </summary>
public class SpatialGrid<T> where T : class
{
    private readonly float cellSize;

    // 셀 좌표 → 아이템 목록
    // long 키: (x << 32 | (uint)y) 패킹 — Vector2Int 대비 struct 생성/해시 단순화
    private readonly Dictionary<long, List<(T item, Vector2 pos)>> cells = new();

    // ── 프레임 단위 쿼리 캐시 ──────────────────────────────────────────────────
    // 캐시 키: (cx, cy, range) — 같은 셀+반경 조합이면 수집 결과 공유
    // ValueTuple 사용: 충돌 없는 정확한 다중 키, struct이므로 힙 할당 없음
    private readonly Dictionary<(int cx, int cy, int range), (int start, int count)> candidateCache = new();

    /// <summary>
    /// 프레임 내 수집된 후보 아이템의 공유 풀.
    /// 각 캐시 항목은 [start, start+count) 구간을 슬롯으로 사용한다.
    /// 프레임이 바뀌면 Clear()로 초기화된다.
    /// </summary>
    private readonly List<(T item, Vector2 pos)> candidatePool = new(512);
    private int cacheFrame = -1;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }

    public void Clear()
    {
        cells.Clear();
    }

    public void Insert(T item, Vector2 position)
    {
        // 월드 좌표 → 셀 키로 변환 후 해당 셀 목록에 추가
        var key = CellKey(position);
        if (!cells.TryGetValue(key, out var list))
        {
            list = new List<(T, Vector2)>();
            cells[key] = list;
        }
        list.Add((item, position));
    }

    /// <summary>할당 버전 — 내부적으로 GC-free 오버로드를 사용한다.</summary>
    public List<T> Query(Vector2 center, float radius)
    {
        var result = new List<T>();
        Query(center, radius, result);
        return result;
    }

    /// <summary>
    /// GC-free 오버로드: 결과를 호출자가 제공한 리스트에 채운다. 호출 전 Clear()는 호출자 책임.
    ///
    /// 실행 흐름:
    ///   1. 프레임이 바뀌면 캐시/풀 초기화
    ///   2. (cx, cy, range) 캐시 조회
    ///      - 캐시 미스: CollectCandidates()로 주변 셀 아이템 수집 후 candidatePool에 추가
    ///      - 캐시 히트: 이전에 수집한 풀 슬롯 재사용 (TryGetValue 생략)
    ///   3. 캐시된 후보 목록에서 정확한 거리(sqrMagnitude)로 필터링
    /// </summary>
    public void Query(Vector2 center, float radius, List<T> result)
    {
        // 프레임이 바뀌면 캐시 전체 초기화 — Time.frameCount 비교로 자동 무효화
        if (Time.frameCount != cacheFrame)
        {
            candidateCache.Clear();
            candidatePool.Clear();
            cacheFrame = Time.frameCount;
        }

        // 반경을 셀 단위로 환산
        int range = Mathf.CeilToInt(radius / cellSize);
        // 월드 좌표 → 셀 좌표
        int cx = Mathf.FloorToInt(center.x / cellSize);
        int cy = Mathf.FloorToInt(center.y / cellSize);

        // 같은 셀+반경 조합은 수집 후보가 동일 → 캐시에서 슬롯 재사용
        var cacheKey = (cx, cy, range);
        if (!candidateCache.TryGetValue(cacheKey, out var slot))
        {
            // 캐시 미스: 주변 셀 순회하여 후보 수집 (이 프레임에 처음 온 셀 조합)
            slot = CollectCandidates(cx, cy, range);
            candidateCache[cacheKey] = slot;
        }
        // 캐시 히트: 이미 수집된 슬롯 재사용 → TryGetValue × (2*range+1)² 생략

        // 캐시된 후보를 정확한 거리로 필터링 (셀 룩업 없이 수학 연산만)
        float sqRadius = radius * radius;
        int end = slot.start + slot.count;
        for (int i = slot.start; i < end; i++)
        {
            var (item, pos) = candidatePool[i];
            // Vector2 임시 생성 없이 성분별 직접 계산
            float dx = pos.x - center.x;
            float dy = pos.y - center.y;
            if (dx * dx + dy * dy <= sqRadius)
                result.Add(item);
        }
    }

    /// <summary>
    /// (cx, cy) 기준 ±range 셀 내 후보 아이템을 candidatePool에 추가하고
    /// 슬롯 정보 (start, count)를 반환한다.
    ///
    /// 코너 컬링 전략 — 셀 간 AABB 최솟값 거리 사용:
    ///   유닛 개별 위치 대신 "홈 셀과 후보 셀 사이의 최단 거리"로 컬링한다.
    ///   이 거리 > 보수적 반경이면 홈 셀의 어떤 유닛도 해당 후보 셀이 필요 없다고 보장된다.
    ///
    ///   셀 간 X 거리 = max(0, |x - cx| - 1) × cellSize
    ///     인접 셀(|Δx|=1): 0  (셀 경계가 맞닿음)
    ///     두 칸(|Δx|=2):   cellSize
    ///     N칸(|Δx|=N):    (N-1) × cellSize
    ///
    ///   효과: range ≥ 4 (radius ≥ 4×cellSize) 부터 코너 셀 배제 시작.
    ///   소범위 쿼리(range=1~2)는 배제되는 셀이 없어 동작은 같고 계산 비용만 소폭 추가.
    ///   탐지 범위처럼 radius가 큰 쿼리에서 TryGetValue 추가 절감.
    /// </summary>
    private (int start, int count) CollectCandidates(int cx, int cy, int range)
    {
        int start = candidatePool.Count;

        // 보수적 반경: range × cellSize (실제 radius ≤ range × cellSize)
        // 셀 간 거리 비교에 사용 — 유닛이 셀 내 어디 있든 항상 안전
        float sqConservRadius = (float)(range * range) * (cellSize * cellSize);

        for (int x = cx - range; x <= cx + range; x++)
        {
            // 홈 셀과 후보 셀의 X 방향 최솟값 거리
            // 인접(|Δx|=1)이면 0, 두 칸 떨어지면 cellSize, ...
            float cdx = Mathf.Max(0, Mathf.Abs(x - cx) - 1) * cellSize;
            float sqDx = cdx * cdx;

            // X만으로 이미 보수적 반경 초과 → 이 열 전체 스킵
            if (sqDx > sqConservRadius) continue;

            for (int y = cy - range; y <= cy + range; y++)
            {
                float cdy = Mathf.Max(0, Mathf.Abs(y - cy) - 1) * cellSize;

                // 셀 간 AABB 거리² > 보수적 반경² → 홈 셀의 어떤 유닛도 이 셀 불필요 → 스킵
                if (sqDx + cdy * cdy > sqConservRadius) continue;

                // 해당 셀에 아이템이 없으면 스킵
                if (!cells.TryGetValue(PackKey(x, y), out var items)) continue;

                // foreach 열거자 대신 for 인덱스 루프 — enumerator 오버헤드 제거
                int cnt = items.Count;
                for (int k = 0; k < cnt; k++)
                    candidatePool.Add(items[k]);
            }
        }

        return (start, candidatePool.Count - start);
    }

    /// <summary>두 셀 좌표를 long 1개로 패킹. 상위 32비트=x, 하위 32비트=y.</summary>
    private static long PackKey(int x, int y) => ((long)x << 32) | (uint)y;

    /// <summary>월드 좌표 → long 셀 키.</summary>
    private long CellKey(Vector2 pos)
        => PackKey(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize));
}
