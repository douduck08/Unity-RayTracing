using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlaneData {
    public Vector3 position;
    public Vector3 normal;
    public Color albedo;
    public Color specular;
    public int material;
}

[RequireComponent (typeof (MeshRenderer))]
public class RayTracingPlane : RayTracingObjectBase<RayTracingPlane, PlaneData>, IRayTracingObject<PlaneData> {

    // (3 + 3 + 4 + 4 + 1) * 4 = 15 * 4
    public const int DATA_SIZE = 60;

    [SerializeField] MaterialType materialType = MaterialType.Diffuse;
    [SerializeField] Color color = Color.white;
    [SerializeField, Range (0f, 1f)] float metallic = 0.02f;
    [SerializeField, Range (0f, 1f)] float glossiness = 0.5f;

    MaterialPropertyBlock materialPropertyBlock;
    MeshRenderer meshRenderer;

    void OnEnable () {
        Init ();
        UpdateMaterialPropertyBlock ();
        AddObject (this);
    }

    void OnDisable () {
        RemoveObject (this);
    }

    void OnValidate () {
        Init ();
        UpdateMaterialPropertyBlock ();
        UpdateObject (this);
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

    public void SetMaterial (MaterialType materialType, Color color, float metallic = 0.02f, float glossiness = 0.5f) {
        this.materialType = materialType;
        this.color = color;
        this.metallic = Mathf.Clamp01 (metallic);
        this.glossiness = Mathf.Clamp01 (glossiness);
        OnValidate ();
    }

    public override PlaneData GetData () {
        PlaneData result;
        result.position = transform.position;
        result.normal = transform.up;
        result.albedo = Color.Lerp (color, new Color (0.04f, 0.04f, 0.04f), metallic);
        result.albedo.a = color.a;
        result.specular = Color.Lerp (new Color (1f, 1f, 1f), color, metallic); ;
        result.specular.a = 1f - glossiness;
        result.material = (int)materialType;
        return result;
    }
}