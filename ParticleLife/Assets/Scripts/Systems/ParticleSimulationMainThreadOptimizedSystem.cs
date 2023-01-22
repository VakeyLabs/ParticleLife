using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ParticleSimulationMainThreadOptimizedSystem: SystemBase
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

        var force = new NativeArray<float3>(transforms.Length, Allocator.TempJob);

        for (var i = 0; i < transforms.Length; i++)
        {
            var aColor = (int)particleTags[i].color;
            var aPos = transforms[i].Position;
            var aVel = velocities[i].value;
            var entity = entities[i];

            for (var k = i+1; k < transforms.Length; k++)
            {
                var bColor = (int)particleTags[k].color;
                var bPos = transforms[k].Position;
                var delta = spawner.GetDelta(aPos, bPos);
                var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);
                
                if (distance < spawner.particleProperties.minRadius)
                {
                    var attraction = spawner.particleProperties.innerDetract;
                    var forceStrength = spawner.GetForce(attraction, distance, delta);
                    force[i] += forceStrength;
                    force[k] -= forceStrength;
                }
                else if (distance < spawner.particleProperties.maxRadius)
                {
                    var attractionA = particleRuleBuffer[aColor * 2 + bColor].attraction;
                    force[i] += spawner.GetForce(attractionA, distance, delta);
                    
                    var attractionB = particleRuleBuffer[bColor * 2 + aColor].attraction;
                    force[k] -= spawner.GetForce(attractionB, distance, delta);
                }
            }

            aVel = math.lerp(aVel, force[i] * 0.4f, 0.5f);
            commandBuffer.SetComponent<Velocity>(entity, new Velocity { value = aVel });
        }

        particleTags.Dispose();
        velocities.Dispose();
        entities.Dispose();
        transforms.Dispose();
        force.Dispose();
    }
}