using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
// [BurstCompile]
public partial struct BoidJobSystem: ISystem
{
    EntityQuery particlesQuery;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        using var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
        
        queryBuilder.WithAll<ParticleTag, Velocity, WorldTransform>();
        particlesQuery = state.GetEntityQuery(queryBuilder);
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        console.log("----------------------BoidJobSystem");
        var world = state.WorldUnmanaged;
        var grid = SystemAPI.GetSingleton<Grid>();
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particleRuleBuffer = SystemAPI.GetSingletonBuffer<ParticleRuleElement>(true);
        // var particlesQuery = SystemAPI.QueryBuilder().WithAll<ParticleTag, Velocity, WorldTransform>().Build();
        var particleCount = particlesQuery.CalculateEntityCount();

        // var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        // var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        // var transforms = particlesQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

        var gridHashMap = new NativeMultiHashMap<int, int>(particleCount, world.UpdateAllocator.ToAllocator);

        var forces = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(particleCount, ref world.UpdateAllocator);
        var particlePos = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(particleCount, ref world.UpdateAllocator);
        var particleColors = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(particleCount, ref world.UpdateAllocator);

        var copyParticlePositionJobHandle = new CopyParticlePositionJob
        {
            chunkBaseEntityIndices = particlesQuery.CalculateBaseEntityIndexArrayAsync(
                world.UpdateAllocator.ToAllocator, state.Dependency,
                out var copyParticlePositionJobChunkHandle
            ),
            positions = particlePos,
        }.ScheduleParallel(particlesQuery, copyParticlePositionJobChunkHandle);
        
        var copyParticleColorJobHandle = new CopyParticleColorJob
        {
            chunkBaseEntityIndices = particlesQuery.CalculateBaseEntityIndexArrayAsync(
                world.UpdateAllocator.ToAllocator, state.Dependency,
                out var copyParticleColorJobChunkHandle
            ),
            colors = particleColors,
        }.ScheduleParallel(particlesQuery, copyParticleColorJobChunkHandle);

        var copyJobsHandle = JobHandle.CombineDependencies(copyParticlePositionJobHandle, copyParticleColorJobHandle);

        var gridJobHandle = new GridAllocationJob2 { 
            maxRadius = spawner.particleProperties.maxRadius,
            grid = grid,
            particleColors = particleColors,
            particlePos = particlePos,
            gridHashMap = gridHashMap.AsParallelWriter(),
        }.ScheduleParallel(particlesQuery, copyJobsHandle);
        
        var boidJobHndle = new BoidJob2 {
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleColors = particleColors,
            particlePos = particlePos,
            forces = forces,
        }.Schedule(gridHashMap, 64, gridJobHandle);
        // var boidJobHndle = new BoidJob {
        //     grid = grid,
        //     spawner = spawner,
        //     particleRuleBuffer = particleRuleBuffer,
        //     particleColors = particleColors,
        //     // entities = entities,
        //     particlePos = particlePos,
        //     gridHashMap = gridHashMap,
        //     forces = forces,
        // }.ScheduleParallel(gridJobHandle);
        
        var applyVelocityJob = new ApplyVelocityJob { 
            lerpTime = spawner.particleProperties.lerpTime,
            forces = forces,
        }.ScheduleParallel(boidJobHndle);

        // state.Dependency = boidJobHndle;
        state.Dependency = applyVelocityJob;

        // We pass the job handle and add the dependency so that we keep the proper ordering between the jobs
        // as the looping iterates. For our purposes of execution, this ordering isn't necessary; however, without
        // the add dependency call here, the safety system will throw an error, because we're accessing multiple
        // pieces of boid data and it would think there could possibly be a race condition.

        particlesQuery.AddDependency(state.Dependency);
        particlesQuery.ResetFilter();

        // particleTags.Dispose();
        // entities.Dispose();
        // particlePos.Dispose();
    }
}

[BurstCompile]
partial struct ApplyVelocityJob : IJobEntity
{
    [ReadOnly] public float lerpTime;
    [ReadOnly] public NativeArray<float3> forces;

    [BurstCompile]
    void Execute([EntityIndexInQuery] int i, ref Velocity velocity)
    {
        velocity = new Velocity { value = math.lerp(velocity.value, forces[i], lerpTime) };
    }
}

[BurstCompile]
partial struct CopyParticlePositionJob : IJobEntity
{
    [ReadOnly] public NativeArray<int> chunkBaseEntityIndices;
    [NativeDisableParallelForRestriction] public NativeArray<float3> positions;
    void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in WorldTransform localToWorld)
    {
        int entityIndexInQuery = chunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
        positions[entityIndexInQuery] = localToWorld.Position;
    }
}


[BurstCompile]
partial struct CopyParticleColorJob : IJobEntity
{
    [ReadOnly] public NativeArray<int> chunkBaseEntityIndices;
    [NativeDisableParallelForRestriction] public NativeArray<int> colors;
    void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in ParticleTag particle)
    {
        int entityIndexInQuery = chunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;
        colors[entityIndexInQuery] = (int)particle.color;
    }
}

public struct ParticleGridCell2
{
    public int index;
    public int color;
    public float3 position;
}
// [BurstCompile]
public partial struct GridAllocationJob2: IJobEntity
{
    [ReadOnly] public float maxRadius;
    [ReadOnly] public Grid grid;
    [ReadOnly] public NativeArray<int> particleColors;
    [ReadOnly] public NativeArray<float3> particlePos;
    public NativeMultiHashMap<int, int>.ParallelWriter gridHashMap;
    
    // [BurstCompile]  
    public void Execute([EntityIndexInQuery] int index)
    {
        var position = particlePos[index];
        var key = grid.GetHashMapKey(position);
        // var particleCell = new ParticleGridCell2 {
        //     index = index,
        //     position = position,
        //     color = particleColors[index],
        // };

        console.log("hash", key, index);

        gridHashMap.Add(key, index);
    }
}

[BurstCompile]
public partial struct BoidJob: IJobEntity
{
    [ReadOnly] public Grid grid;
    [ReadOnly] public ParticleSpawner spawner;
    [ReadOnly] public NativeArray<int> particleColors;
    // [ReadOnly] public NativeArray<Entity> entities;
    [ReadOnly] public NativeArray<float3> particlePos;
    [ReadOnly] public DynamicBuffer<ParticleRuleElement> particleRuleBuffer;
    [ReadOnly] public NativeMultiHashMap<int, int> gridHashMap;
    public NativeArray<float3> forces;
    
    [BurstCompile]
    public void Execute([EntityIndexInQuery] int i, ref Velocity velocity)
    {
        // ParticleGridCell2 cell = new ParticleGridCell2();
        int cellIndex;
        NativeMultiHashMapIterator<int> iterator;

        var aColor = particleColors[i];
        var aPos = particlePos[i];
        var force = float3.zero;

        var key = grid.GetHashMapKey(aPos);
     
        // if (gridHashMap.TryGetFirstValue(key, out cell, out iterator))
        if (gridHashMap.TryGetFirstValue(key, out cellIndex, out iterator))
        {
            do {
                // var bColor = cell.color;
                // var bPos = cell.position;
                
                // if (i != cell.index)
                var bColor = particleColors[cellIndex];
                var bPos = particlePos[cellIndex];
                
                if (i != cellIndex)
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
            // } while(gridHashMap.TryGetNextValue(out cell, ref iterator));
            } while(gridHashMap.TryGetNextValue(out cellIndex, ref iterator));
        }

        forces[i] = force;
    }
}


// [BurstCompile]
public partial struct BoidJob2: IJobNativeMultiHashMapMergedSharedKeyIndices
{
    [ReadOnly] public ParticleSpawner spawner;
    [ReadOnly] public NativeArray<int> particleColors;
    [ReadOnly] public NativeArray<float3> particlePos;
    [ReadOnly] public DynamicBuffer<ParticleRuleElement> particleRuleBuffer;
    public NativeArray<float3> forces;

    // [BurstCompile]
    public void ExecuteFirst(int index) {
        console.log("boid first", index);
    }

    // [BurstCompile]
    public void ExecuteNext(int cellIndex, int index)
    {
        console.log("boid", index, "(" + cellIndex + ")");
        var aColor = particleColors[cellIndex];
        var aPos = particlePos[cellIndex];

        var bColor = particleColors[index];
        var bPos = particlePos[index];
        
        var delta = spawner.GetDelta(aPos, bPos);
        var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);

        if (distance < spawner.particleProperties.minRadius)
        {
            var attraction = spawner.particleProperties.innerDetract;
            forces[cellIndex] += spawner.GetForce(attraction, distance, delta);
        }
        else if (distance < spawner.particleProperties.maxRadius)
        {
            var attraction = particleRuleBuffer[aColor * 2 + bColor].attraction;
            forces[cellIndex] += spawner.GetForce(attraction, distance, delta);
        }
    }
}