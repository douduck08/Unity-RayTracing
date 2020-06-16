using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MaterialType {
    Diffuse = 1,
    Glossy = 2,
    Translucent = 3,
    Volume = 9,
    Light = 10
}

public struct MaterialData {
    public static readonly int Stride = 8 * sizeof (float) + sizeof (int);

    public Vector4 albedo;
    public Vector4 specular;
    public int type;
}

[System.Serializable]
public class RayTracingMaterial {

    [SerializeField] MaterialType materialType = MaterialType.Diffuse;
    [SerializeField, ColorUsage (false, false)] Color color = Color.white;
    [SerializeField, Range (0f, 1f)] float metallic = 0.02f;
    [SerializeField, Range (0f, 1f)] float glossiness = 0.5f;
    [SerializeField, Range (1f, 10f)] float refractiveIndex = 1f;
    [SerializeField, Range (1f, 10f)] float lightBoost = 1f;
    [SerializeField, Range (0.1f, 10f)] float density = 1f;

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
        albedo.w = GetAlbedoW ();
        specular = Color.Lerp (new Color (1f, 1f, 1f), color, metallic); ;
        specular.w = 1f - glossiness;
        material = (int)materialType;
    }

    public void GetStructData (out MaterialData materialData) {
        materialData.albedo = Color.Lerp (color, new Color (0.04f, 0.04f, 0.04f), metallic);
        materialData.albedo.w = GetAlbedoW ();
        materialData.specular = Color.Lerp (new Color (1f, 1f, 1f), color, metallic); ;
        materialData.specular.w = 1f - glossiness;
        materialData.type = (int)materialType;
    }

    float GetAlbedoW () {
        if (materialType == MaterialType.Light) return lightBoost;
        if (materialType == MaterialType.Volume) return density;
        return refractiveIndex;
    }
}
