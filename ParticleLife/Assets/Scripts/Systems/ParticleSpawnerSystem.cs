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
        var spawnRadius = spawner.spawnProperties.spawnRadius;
        var fromRot = spawner.spawnProperties.sizeFrom;
        var toRot = spawner.spawnProperties.sizeTo;
        
        var count = 0;
        while (entityCount < spawner.spawnProperties.total && count < 100) {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var particleEntityBuffer = SystemAPI.GetBuffer<ParticleEntityElement>(SystemAPI.GetSingletonEntity<ParticleSpawner>());
            var particleEntity = commandBuffer.Instantiate(particleEntityBuffer[entityCount % 2].prefab);
            var position =  (UnityEngine.Vector3)UnityEngine.Random.insideUnitCircle * spawnRadius;
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