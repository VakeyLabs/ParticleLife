using System;
using UnityEngine;
using Unity.Entities;

public enum ParticleSystemType
{
    MainThreadNaive,
    MainThreadOptimized,
}

public class ParticleSystemManagement: MonoBehaviour
{ 
    public ParticleSystemType currentSystem;

    private Action<bool> currentSystemHandle;

    private Action<bool> GetSystem(ParticleSystemType systemType, World world)
    {
        if (systemType == ParticleSystemType.MainThreadNaive)
        {
            return (bool enabled) => world.GetExistingSystemManaged<ParticleSimulationMainThreadNaiveSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.MainThreadOptimized)
        {
            return (bool enabled) => {
                var blah = world.GetExistingSystemManaged<ParticleSimulationMainThreadOptimizedSystem>();
                blah.Enabled = enabled;
            };
        }
        // else if (systemType == ParticleSystemType.UnmanagedSystem)
        // {
        //     return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<MyUnmanagedSys>().Enabled = enabled;
        // }

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