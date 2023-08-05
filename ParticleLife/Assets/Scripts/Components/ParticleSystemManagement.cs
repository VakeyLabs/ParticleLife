using System;
using UnityEngine;
using Unity.Entities;

public enum ParticleSystemType
{
    MainThreadNaive,
    MainThreadOptimized,
    MainThreadSpatialPartitioning,
    JobNaive,
    JobSpatialPartitioning,
}

public class ParticleSystemManagement: MonoBehaviour
{ 
    public ParticleSystemType currentSystem;

    private Action<bool> currentSystemHandle;

    private Action<bool> GetSystem(ParticleSystemType systemType, World world)
    {
        if (systemType == ParticleSystemType.MainThreadNaive)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<MainThreadNaiveParticleSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.MainThreadOptimized)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<MainThreadOptimizedParticleSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.JobNaive)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<JobNaiveParticleSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.MainThreadSpatialPartitioning)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<MainThreadSpatialPartitioningParticleSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.JobSpatialPartitioning)
        {
            // return (bool enabled) => world.GetExistingSystemManaged<JobSpatialPartitioningParticleSystem>().Enabled = enabled;
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<JobSpatialPartitioningParticleSystem>().Enabled = enabled;
        }

        return (bool enabled) => { };
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        var world = World.DefaultGameObjectInjectionWorld;

        if (currentSystemHandle == null)
        {
            foreach (ParticleSystemType systemType in Enum.GetValues(typeof(ParticleSystemType)))
            {
                var systemHandle = GetSystem(systemType, world);

                if (currentSystem == systemType)
                {
                    currentSystemHandle = systemHandle;
                }
                else
                {
                    systemHandle(false);
                }
            }
        }
        else
        {
            currentSystemHandle(false);
            currentSystemHandle = GetSystem(currentSystem, world);
            currentSystemHandle(true);
        }
    }
}