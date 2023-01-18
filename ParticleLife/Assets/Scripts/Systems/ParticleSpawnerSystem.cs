using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ParticleSpawnerSystem: SystemBase
{
    private Entity GetParticlePrefab(ParticleSpawner spawner, int index)
    {
        if (index == 0) return spawner.redParticlePrefab;
        else return spawner.greenParticlePrefab;
    }

    protected override void OnUpdate()
    {
        var particleQuery = EntityManager.CreateEntityQuery(typeof(ParticleTag));
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
        var entityCount = particleQuery.CalculateEntityCount();
        
        if (entityCount < spawner.particleProperties.count) {
            var particleEntity = commandBuffer.Instantiate(GetParticlePrefab(spawner, entityCount % 2));
            var position = new float3(UnityEngine.Random.Range(-100, 100), UnityEngine.Random.Range(-40, 40), 0);
            commandBuffer.SetComponent<LocalTransform>(particleEntity, new LocalTransform { Position = position, Rotation = quaternion.identity, Scale = UnityEngine.Random.Range(1, 3)});
        }
    }
}