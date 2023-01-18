
using UnityEngine;
using Unity.Entities;

public struct ParticleTag: IComponentData { public ParticleColor color; }
public class ParticleTagAuthoring: MonoBehaviour { public ParticleColor color; }
public class ParticleTagBaker: Baker<ParticleTagAuthoring>
{
    public override void Bake(ParticleTagAuthoring authoring)
    {
        AddComponent(new ParticleTag { color = authoring.color });
    }
}