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

        var color = meshRenderer.sharedMaterial.GetColor ("_Color");
        var smoothness = meshRenderer.sharedMaterial.GetFloat ("_Glossiness");
        var metallic = meshRenderer.sharedMaterial.GetFloat ("_Metallic");

        result.albedo = Color.Lerp (color, new Color (0.04f, 0.04f, 0.04f), metallic);
        result.albedo.a = color.a;
        result.specular = meshRenderer.sharedMaterial.GetColor ("_Color") * smoothness;
        result.specular.a = 1f - smoothness;
        return result;
    }
}