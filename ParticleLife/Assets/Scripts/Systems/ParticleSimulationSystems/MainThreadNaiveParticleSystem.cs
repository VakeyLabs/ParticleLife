using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

[BurstCompile]
public partial struct MainThreadNaiveParticleSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particleRuleBuffer = SystemAPI.GetBuffer<ParticleRuleElement>(SystemAPI.GetSingletonEntity<ParticleSpawner>());
        var particlesQuery = SystemAPI.QueryBuilder().WithAll<ParticleTag, Velocity, WorldTransform>().Build();

        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

        new NaiveParticleSimulationJob { 
            spawner = spawner,
            particleRuleBuffer = particleRuleBuffer,
            particleTags = particleTags,
            entities = entities,
            transforms = transforms,
            commandBuffer = commandBuffer.AsParallelWriter(),
        }.Run();

        particleTags.Dispose();
        entities.Dispose();
        transforms.Dispose();
    }
}