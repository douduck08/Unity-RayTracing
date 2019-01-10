using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Ray {
    public Vector3 origin;
    public Vector3 direction;
    public Vector3 color;
}

public struct SphereData {
    public Vector3 position;
    public float radius;
    public Color color;
}

public static class StructDataSize {
    public const int Ray = 36;
    public const int Sphere = 32;
}

public class RayTracingObjectManager {

    public const int MAX_OBJECT_COUNT = 1000;

    #region singleton part
    static RayTracingObjectManager _instance;
    public static RayTracingObjectManager instance {
        get {
            if (_instance == null) {
                _instance = new RayTracingObjectManager ();
            }
            return _instance;
        }
    }
    #endregion

    RayTracingObjectManager () { }

    bool isDirty = true;
    List<RayTracingSphere> spheres = new List<RayTracingSphere> ();
    SphereData[] sphereDatas;

    public SphereData[] sphereDataArray {
        get {
            RebuildDataArrayIfNeed ();
            return sphereDatas;
        }
    }

    public int sphereNumber {
        get {
            RebuildDataArrayIfNeed ();
            return sphereDatas.Length;
        }
    }

    public void AddObject (RayTracingSphere sphere) {
        spheres.Add (sphere);
        isDirty = true;
    }

    public void RemoveObject (RayTracingSphere sphere) {
        spheres.Remove (sphere);
        isDirty = true;
    }

    void RebuildDataArrayIfNeed () {
        if (!isDirty) return;

        sphereDatas = new SphereData[spheres.Count];
        for (int i = 0; i < spheres.Count; i++) {
            sphereDatas[i] = spheres[i].GetSphereData ();
        }
        isDirty = false;
    }

}