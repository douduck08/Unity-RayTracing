using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (MeshRenderer))]
public class RayTracingSphere : MonoBehaviour {

    MeshRenderer meshRenderer;

    void OnEnable () {
        RayTracingObjectManager.instance.AddObject (this);
        meshRenderer = GetComponent<MeshRenderer> ();
    }

    void OnDisable () {
        RayTracingObjectManager.instance.RemoveObject (this);
    }

    public SphereData GetSphereData () {
        SphereData result;
        result.position = transform.position;
        result.radius = transform.lossyScale.x / 2;
        result.color = meshRenderer.sharedMaterial.GetColor ("_Color");
        return result;
    }
}