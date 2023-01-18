using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ParticleSimulationSystem: SystemBase
{
    protected override void OnUpdate()
    {
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        var particlesQuery = EntityManager.CreateEntityQuery(typeof(ParticleTag), typeof(Velocity), typeof(WorldTransform));
        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var particleVelocities = particlesQuery.ToComponentDataArray<Velocity>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var particleTransforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

        var ruleMatrix = spawner.particleMatrix.CreateMatrix();
        var forceX = new NativeArray<float>(particleTransforms.Length, Allocator.TempJob);
        var forceY = new NativeArray<float>(particleTransforms.Length, Allocator.TempJob);

        for (var i = 0; i < particleTransforms.Length; i++)
        {
            var aPos = particleTransforms[i].Position;
            var aVel = particleVelocities[i].value;
            var aColor = (int)particleTags[i].color;
            var entity = entities[i];

            for (var k = i+1; k < particleTransforms.Length; k++)
            {
                var bColor = (int)particleTags[k].color;
                var bPos = particleTransforms[k].Position;
                var delta = aPos - bPos;
                var edgeDeltaX = delta.x > 0 ? delta.x - spawner.simulationBounds.width : delta.x + spawner.simulationBounds.width;
                var edgeDeltaY = delta.y > 0 ? delta.y - spawner.simulationBounds.height : delta.y + spawner.simulationBounds.height;
                delta.x = math.abs(edgeDeltaX) < math.abs(delta.x) ? edgeDeltaX : delta.x;
                delta.y = math.abs(edgeDeltaY) < math.abs(delta.y) ? edgeDeltaY : delta.y;
                var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);

                if (distance < spawner.particleProperties.minRadius)
                {
                    var force = spawner.particleProperties.innerDetract * 1/distance;
                    var fx = (force * delta.x);
                    var fy = (force * delta.y);
                    forceX[i] += fx;
                    forceY[i] += fy;
                    forceX[k] -= fx;
                    forceY[k] -= fy;
                }
                else if (distance < spawner.particleProperties.maxRadius)
                {
                    var attraction = ruleMatrix[aColor][bColor];
                    var force = attraction * 1/distance;
                    forceX[i] += (force * delta.x);
                    forceY[i] += (force * delta.y);
                    
                    var attraction2 = ruleMatrix[bColor][aColor];
                    force = attraction2 * 1/distance;
                    forceX[k] -= (force * delta.x);
                    forceY[k] -= (force * delta.y);
                }
            }

            var valX = (forceX[i]) * 0.4f;
            var valY = (forceY[i]) * 0.4f;

            aVel = math.lerp(aVel, new float3(valX, valY, 0), 0.5f);
            EntityManager.SetComponentData<Velocity>(entity, new Velocity { value = aVel });
        }

        particlesQuery.Dispose();
        particleTags.Dispose();
        particleVelocities.Dispose();
        entities.Dispose();
        particleTransforms.Dispose();
        ruleMatrix.Dispose();
        forceX.Dispose();
        forceY.Dispose();
    }
}