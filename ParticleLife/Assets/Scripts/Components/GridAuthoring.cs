using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Grid: IComponentData
{
    public int cellSize, unitYMultiplier;

    public float3 GetCellPosition(int key)
    {
        var halfMultiplier = unitYMultiplier/2;
        var x = key % unitYMultiplier;

        if (x > halfMultiplier && x < unitYMultiplier) {
            x -= unitYMultiplier;
        } else if (x < -halfMultiplier && x > - unitYMultiplier) {
            x += unitYMultiplier;
        }

        return new float3(x, math.floor((key+halfMultiplier) / (float)unitYMultiplier), 0);
    }

    public int GetHashMapKey(float3 pos)
    {
        return (int) (math.floor(pos.x / cellSize) + unitYMultiplier * math.floor(pos.y / cellSize));
    }

    public int GetEntityCount<T>(NativeMultiHashMap<int, T> hashMap, int key) where T: unmanaged
    {
        T cell;
        NativeMultiHashMapIterator<int> iterator;
        int count = 0;

        if (hashMap.TryGetFirstValue(key, out cell, out iterator)) {
            do { count++; } while(hashMap.TryGetNextValue(out cell, ref iterator));
        }

        return count;
    }

    public NativeArray<int> GetSurroundingCells(int key, float maxRadius)
    {
        var keys = new NativeArray<int>(GetSorroundingCellsCount(maxRadius), Allocator.Temp);
        GetSurroundingCells(ref keys, key);
        return keys;
    }

    public int GetSorroundingCellsCount(float maxRadius)
    {
        var cellsLength = (int)math.ceil(maxRadius / cellSize) * 2 + 1;
        return cellsLength * cellsLength;
    }

    public void GetSurroundingCells(ref NativeArray<int> keys, int key)
    {
        var cellPos = GetCellPosition(key);
        // var leftX = -1;
        // var rightX = 1;
        // var topY = unitYMultiplier;
        // var bottomY = -unitYMultiplier;

        // if (cellPos.x == horizontalCount - 1) rightX += -horizontalCount * 2;
        // else if (cellPos.x == -horizontalCount) leftX += horizontalCount * 2;

        // if (cellPos.y == verticalCount - 1) topY += -verticalCount * 2 * unitYMultiplier;
        // else if (cellPos.y == -verticalCount) bottomY += verticalCount * 2 * unitYMultiplier;
        
        var index = 0;
        var count = ((int)math.sqrt(keys.Length) - 1) / 2;

        var yStart = unitYMultiplier * count;
        var xStart = -count;

        for (var y = yStart; y >= -yStart; y -= unitYMultiplier)
        {
            for (var x = xStart; x <= -xStart; x++)
            {
                keys[index++] = key + x + y;
            }
        }
    }
}

public class GridAuthoring: MonoBehaviour
{ 
    public int cellSize = 25;
    public int unitYMultiplier = 1000;
}

public class GridBaker: Baker<GridAuthoring>
{
    public override void Bake(GridAuthoring authoring)
    {
        AddComponent(new Grid {
            cellSize = authoring.cellSize,
            unitYMultiplier = authoring.unitYMultiplier,
        });
    }
}