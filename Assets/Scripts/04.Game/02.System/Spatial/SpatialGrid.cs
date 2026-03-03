using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid<T> where T : class
{
    private readonly float cellSize;
    private readonly Dictionary<Vector2Int, List<(T item, Vector2 pos)>> cells = new();

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
        var cell = WorldToCell(position);
        if (!cells.TryGetValue(cell, out var list))
        {
            list = new List<(T, Vector2)>();
            cells[cell] = list;
        }
        list.Add((item, position));
    }

    public List<T> Query(Vector2 center, float radius)
    {
        var result = new List<T>();
        int range = Mathf.CeilToInt(radius / cellSize);
        float sqRadius = radius * radius;
        var centerCell = WorldToCell(center);

        for (int x = centerCell.x - range; x <= centerCell.x + range; x++)
        {
            for (int y = centerCell.y - range; y <= centerCell.y + range; y++)
            {
                if (!cells.TryGetValue(new Vector2Int(x, y), out var items)) continue;
                foreach (var (item, pos) in items)
                {
                    if ((pos - center).sqrMagnitude <= sqRadius)
                        result.Add(item);
                }
            }
        }

        return result;
    }

    private Vector2Int WorldToCell(Vector2 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize)
        );
    }
}
