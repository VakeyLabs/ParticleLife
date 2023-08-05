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
        var fromPos = spawner.spawnProperties.fromPos;
        var toPos = spawner.spawnProperties.toPos;
        var fromRot = spawner.spawnProperties.fromRot;
        var toRot = spawner.spawnProperties.toRot;
        
        var count = 0;
        while (entityCount < spawner.particleProperties.count && count < 100) {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var particleEntityBuffer = SystemAPI.GetBuffer<ParticleEntityElement>(SystemAPI.GetSingletonEntity<ParticleSpawner>());
            var particleEntity = commandBuffer.Instantiate(particleEntityBuffer[entityCount % 2].prefab);
            var position = new float3(UnityEngine.Random.Range(fromPos.x, toPos.x), UnityEngine.Random.Range(fromPos.y, toPos.y), 0);
            var scale = UnityEngine.Random.Range(fromRot, toRot);

            commandBuffer.SetComponent<LocalTransform>(
                particleEntity, 
                new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = scale }
            );
            entityCount++;
            count++;

        }
    }
}