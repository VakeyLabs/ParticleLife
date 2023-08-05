using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public enum IterationType {
    Iteration1,
    Iteration2,
    Iteration3,
}

public struct Grid: IComponentData
{
    public IterationType iteration;
    public float testRadius;
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
        // return(int) math.hash(new int3(math.floor(pos / cellSize)));
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

    public int GetSorroundingCellsCount(float maxRadius)
    {
        var cellsLength = (int)math.ceil(maxRadius / cellSize) * 2 + 1;
        return cellsLength * cellsLength;
    }

    
    public NativeArray<int> GetSurroundingCells(int key, float3 pos, float maxRadius)
    {
        var cellCenterPos = new float3(math.floor(pos.x / cellSize), math.floor(pos.y / cellSize), 0) * cellSize;
        var relativePos = pos - cellCenterPos;
        var xStart = (int)math.floor((relativePos.x - maxRadius) / cellSize);
        var xEnd = (int)math.floor((relativePos.x + maxRadius) / cellSize) + 1;
        var yStart = (int)math.floor((relativePos.y - maxRadius) / cellSize);
        var yEnd = (int)math.floor((relativePos.y + maxRadius) / cellSize) + 1;
        var cellHeight = yEnd - yStart;
        var cellWidth = xEnd - xStart;
        var cellCount = cellHeight * cellWidth;

        yStart*=unitYMultiplier;
        yEnd*=unitYMultiplier;

        var keys = new NativeArray<int>(cellCount, Allocator.Temp);
        var index = 0;

        for (var y = yStart; y < yEnd; y+=unitYMultiplier)
        {
            for (var x = xStart; x < xEnd; x++)
            {
                keys[index++] = key + x + y;
            }
        }
        
        return keys;
    }
}

public class GridAuthoring: MonoBehaviour
{ 
    public IterationType iteration;
    public float testRadius = 150;
    public int cellSize = 25;
    public int unitYMultiplier = 1000;
}

public class GridBaker: Baker<GridAuthoring>
{
    public override void Bake(GridAuthoring authoring)
    {
        AddComponent(new Grid {
            iteration = authoring.iteration,
            testRadius = authoring.testRadius,
            cellSize = authoring.cellSize,
            unitYMultiplier = authoring.unitYMultiplier,
        });
    }
}