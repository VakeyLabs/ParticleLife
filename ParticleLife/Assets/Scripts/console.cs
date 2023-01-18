using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

static public class console
{
    static public void log(params object[] objects)
    {
        string message = "";

        foreach (var item in objects)
        {
            message += ((item == null) 
                ? "[null]" 
                : item.ToString());
                // : (item is Entity) ? $":{item.ToString()}:{Build.EntityManager.GetName((Entity)item)}" : item.ToString());

            message += ", ";  
        }

        message = message.Remove(message.Length - 2, 2);
        
        Debug.Log(message);   
    }

    static public string arrayToString<T>(IEnumerable<T> array) where T : struct
    {
        string message = "[";

        foreach (var item in array)
        {
            message += item.ToString() + ", ";  
        }

        return message.Remove(message.Length - 2, 2) + "]";
    }

    static public string arrayToString<T>(NativeArray<T> array) where T : struct
    {
        string message = "[";

        for (var i = 0; i < array.Length; i++)
        {
            var item = array[i];

            message += item.ToString() + ", ";  
        }

        return message.Remove(message.Length - 2, 2) + "]";
    }

    static public void logEntityComponents(Entity entity)
    {
        logEntityComponents(World.DefaultGameObjectInjectionWorld.EntityManager, entity);
    }

    static public void logEntityComponents(EntityManager entityManager, Entity entity)
    {
        var message = "";

        var componentTypes = entityManager.GetComponentTypes(entity);

        foreach (var item in componentTypes) {
            message += ((item == null) ? "[null]" : item.ToString()) + ", ";  
        }

        componentTypes.Dispose();

        message = message.Remove(message.Length - 2, 2);

        console.log(message);
    }

    static public void logObjectHiearchy(Transform transform)
    {
        logObjectHiearchy("", transform);
    }

    static public void logObjectHiearchy(string message, Transform transform)
    {
        if (message == null) message = "";
        else if (message.Length > 0) message += ", ";

        Action<Transform> setParentName = null;
        setParentName = (parent) => {
            if (parent == null) return;
            setParentName(parent.parent);
            message += " > " + parent.name;
        };

        setParentName(transform);
        
        Debug.Log(message);   
    }

    static public long RecordPeformance(string message, Action action)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (action != null) action();

        sw.Stop();
        console.log(message, sw.ElapsedMilliseconds);
        
        return sw.ElapsedMilliseconds;
    }
}
