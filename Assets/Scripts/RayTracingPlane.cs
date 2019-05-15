using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (MeshRenderer))]
public class RayTracingPlane : MonoBehaviour {

    [SerializeField] MaterialType materialType = MaterialType.Diffuse;
    [SerializeField] Color color = Color.white;
    [SerializeField, Range (0f, 1f)] float glossiness = 0.5f;
    [SerializeField, Range (0f, 1f)] float metallic = 0.02f;

    MaterialPropertyBlock materialPropertyBlock;
    MeshRenderer meshRenderer;

    void OnEnable () {
        Init ();
        UpdateMaterialPropertyBlock ();
        RayTracingObjectManager.instance.AddObject (this);
    }

    void OnDisable () {
        RayTracingObjectManager.instance.RemoveObject (this);
    }

    void OnValidate () {
        Init ();
        UpdateMaterialPropertyBlock ();
        RayTracingObjectManager.instance.UpdateObject (this);
    }

    void Init () {
        if (materialPropertyBlock == null) {
            materialPropertyBlock = new MaterialPropertyBlock ();
            meshRenderer = GetComponent<MeshRenderer> ();
        }
    }

    void UpdateMaterialPropertyBlock () {
        materialPropertyBlock.SetColor ("_Color", color);
        materialPropertyBlock.SetFloat ("_Glossiness", glossiness);
        materialPropertyBlock.SetFloat ("_Metallic", metallic);
        meshRenderer.SetPropertyBlock (materialPropertyBlock);
    }

    public PlaneData GetData () {
        PlaneData result;
        result.position = transform.position;
        result.normal = transform.up;
        result.albedo = Color.Lerp (color, new Color (0.04f, 0.04f, 0.04f), metallic);
        result.albedo.a = color.a;
        result.specular = Color.Lerp (new Color (1f, 1f, 1f), color, metallic);;
        result.specular.a = 1f - glossiness;
        result.material = (int) materialType;
        return result;
    }
}