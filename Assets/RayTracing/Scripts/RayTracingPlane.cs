using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlaneData {
    public Vector3 position;
    public Vector3 normal;
    public Vector4 albedo;
    public Vector4 specular;
    public int material;
}

// TODO: replace RayTracingObjectBase with RayTracingShape
[RequireComponent (typeof (MeshRenderer))]
public class RayTracingPlane : RayTracingObjectBase<RayTracingPlane, PlaneData>, IRayTracingObject<PlaneData> {

    // (3 + 3 + 4 + 4 + 1) * 4 = 15 * 4
    public const int DATA_SIZE = 60;

    public RayTracingMaterial material;
    MaterialPropertyBlock materialPropertyBlock;
    MeshRenderer meshRenderer;

    void OnEnable () {
        material.BindRenderer (GetComponent<MeshRenderer> ());
        AddObject (this);
    }

    void OnDisable () {
        RemoveObject (this);
    }

    void OnValidate () {
        material.BindRenderer (GetComponent<MeshRenderer> ());
        UpdateObject (this);
    }

    public override PlaneData GetData () {
        PlaneData result;
        result.position = transform.position;
        result.normal = transform.up;
        material.GetStructData (out result.albedo, out result.specular, out result.material);
        return result;
    }
}