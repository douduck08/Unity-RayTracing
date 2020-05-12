using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SphereData {
    public Vector3 position; // 3
    public float radius; // 1
    public Color albedo; // 4
    public Color specular; // 4
    public int material; // 1
}

public class RayTracingSphere : RayTracingObjectBase<RayTracingSphere, SphereData>, IRayTracingObject<SphereData> {

    // (3 + 1 + 4 + 4 + 1) * 4 = 13 * 4
    public const int DATA_SIZE = 52;

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

    public override SphereData GetData () {
        SphereData result;
        result.position = transform.position;
        result.radius = transform.lossyScale.x / 2;
        result.albedo = Color.Lerp (color, new Color (0.04f, 0.04f, 0.04f), metallic);
        result.albedo.a = color.a;
        result.specular = Color.Lerp (new Color (1f, 1f, 1f), color, metallic); ;
        result.specular.a = 1f - glossiness;
        result.material = (int)materialType;
        return result;
    }
}