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

    public NativeArray<int> GetSurroundingCellsOld(int key, float3 pos, float maxRadius)
    {
        var cellRadiusFloat = maxRadius / cellSize * 2;
        var cellRadiusInt = (int)math.ceil(cellRadiusFloat);
        var cellWidth = cellRadiusInt;
        var cellHeight = cellRadiusInt;
        var direction = cellRadiusInt % 2 * 2 - 1; // even = -1, odd = 1
        cellRadiusInt =  (cellRadiusInt- 1) / 2;
        var percentage = direction > 0 ? 1 - (cellRadiusFloat % 1) : (cellRadiusFloat % 1);

        var maxDistance = cellSize / 2;
        var cellCenterPos = new float3(math.floor(pos.x / cellSize), math.floor(pos.y / cellSize), 0) * cellSize + maxDistance;
        var posDistance = pos - cellCenterPos;
        var percentageDistance = posDistance / maxDistance;

        var yStart = unitYMultiplier * -cellRadiusInt;
        var xStart = -cellRadiusInt;
        
        if (direction > 0) {
            if (percentageDistance.x < 0 && -percentageDistance.x > percentage) {
                cellWidth++;
                xStart--;
            } else if (percentageDistance.x > percentage) {
                cellWidth++;
            }

            if (percentageDistance.y < 0 && -percentageDistance.y > percentage) {
                cellHeight++;
                yStart -= unitYMultiplier;
            } else if (percentageDistance.y > percentage) {
                cellHeight++;
            }
        } else {
            if (percentageDistance.x < 0) {
                if (-percentageDistance.x < percentage) {
                    cellWidth++;
                }
                xStart--;
            } else if (percentageDistance.x < percentage) {
                cellWidth++;
                xStart--;
            }
            
            if (percentageDistance.y < 0) {
                if (-percentageDistance.y < percentage) {
                    cellHeight++;
                }
                yStart -= unitYMultiplier;
            } else if (percentageDistance.y < percentage) {
                cellHeight++;
                yStart -= unitYMultiplier;
            }
        }
        
        var cellCount = cellHeight * cellWidth;

        var keys = new NativeArray<int>(cellCount, Allocator.Temp);
        
        var index = 0;

        for (var y = 0; y < cellHeight; y++)
        {
            for (var x = 0; x < cellWidth; x++)
            {
                if (index < keys.Length) keys[index++] = key + (xStart + x) + (yStart + y * unitYMultiplier);
                else index++;
            }
        }
        // console.log(
        //     "keys=" + keys.Length, 
        //     "index=" + index,
        //     "cellWidth=" + cellWidth,
        //     "cellHeight=" + cellHeight,
        //     "yStart=" + yStart,
        //     "xStart=" + xStart,
        //     "percentage=" + percentage,
        //     "percentageDistance=" + percentageDistance,
        //     "maxDistance=" + maxDistance
        // );
        // console.log(
        //     "GetCellPosition(key)=" + GetCellPosition(key),
        //     "cellCenterPos=" + cellCenterPos,
        //     "pos=" + pos,
        //     "posDistance=" + posDistance
        // );
        
        return keys;
    }

    public NativeArray<int> GetSurroundingCells(int key, float maxRadius)
    {
        var keysLength = GetSorroundingCellsCount(maxRadius);
        var keys = new NativeArray<int>(keysLength, Allocator.Temp);
        
        var index = 0;
        var count = ((int)math.sqrt(keysLength) - 1) / 2;

        var yStart = unitYMultiplier * -count;
        var xStart = -count;

        for (var y = yStart; y <= -yStart; y += unitYMultiplier)
        {
            for (var x = xStart; x <= -xStart; x++)
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