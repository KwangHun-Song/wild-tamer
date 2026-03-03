using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid<T> where T : class
{
    private readonly float cellSize;

    // 키: (x << 32 | (uint)y) long 패킹
    // Vector2Int 대비 struct 생성 없이 비트 연산만으로 키 생성 + long.GetHashCode 단순화
    private readonly Dictionary<long, List<(T item, Vector2 pos)>> cells = new();

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
    /// 알고리즘:
    ///   1. 반경을 셀 단위로 환산해 AABB 범위(range) 결정
    ///   2. 중심 셀 ±range 정사각형을 순회
    ///   3. 각 셀 AABB와 원의 교차 여부를 사전 체크(코너 셀 컬링)
    ///   4. 교차하는 셀에서만 아이템별 정밀 거리 검사
    /// </summary>
    public void Query(Vector2 center, float radius, List<T> result)
    {
        // 반경을 셀 단위로 환산 — 몇 개의 셀 행/열을 검색할지 결정
        int range = Mathf.CeilToInt(radius / cellSize);
        float sqRadius = radius * radius;

        // 중심 월드 좌표 → 셀 좌표
        int cx = Mathf.FloorToInt(center.x / cellSize);
        int cy = Mathf.FloorToInt(center.y / cellSize);

        // 중심 셀 기준 ±range 범위의 정사각형 AABB를 순회
        for (int x = cx - range; x <= cx + range; x++)
        {
            // 셀 X 범위의 최근접 점 (셀 밖이면 경계값, 안이면 center.x)
            float nearX = Mathf.Clamp(center.x, x * cellSize, (x + 1) * cellSize);
            float edgeDx = nearX - center.x;

            for (int y = cy - range; y <= cy + range; y++)
            {
                // 셀 Y 범위의 최근접 점
                float nearY = Mathf.Clamp(center.y, y * cellSize, (y + 1) * cellSize);
                float edgeDy = nearY - center.y;

                // 셀 AABB와 원의 교차 여부 — 코너 셀 조기 컬링
                // 셀에서 원 중심까지 가장 가까운 점이 반경 밖이면 이 셀 전체 스킵
                if (edgeDx * edgeDx + edgeDy * edgeDy > sqRadius) continue;

                // 해당 셀에 등록된 아이템이 없으면 스킵
                if (!cells.TryGetValue(PackKey(x, y), out var items)) continue;

                // foreach 열거자 대신 for 인덱스 루프 — enumerator 오버헤드 제거
                int count = items.Count;
                for (int k = 0; k < count; k++)
                {
                    var (item, pos) = items[k];

                    // 아이템별 정밀 거리 검사 — Vector2 임시 객체 없이 성분별 계산
                    float pdx = pos.x - center.x;
                    float pdy = pos.y - center.y;
                    if (pdx * pdx + pdy * pdy <= sqRadius)
                        result.Add(item);
                }
            }
        }
    }

    /// <summary>두 셀 좌표를 long 1개로 패킹. 상위 32비트=x, 하위 32비트=y.</summary>
    private static long PackKey(int x, int y) => ((long)x << 32) | (uint)y;

    /// <summary>월드 좌표 → long 셀 키.</summary>
    private long CellKey(Vector2 pos)
        => PackKey(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize));
}
