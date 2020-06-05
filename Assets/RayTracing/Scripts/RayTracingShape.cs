using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType {
    Sphere = 1,
    Box = 2,
    Plane = 3
}

public struct ShapeData {
    public static readonly int Stride = 9 * sizeof (float) + sizeof (int) + MaterialData.Stride;

    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public int type;
    public MaterialData material;
}

[RequireComponent (typeof (MeshRenderer))]
public abstract class RayTracingShape : MonoBehaviour {

    static bool isDirty = true;
    static List<RayTracingShape> objectList = new List<RayTracingShape> ();
    static AabbTree aabbTree = new AabbTree ();

    public static bool UpdateComputeBufferIfNeeded (ref ComputeBuffer buffer, out int count) {
        if (!isDirty) {
            count = 0;
            return false;
        }

        isDirty = false;
        count = objectList.Count;
        var dataArray = new ShapeData[count];
        for (int i = 0; i < count; i++) {
            dataArray[i] = objectList[i].GetShapeData ();
        }
        buffer.SetData (dataArray);
        return true;
    }

    public static bool RebuildAabbTree () {
        aabbTree.Build (objectList);
        return true;
    }

    public static void DrawGizmos () {
        if (Application.isPlaying) {
            aabbTree.DrawGizmos ();
        }
    }

    static void AddObject (RayTracingShape obj) {
        isDirty = true;
        objectList.Add (obj);
    }

    static void RemoveObject (RayTracingShape obj) {
        isDirty = true;
        objectList.Remove (obj);
    }

    static void UpdateObject (RayTracingShape obj) {
        isDirty = true;
    }

    public RayTracingMaterial material;

    protected void OnEnable () {
        material.BindRenderer (GetComponent<MeshRenderer> ());
        AddObject (this);
    }

    protected void OnDisable () {
        RemoveObject (this);
    }

    void OnValidate () {
        material.BindRenderer (GetComponent<MeshRenderer> ());
        UpdateObject (this);
    }

    public abstract ShapeData GetShapeData ();
    public abstract Aabb GetAabb ();
}

public static class RayTracingObjectManager {

    static bool isDirty = true;
    static List<RayTracingBox> objectList = new List<RayTracingBox> ();

    static AabbTree aabbTree = new AabbTree ();

    public static void AddObject (RayTracingBox obj) {
        isDirty = true;
        objectList.Add (obj);
    }

    public static void RemoveObject (RayTracingBox obj) {
        isDirty = true;
        objectList.Remove (obj);
    }

    public static void UpdateObject (RayTracingBox obj) {
        isDirty = true;
    }

    public static void BuildAabbTree () {
        aabbTree.Build (objectList);
    }

    public static void DrawGizmos () {
        aabbTree.DrawGizmos ();
    }
}
