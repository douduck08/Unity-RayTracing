using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRayTracingObject<TData> {
    TData GetData ();
}

public abstract class RayTracingObjectBase<TComponet, TData> : MonoBehaviour where TComponet : IRayTracingObject<TData> {

    static bool isDirty = true;
    static List<TComponet> componentList = new List<TComponet> ();

    public static void AddObject (TComponet obj) {
        isDirty = true;
        componentList.Add (obj);
    }

    public static void RemoveObject (TComponet obj) {
        isDirty = true;
        componentList.Remove (obj);
    }

    public static void UpdateObject (TComponet obj) {
        isDirty = true;
    }

    public static ComputeBuffer CreateComputeBuffer (int count, int dataSize) {
        return new ComputeBuffer (count, dataSize);
    }

    public static bool UpdateComputeBufferIfNeeded (ref ComputeBuffer buffer, out int count) {
        if (!isDirty) {
            count = 0;
            return false;
        }

        isDirty = false;
        count = componentList.Count;
        var dataArray = new TData[componentList.Count];
        for (int i = 0; i < componentList.Count; i++) {
            dataArray[i] = componentList[i].GetData ();
        }
        buffer.SetData (dataArray);
        return true;
    }

    public abstract TData GetData ();
}
