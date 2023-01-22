using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct ParticleSimulationJobNaiveSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particleRuleBuffer = SystemAPI.GetSingletonBuffer<ParticleRuleElement>(true);
        var particlesQuery = SystemAPI.QueryBuilder().WithAllRW<ParticleTag, WorldTransform>().Build();

        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);
        
        var jobHandle = new ParticleSimulationNaiveJob{ 
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleTags = particleTags,
            entities = entities,
            transforms = transforms,
            commandBuffer = commandBuffer.AsParallelWriter(),
        }.ScheduleParallel(state.Dependency);

        jobHandle.Complete();

        particleTags.Dispose();
        entities.Dispose();
        transforms.Dispose();
    }
}


[BurstCompile]
public partial struct ParticleSimulationNaiveJob: IJobEntity
{
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
    public EntityCommandBuffer.ParallelWriter commandBuffer;
    
    [BurstCompile]
    public void Execute([EntityIndexInQuery] int i, Velocity velocity)
    {
        var aColor = (int)particleTags[i].color;
        var aPos = transforms[i].Position;
        var entity = entities[i];
        var force = float3.zero;

        for (var k = 0; k < transforms.Length; k++)
        {
            var bColor = (int)particleTags[k].color;
            var bPos = transforms[k].Position;
            var bEntity = entities[k];
            
            if (entity != bEntity) {
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
        }

        commandBuffer.SetComponent<Velocity>(i, entity, new Velocity { value = math.lerp(velocity.value, force * 0.4f, 0.5f) });
    }
}