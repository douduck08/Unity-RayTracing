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
    public Color albedo;
    public Color specular;
    public int material;
}

public struct PlaneData {
    public Vector3 position;
    public Vector3 normal;
    public Color albedo;
    public Color specular;
    public int material;
}

public static class StructDataSize {
    public const int Ray = 36;
    public const int Sphere = 52;
    public const int Plane = 60;
}

public enum MaterialType {
    Diffuse = 1,
    Gloosy = 2
}

public class RayTracingObjectManager {

    public const int MAX_OBJECT_COUNT = 100;

    static RayTracingObjectManager _instance;
    public static RayTracingObjectManager instance {
        get {
            if (_instance == null) {
                _instance = new RayTracingObjectManager ();
            }
            return _instance;
        }
    }

    RayTracingObjectManager () { }

    bool sphereIsDirty = true;
    List<RayTracingSphere> spheres = new List<RayTracingSphere> ();
    bool planeIsDirty = true;
    List<RayTracingPlane> planes = new List<RayTracingPlane> ();

    public SphereData[] sphereArray;
    public PlaneData[] planeArray;

    public void AddObject (RayTracingSphere sphere) {
        spheres.Add (sphere);
        sphereIsDirty = true;
    }

    public void RemoveObject (RayTracingSphere sphere) {
        spheres.Remove (sphere);
        sphereIsDirty = true;
    }

    public void UpdateObject (RayTracingSphere sphere) {
        sphereIsDirty = true;
    }

    public bool RebuildSphereArrayIfNeeded () {
        if (!sphereIsDirty) return false;

        sphereIsDirty = false;
        sphereArray = new SphereData[spheres.Count];
        for (int i = 0; i < spheres.Count; i++) {
            sphereArray[i] = spheres[i].GetData ();
        }
        return true;
    }

    public void AddObject (RayTracingPlane plane) {
        planes.Add (plane);
        planeIsDirty = true;
    }

    public void RemoveObject (RayTracingPlane plane) {
        planes.Remove (plane);
        planeIsDirty = true;
    }

    public void UpdateObject (RayTracingPlane plane) {
        planeIsDirty = true;
    }

    public bool RebuildPlaneArrayIfNeeded () {
        if (!planeIsDirty) return false;

        planeIsDirty = false;
        planeArray = new PlaneData[planes.Count];
        for (int i = 0; i < planes.Count; i++) {
            planeArray[i] = planes[i].GetData ();
        }
        return true;
    }
}