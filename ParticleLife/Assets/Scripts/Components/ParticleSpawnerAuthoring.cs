using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;

[Serializable]
public struct SimulationBounds
{
    public float height, width;
    [HideInInspector]
    public float heightRadius, widthRadius;
}

[Serializable]
public struct ParticleProperties
{
    public float innerDetract;
    public int count;
    public float minRadius, maxRadius;
}

[Serializable]
public struct ParticleMatrix
{
    public float redToRed, redToGreen;
    public float greenToRed, greenToGreen;

    // Todo: convert to dynamic buffer
    public NativeArray<NativeArray<float>> CreateMatrix()
    {
        var ruleMatrix = new NativeArray<NativeArray<float>>(2, Allocator.TempJob);
        var red = new NativeArray<float>(2, Allocator.TempJob);
        red[0] = redToRed;
        red[1] = redToGreen;
        ruleMatrix[0] = red;
        var green = new NativeArray<float>(2, Allocator.TempJob);
        green[0] = greenToRed;
        green[1] = greenToGreen;
        ruleMatrix[1] = green;

        return ruleMatrix;
    }
}

public struct ParticleSpawner: IComponentData
{ 
    // Todo: convert to dynamic buffer
    public Entity redParticlePrefab, greenParticlePrefab;
    public SimulationBounds simulationBounds;
    public ParticleProperties particleProperties;
    public ParticleMatrix particleMatrix;
}

public class ParticleSpawnerAuthoring: MonoBehaviour
{ 
    public GameObject redParticlePrefab, greenParticlePrefab;
    public SimulationBounds simulationBounds;
    public ParticleProperties particleProperties;
    public ParticleMatrix particleMatrix;
}

public class ParticleSpawnerBaker: Baker<ParticleSpawnerAuthoring>
{
    public override void Bake(ParticleSpawnerAuthoring authoring)
    {
        var bounds = authoring.simulationBounds;
        bounds.heightRadius = bounds.height / 2;
        bounds.widthRadius = bounds.width / 2;

        AddComponent(new ParticleSpawner { 
            redParticlePrefab = GetEntity(authoring.redParticlePrefab),
            greenParticlePrefab = GetEntity(authoring.greenParticlePrefab),
            simulationBounds = bounds,
            particleProperties = authoring.particleProperties,
            particleMatrix = authoring.particleMatrix,
        });
    }
}