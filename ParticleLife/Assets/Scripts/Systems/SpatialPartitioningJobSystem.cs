using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct ParticleGridCell
{
    public Entity entity;
    public ParticleColor color;
    public float3 position;
}

[BurstCompile]
public partial struct SpatialPartitioningJobSystem: ISystem
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
        var velocities = particlesQuery.ToComponentDataArray<Velocity>(Allocator.TempJob);
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
        }.ScheduleParallel(state.Dependency).Complete();

        gridHashMap.Dispose();
        particleTags.Dispose();
        velocities.Dispose();
        entities.Dispose();
        transforms.Dispose();
    }
}

[BurstCompile]
public partial struct GridAllocationJob: IJobEntity
{
    [ReadOnly]
    public Grid grid;
    public NativeMultiHashMap<int, ParticleGridCell>.ParallelWriter gridHashMap;
    
    [BurstCompile]  
    public void Execute(Entity entity, RefRO<WorldTransform> transform, RefRO<ParticleTag> particle)
    {
        var position = transform.ValueRO.Position;
        gridHashMap.Add(grid.GetHashMapKey(position), new ParticleGridCell {
            entity = entity,
            position = position,
            color = particle.ValueRO.color,
        });
    }
}

[BurstCompile]
public partial struct SpatialPartitioningJob: IJobEntity
{
    public int horizontalCount, verticalCount;
    [ReadOnly]
    public Grid grid;
    [ReadOnly]
    public ParticleSpawner spawner;
    [ReadOnly]
    public NativeArray<ParticleTag> particleTags;
    [ReadOnly]
    public NativeArray<Entity> entities;
    [ReadOnly]
    public NativeArray<WorldTransform> transforms;
    [ReadOnly]
    public DynamicBuffer<ParticleRuleElement> particleRuleBuffer;
    [ReadOnly]
    public NativeMultiHashMap<int, ParticleGridCell> gridHashMap;
    public EntityCommandBuffer.ParallelWriter commandBuffer;
    
    [BurstCompile]
    public void Execute([EntityIndexInQuery] int i, Velocity velocity)
    {
        ParticleGridCell cell = new ParticleGridCell();
        NativeMultiHashMapIterator<int> iterator;
        var keys = new NativeArray<int>(9, Allocator.Temp);

        var aColor = (int)particleTags[i].color;
        var aPos = transforms[i].Position;
        var entity = entities[i];
        var force = float3.zero;
        var key = grid.GetHashMapKey(aPos);
        grid.GetSurroundingCells(ref keys, key, horizontalCount, verticalCount);

        for (var k = 0; k < keys.Length; k++)
        {
            if (gridHashMap.TryGetFirstValue(keys[k], out cell, out iterator))
            {
                do {
                    var bColor = (int)cell.color;
                    var bPos = cell.position;
                    var bEntity = cell.entity;
                    
                    if (entity != bEntity)
                    {
                        var delta = spawner.GetDelta(aPos, bPos);
                        var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);
                        
                        if (distance < spawner.particleProperties.minRadius)
                        {
                            var attraction = spawner.particleProperties.innerDetract;
                            force += spawner.GetForce(attraction, distance, delta);
                        }
                        else if (distance < spawner.particleProperties.maxRadius)
                        {
                            var attraction = particleRuleBuffer[aColor * 2 + bColor].attraction;
                            force += spawner.GetForce(attraction, distance, delta);
                        }
                    }
                } while(gridHashMap.TryGetNextValue(out cell, ref iterator));
            }
        }

        commandBuffer.SetComponent<Velocity>(i, entity, new Velocity { value = math.lerp(velocity.value, force * 0.4f, 0.5f) });
        keys.Dispose();
    }
}