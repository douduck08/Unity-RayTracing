using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SphereData {
    public Vector3 position; // 3
    public float radius; // 1
    public Vector4 albedo; // 4
    public Vector4 specular; // 4
    public int material; // 1
}

public class RayTracingSphere : RayTracingObjectBase<RayTracingSphere, SphereData>, IRayTracingObject<SphereData> {

    // (3 + 1 + 4 + 4 + 1) * 4 = 13 * 4
    public const int DATA_SIZE = 52;

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

    public override SphereData GetData () {
        SphereData result;
        result.position = transform.position;
        result.radius = transform.lossyScale.x / 2;
        material.GetStructData (out result.albedo, out result.specular, out result.material);
        return result;
    }
}