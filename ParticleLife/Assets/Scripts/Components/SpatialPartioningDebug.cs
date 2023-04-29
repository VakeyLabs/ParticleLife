using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial class SpatialPartioningDebug: MonoBehaviour
{
    public Camera mainCamera;

    private void DebugDrawCells(float3 pos, Grid grid)
    {
        Color color = Color.green;
        float3 lowerLeft = new float3(
            math.floor(pos.x / grid.cellSize) * grid.cellSize,
            math.floor(pos.y / grid.cellSize) * grid.cellSize,
            0
        );

        Debug.DrawLine(lowerLeft, lowerLeft + new float3(1, 0, 0) * grid.cellSize, color);
        Debug.DrawLine(lowerLeft, lowerLeft + new float3(0, 1, 0) * grid.cellSize, color);
        Debug.DrawLine(lowerLeft + new float3(1, 0, 0) * grid.cellSize, lowerLeft + new float3(1, 1, 0) * grid.cellSize, color);
        Debug.DrawLine(lowerLeft + new float3(0, 1, 0) * grid.cellSize, lowerLeft + new float3(1, 1, 0) * grid.cellSize, color);
    }

    public void DrawCircle(float3 center, float radius, int vertexCount, Color color)
    {
        var deltaTheta = (2f * Mathf.PI) / vertexCount;
        var theta = 0f;

        var oldPos = center + new float3(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta), 0);

        for (var i = 1; i < vertexCount + 1; i++)
        {
            theta += deltaTheta;
            var pos = new float3(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta), 0);
            Debug.DrawLine(oldPos, (center + pos), color);
            oldPos = center + pos;
        }
    }

    private void DebugDrawCells(int key, Grid grid)
    {
        DebugDrawCells(grid.GetCellPosition(key) * grid.cellSize, grid);
    }

    void Update()
    {
        Grid grid;
        World.DefaultGameObjectInjectionWorld.EntityManager
            .CreateEntityQuery(new ComponentType[] { typeof(Grid) })
            .TryGetSingleton<Grid>(out grid);
        ParticleSpawner spawner;
        World.DefaultGameObjectInjectionWorld.EntityManager
            .CreateEntityQuery(new ComponentType[] { typeof(ParticleSpawner) })
            .TryGetSingleton<ParticleSpawner>(out spawner);

        var mousePos = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));
        var key = grid.GetHashMapKey(mousePos);
        // DebugDrawCells(mousePos, grid);
        // DebugDrawCells(key, grid);

        // var horizontalCount = (int) math.ceil(spawner.simulationBounds.widthRadius / grid.cellSize);
        // var verticalCount = (int) math.ceil(spawner.simulationBounds.heightRadius / grid.cellSize);
        var keys = grid.GetSurroundingCells(key, mousePos, spawner.particleProperties.maxRadius);

        for (var i = 0; i < keys.Length; i++) {
            DebugDrawCells(keys[i], grid);
        }

        DrawCircle(mousePos, spawner.particleProperties.maxRadius, 100, Color.red);

        // console.log("key", key);
        // console.log(key, "|", console.arrayToString(keys));

        keys.Dispose();
    }
}