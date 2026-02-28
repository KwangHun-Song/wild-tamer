using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid<T> where T : class
{
    private readonly float cellSize;
    private readonly Dictionary<Vector2Int, List<T>> cells = new();

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
        Vector2Int cell = WorldToCell(position);
        if (!cells.ContainsKey(cell))
        {
            cells[cell] = new List<T>();
        }
        cells[cell].Add(item);
    }

    public List<T> Query(Vector2 center, float radius)
    {
        List<T> result = new List<T>();
        int range = Mathf.CeilToInt(radius / cellSize);
        Vector2Int centerCell = WorldToCell(center);

        for (int x = centerCell.x - range; x <= centerCell.x + range; x++)
        {
            for (int y = centerCell.y - range; y <= centerCell.y + range; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (cells.TryGetValue(cell, out List<T> items))
                {
                    result.AddRange(items);
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
