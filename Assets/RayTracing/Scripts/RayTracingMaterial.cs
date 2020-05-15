using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MaterialType {
    Diffuse = 1,
    Glossy = 2,
    Translucent = 3,
    Light = 10
}

[System.Serializable]
public class RayTracingMaterial {

    [SerializeField] MaterialType materialType = MaterialType.Diffuse;
    [SerializeField] Color color = Color.white;
    [SerializeField, Range (0f, 1f)] float metallic = 0.02f;
    [SerializeField, Range (0f, 1f)] float glossiness = 0.5f;
    [SerializeField, Range (1f, 10f)] float refractiveIndex = 1f;

    MaterialPropertyBlock materialPropertyBlock;
    Renderer renderer;

    public void BindRenderer (Renderer renderer) {
        this.renderer = renderer;
        UpdateMaterialPropertyBlock ();
    }

    public void UpdateMaterialPropertyBlock () {
        if (materialPropertyBlock == null) {
            materialPropertyBlock = new MaterialPropertyBlock ();
        }

        materialPropertyBlock.SetColor ("_Color", color);
        materialPropertyBlock.SetFloat ("_Glossiness", glossiness);
        materialPropertyBlock.SetFloat ("_Metallic", metallic);

        if (renderer != null) {
            renderer.SetPropertyBlock (materialPropertyBlock);
        }
    }

    public void SetMaterial (MaterialType materialType, Color color, float metallic = 0.02f, float glossiness = 0.5f) {
        this.materialType = materialType;
        this.color = color;
        this.metallic = Mathf.Clamp01 (metallic);
        this.glossiness = Mathf.Clamp01 (glossiness);
        UpdateMaterialPropertyBlock ();
    }

    public void GetStructData (out Vector4 albedo, out Vector4 specular, out int material) {
        albedo = Color.Lerp (color, new Color (0.04f, 0.04f, 0.04f), metallic);
        albedo.w = refractiveIndex;
        specular = Color.Lerp (new Color (1f, 1f, 1f), color, metallic); ;
        specular.w = 1f - glossiness;
        material = (int)materialType;
    }
}
