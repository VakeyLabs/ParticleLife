using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ParticleSpawnerSystem: SystemBase
{
    // Todo: Convert to Job
    protected override void OnUpdate()
    {
        var particleQuery = EntityManager.CreateEntityQuery(typeof(ParticleTag));
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var entityCount = particleQuery.CalculateEntityCount();
        
        if (entityCount < spawner.particleProperties.count) {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var particleEntityBuffer = SystemAPI.GetBuffer<ParticleEntityElement>(SystemAPI.GetSingletonEntity<ParticleSpawner>());
            var particleEntity = commandBuffer.Instantiate(particleEntityBuffer[entityCount % 2].prefab);
            var position = new float3(UnityEngine.Random.Range(-100, 100), UnityEngine.Random.Range(-40, 40), 0);

            commandBuffer.SetComponent<LocalTransform>(particleEntity, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = UnityEngine.Random.Range(1, 3)});
        }
    }
}