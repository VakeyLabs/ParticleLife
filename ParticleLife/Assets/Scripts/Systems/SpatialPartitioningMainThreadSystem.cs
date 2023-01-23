using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpatialPartitioningMainThreadSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var grid = SystemAPI.GetSingleton<Grid>();
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particleRuleBuffer = SystemAPI.GetSingletonBuffer<ParticleRuleElement>(true);
        var particlesQuery = SystemAPI.QueryBuilder().WithAll<ParticleTag, Velocity, WorldTransform>().Build();

        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

        var gridHashMap = new NativeMultiHashMap<int, ParticleGridCell>(particlesQuery.CalculateEntityCount(), Allocator.TempJob);

        new GridAllocationJob { 
            grid = grid,
            gridHashMap = gridHashMap.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();
        
        var horizontalCount = (int) spawner.simulationBounds.widthRadius / grid.cellSize;
        var verticalCount = (int) spawner.simulationBounds.heightRadius / grid.cellSize;

        new SpatialPartitioningJob{
            horizontalCount = horizontalCount,
            verticalCount = verticalCount,
            grid = grid,
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleTags = particleTags,
            entities = entities,
            transforms = transforms,
            gridHashMap = gridHashMap,
            commandBuffer = commandBuffer.AsParallelWriter(),
        }.Run();

        gridHashMap.Dispose();
        particleTags.Dispose();
        entities.Dispose();
        transforms.Dispose();
    }
}