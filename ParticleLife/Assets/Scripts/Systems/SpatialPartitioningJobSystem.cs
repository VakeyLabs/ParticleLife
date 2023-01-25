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
    private NativeMultiHashMap<int, int> keyHashMap;

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

        var hashMapLength = grid.GetSorroundingCellsCount(spawner.particleProperties.maxRadius) * particlesQuery.CalculateEntityCount();
        var gridHashMap = new NativeMultiHashMap<int, ParticleGridCell>(hashMapLength, Allocator.TempJob);

        // if (!keyHashMap.IsCreated) {
        //     var count = 50;
        //     keyHashMap = new NativeMultiHashMap<int, int>(count, Allocator.TempJob);

        //     var yStart = grid.unitYMultiplier * count;
        //     var xStart = -count;

        //     for (var y = yStart; y >= -yStart; y -= grid.unitYMultiplier)
        //     {
        //         for (var x = xStart; x <= -xStart; x++)
        //         {
        //             var key = x + y;
        //             var cells = grid.GetSurroundingCells(key, spawner.particleProperties.maxRadius);
        //             for (var i = 0; i < cells.Length; i++) {
        //                 keyHashMap.Add(key, cells[i]);
        //             }
        //             cells.Dispose();
        //         }
        //     }
        // }

        new GridAllocationJob { 
            maxRadius = spawner.particleProperties.maxRadius,
            grid = grid,
            gridHashMap = gridHashMap.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();
        
        new SpatialPartitioningJob {
            grid = grid,
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleTags = particleTags,
            entities = entities,
            transforms = transforms,
            gridHashMap = gridHashMap,
            // keyHashMap = keyHashMap,
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
    public float maxRadius;
    [ReadOnly]
    public Grid grid;
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

// [BurstCompile]
// public partial struct SpatialPartitioningJob: IJobEntity
// {
//     [ReadOnly]
//     public Grid grid;
//     [ReadOnly]
//     public ParticleSpawner spawner;
//     [ReadOnly]
//     public NativeArray<ParticleTag> particleTags;
//     [ReadOnly]
//     public NativeArray<Entity> entities;
//     [ReadOnly]
//     public NativeArray<WorldTransform> transforms;
//     [ReadOnly]
//     public DynamicBuffer<ParticleRuleElement> particleRuleBuffer;
//     [ReadOnly]
//     public NativeMultiHashMap<int, ParticleGridCell> gridHashMap;
//     public EntityCommandBuffer.ParallelWriter commandBuffer;
    
//     [BurstCompile]
//     public void Execute([EntityIndexInQuery] int i, Velocity velocity)
//     {
//         ParticleGridCell cell = new ParticleGridCell();
//         NativeMultiHashMapIterator<int> iterator;

//         var aColor = (int)particleTags[i].color;
//         var aPos = transforms[i].Position;
//         var entity = entities[i];
//         var force = float3.zero;
//         var key = grid.GetHashMapKey(aPos);

//         if (gridHashMap.TryGetFirstValue(key, out cell, out iterator))
//         {
//             do {
//                 var bColor = (int)cell.color;
//                 var bPos = cell.position;
//                 var bEntity = cell.entity;
                
//                 if (entity != bEntity)
//                 {
//                     var delta = spawner.GetDelta(aPos, bPos);
//                     var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);
                    
//                     if (distance < spawner.particleProperties.minRadius)
//                     {
//                         var attraction = spawner.particleProperties.innerDetract;
//                         force += spawner.GetForce(attraction, distance, delta);
//                     }
//                     else if (distance < spawner.particleProperties.maxRadius)
//                     {
//                         var attraction = particleRuleBuffer[aColor * 2 + bColor].attraction;
//                         force += spawner.GetForce(attraction, distance, delta);
//                     }
//                 }
//             } while(gridHashMap.TryGetNextValue(out cell, ref iterator));
//         }

//         commandBuffer.SetComponent<Velocity>(i, entity, new Velocity { value = math.lerp(velocity.value, force * 0.4f, 0.5f) });
//     }
// }

[BurstCompile]
public partial struct SpatialPartitioningJob: IJobEntity
{
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

        var aColor = (int)particleTags[i].color;
        var aPos = transforms[i].Position;
        var entity = entities[i];
        var force = float3.zero;
        
        var key = grid.GetHashMapKey(aPos);
        var keys = grid.GetSurroundingCells(key, spawner.particleProperties.maxRadius);

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

        var lerpTime = spawner.particleProperties.lerpTime;
        commandBuffer.SetComponent<Velocity>(i, entity, new Velocity { value = math.lerp(velocity.value, force, lerpTime) });
        keys.Dispose();
    }
}

// [BurstCompile]
// public partial struct SpatialPartitioningJob: IJobEntity
// {
//     [ReadOnly]
//     public Grid grid;
//     [ReadOnly]
//     public ParticleSpawner spawner;
//     [ReadOnly]
//     public NativeArray<ParticleTag> particleTags;
//     [ReadOnly]
//     public NativeArray<Entity> entities;
//     [ReadOnly]
//     public NativeArray<WorldTransform> transforms;
//     [ReadOnly]
//     public DynamicBuffer<ParticleRuleElement> particleRuleBuffer;
//     [ReadOnly]
//     public NativeMultiHashMap<int, ParticleGridCell> gridHashMap;
//     [ReadOnly]
//     public NativeMultiHashMap<int, int> keyHashMap;
//     public EntityCommandBuffer.ParallelWriter commandBuffer;
    
//     [BurstCompile]
//     public void Execute([EntityIndexInQuery] int i, Velocity velocity)
//     {
//         ParticleGridCell cell = new ParticleGridCell();
//         NativeMultiHashMapIterator<int> gridIterator;
//         NativeMultiHashMapIterator<int> keyIterator;
//         var keys = new NativeArray<int>(grid.GetSorroundingCellsCount(spawner.particleProperties.maxRadius), Allocator.Temp);

//         var aColor = (int)particleTags[i].color;
//         var aPos = transforms[i].Position;
//         var entity = entities[i];
//         var force = float3.zero;
//         var mainKey = grid.GetHashMapKey(aPos);

//         int key = 0;
//         if (keyHashMap.TryGetFirstValue(mainKey, out key, out keyIterator))
//         {
//             do {
//                 if (gridHashMap.TryGetFirstValue(key, out cell, out gridIterator))
//                 {
//                     do {
//                         var bColor = (int)cell.color;
//                         var bPos = cell.position;
//                         var bEntity = cell.entity;
                        
//                         if (entity != bEntity)
//                         {
//                             var delta = spawner.GetDelta(aPos, bPos);
//                             var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);
                            
//                             if (distance < spawner.particleProperties.minRadius)
//                             {
//                                 var attraction = spawner.particleProperties.innerDetract;
//                                 force += spawner.GetForce(attraction, distance, delta);
//                             }
//                             else if (distance < spawner.particleProperties.maxRadius)
//                             {
//                                 var attraction = particleRuleBuffer[aColor * 2 + bColor].attraction;
//                                 force += spawner.GetForce(attraction, distance, delta);
//                             }
//                         }
//                     } while(gridHashMap.TryGetNextValue(out cell, ref gridIterator));
//                 }
//             } while(keyHashMap.TryGetNextValue(out key, ref keyIterator));
//         }

//         commandBuffer.SetComponent<Velocity>(i, entity, new Velocity { value = math.lerp(velocity.value, force * 0.4f, 0.5f) });
//         keys.Dispose();
//     }
// }