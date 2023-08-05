using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[BurstCompile]
public partial struct MainThreadSpatialPartitioningParticleSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<Grid>();
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particleRuleBuffer = SystemAPI.GetSingletonBuffer<ParticleRuleElement>(true);
        var particlesQuery = SystemAPI.QueryBuilder().WithAll<ParticleTag, Velocity, WorldTransform>().Build();

        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);

        var gridHashMap = new NativeMultiHashMap<int, ParticleGridCell>(particlesQuery.CalculateEntityCount(), Allocator.TempJob);

        new GridAllocationJob { 
            grid = grid,
            gridHashMap = gridHashMap.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();

        new SpatialPartitioningParticleJob{
            grid = grid,
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleTags = particleTags,
            entities = entities,
            transforms = transforms,
            gridHashMap = gridHashMap,
        }.Run();

        gridHashMap.Dispose();
        particleTags.Dispose();
        transforms.Dispose();
    }
}