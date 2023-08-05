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

/** TODO
- Move Lerping from Velocity to Transform
- Get Radial Surrounding Cells for min and max
- Optimization Explorations
    - Process entities in chunks
    - Move grid every iteration
*/

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[BurstCompile]
public partial struct JobSpatialPartitioningParticleSystem: ISystem
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
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

        var hashMapLength = grid.GetSorroundingCellsCount(spawner.particleProperties.maxRadius) * particlesQuery.CalculateEntityCount();
        var gridHashMap = new NativeMultiHashMap<int, ParticleGridCell>(hashMapLength, Allocator.TempJob);

        new GridAllocationJob { 
            maxRadius = spawner.particleProperties.maxRadius,
            grid = grid,
            gridHashMap = gridHashMap.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();
        
        new SpatialPartitioningParticleJob {
            grid = grid,
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleTags = particleTags,
            entities = entities,
            transforms = transforms,
            gridHashMap = gridHashMap,
        }.ScheduleParallel(state.Dependency).Complete();

        gridHashMap.Dispose();
        particleTags.Dispose();
        entities.Dispose();
        transforms.Dispose();
    }
}

[BurstCompile]
public partial struct GridAllocationJob: IJobEntity
{
    [ReadOnly] public float maxRadius;
    [ReadOnly] public Grid grid;
    public NativeMultiHashMap<int, ParticleGridCell>.ParallelWriter gridHashMap;
    
    [BurstCompile]
    public void Execute(Entity entity, RefRO<WorldTransform> transform, RefRO<ParticleTag> particle)
    {
        var position = transform.ValueRO.Position;
        var key = grid.GetHashMapKey(position);
        var particleCell = new ParticleGridCell {
            entity = entity,
            position = position,
            color = particle.ValueRO.color,
        };

        gridHashMap.Add(key, particleCell);
        
        // var keys = grid.GetSurroundingCells(key, maxRadius);

        // for (var i = 0; i < keys.Length; i++)
        // {
        //     gridHashMap.Add(keys[i], particleCell);
        // }

        // keys.Dispose();
    }
}

[BurstCompile]
public partial struct SpatialPartitioningParticleJob: IJobEntity
{
    [ReadOnly] public Grid grid;
    [ReadOnly] public ParticleSpawner spawner;
    [ReadOnly] public NativeArray<ParticleTag> particleTags;
    [ReadOnly] public NativeArray<Entity> entities;
    [ReadOnly] public NativeArray<WorldTransform> transforms;
    [ReadOnly] public DynamicBuffer<ParticleRuleElement> particleRuleBuffer;
    [ReadOnly] public NativeMultiHashMap<int, ParticleGridCell> gridHashMap;
    
    [BurstCompile]
    public void Execute([EntityIndexInQuery] int i, ref Velocity velocity)
    {
        var aColor = (int)particleTags[i].color;
        var aPos = transforms[i].Position;
        var entity = entities[i];
        var force = float3.zero;

        var key = grid.GetHashMapKey(aPos);
        var keys = grid.GetSurroundingCells(key, aPos, spawner.particleProperties.maxRadius);

        for (var k = 0; k < keys.Length; k++)
        {
            if (gridHashMap.TryGetFirstValue(keys[k], out var cell, out var iterator))
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

        var lerpTime = spawner.particleProperties.lerpTime;
        velocity = new Velocity { value = math.lerp(velocity.value, force, lerpTime) };

        keys.Dispose();
    }
}