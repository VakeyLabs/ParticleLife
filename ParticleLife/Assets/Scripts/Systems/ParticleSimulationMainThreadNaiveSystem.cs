using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ParticleSimulationMainThreadNaiveSystem: SystemBase
{
    protected override void OnUpdate()
    {
        var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particleRuleBuffer = SystemAPI.GetBuffer<ParticleRuleElement>(SystemAPI.GetSingletonEntity<ParticleSpawner>());
        var particlesQuery = SystemAPI.QueryBuilder().WithAll<ParticleTag, Velocity, WorldTransform>().Build();

        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var velocities = particlesQuery.ToComponentDataArray<Velocity>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

        for (var i = 0; i < transforms.Length; i++)
        {
            var aColor = (int)particleTags[i].color;
            var aPos = transforms[i].Position;
            var aVel = velocities[i].value;
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

            aVel = math.lerp(aVel, force * 0.4f, 0.5f);
            commandBuffer.SetComponent<Velocity>(entity, new Velocity { value = aVel });
        }

        particleTags.Dispose();
        velocities.Dispose();
        entities.Dispose();
        transforms.Dispose();
    }
}