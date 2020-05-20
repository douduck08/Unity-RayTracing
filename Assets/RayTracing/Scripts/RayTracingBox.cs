using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BoxData {
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public Vector4 albedo;
    public Vector4 specular;
    public int material;
}

[RequireComponent (typeof (MeshRenderer))]
public class RayTracingBox : RayTracingObjectBase<RayTracingBox, BoxData>, IRayTracingObject<BoxData> {

    // (3 + 3 + 3 + 4 + 4 + 1) * 4 = 18 * 4
    public const int DATA_SIZE = 72;

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

    public override BoxData GetData () {
        BoxData result;
        result.position = transform.position;
        result.scale = transform.lossyScale;
        result.rotation = transform.eulerAngles;
        material.GetStructData (out result.albedo, out result.specular, out result.material);
        return result;
    }
}
