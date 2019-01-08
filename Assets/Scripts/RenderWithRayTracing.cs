using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RenderWithRayTracing : MonoBehaviour {

    [SerializeField] int renderTextureWidth = 1024;
    [SerializeField] int renderTextureHeight = 1024;
    [SerializeField] int superSampling = 8;
    [SerializeField] ComputeShader rayTracingKernals;

    Camera renderCamera;
    RenderTexture renderTarget;

    Vector3[] frustumCorners = new Vector3[4];
    Vector4 renderParameter;
    Matrix4x4 cameraRayParameter;

    int initCameraRaysKernelID;
    int rayTracingKernelID;
    int normalizeSamplesKernelID;

    ComputeBuffer rayBuffer;
    ComputeBuffer sphereBuffer;

    void OnEnable () {
        renderCamera = GetComponent<Camera> ();

        initCameraRaysKernelID = rayTracingKernals.FindKernel ("InitCameraRays");
        rayTracingKernelID = rayTracingKernals.FindKernel ("RayTracing");
        normalizeSamplesKernelID = rayTracingKernals.FindKernel ("NormalizeSamples");

        renderTarget = new RenderTexture (renderTextureWidth, renderTextureHeight, 0);
        renderTarget.enableRandomWrite = true;
        renderTarget.Create ();
        rayTracingKernals.SetTexture (initCameraRaysKernelID, "result", renderTarget);
        rayTracingKernals.SetTexture (rayTracingKernelID, "result", renderTarget);
        rayTracingKernals.SetTexture (normalizeSamplesKernelID, "result", renderTarget);

        rayBuffer = new ComputeBuffer (renderTextureWidth * renderTextureHeight * superSampling, StructDataSize.Ray);
        rayTracingKernals.SetBuffer (initCameraRaysKernelID, "rayBuffer", rayBuffer);
        rayTracingKernals.SetBuffer (rayTracingKernelID, "rayBuffer", rayBuffer);
        rayTracingKernals.SetBuffer (normalizeSamplesKernelID, "rayBuffer", rayBuffer);

        sphereBuffer = new ComputeBuffer (RayTracingObjectManager.MAX_OBJECT_COUNT, StructDataSize.Sphere);
        rayTracingKernals.SetBuffer (rayTracingKernelID, "sphereBuffer", sphereBuffer);
    }

    void OnDisable () {
        if (rayBuffer != null) rayBuffer.Release ();
        if (sphereBuffer != null) sphereBuffer.Release ();
    }

    // render pipeline
    void OnPreRender () {
        UpdateKernalParameters ();

        rayTracingKernals.SetInt ("sphereNumber", RayTracingObjectManager.instance.sphereNumber);
        sphereBuffer.SetData (RayTracingObjectManager.instance.sphereDataArray);
    }

    void OnRenderObject () {
        rayTracingKernals.Dispatch (initCameraRaysKernelID, Mathf.CeilToInt (renderTextureWidth / 8.0f), Mathf.CeilToInt (renderTextureHeight / 8.0f), superSampling);
        rayTracingKernals.Dispatch (rayTracingKernelID, Mathf.CeilToInt (renderTextureWidth / 8.0f), Mathf.CeilToInt (renderTextureHeight / 8.0f), superSampling);
        rayTracingKernals.Dispatch (normalizeSamplesKernelID, Mathf.CeilToInt (renderTextureWidth / 8.0f), Mathf.CeilToInt (renderTextureHeight / 8.0f), 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderTarget, dest);
    }

    // internal methods
    void UpdateKernalParameters () {
        renderParameter = new Vector4 (renderTarget.width, renderTarget.height, 1f / renderTarget.width, 1f / renderTarget.height);
        rayTracingKernals.SetVector ("renderParameter", renderParameter);

        renderCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), renderCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraRayParameter.SetRow (0, transform.position);
        cameraRayParameter.SetRow (1, Vector3.Normalize (frustumCorners[0]));
        cameraRayParameter.SetRow (2, Vector3.Normalize (frustumCorners[3]) - Vector3.Normalize (frustumCorners[0]));
        cameraRayParameter.SetRow (3, Vector3.Normalize (frustumCorners[1]) - Vector3.Normalize (frustumCorners[0]));
        rayTracingKernals.SetMatrix ("cameraRayParameter", cameraRayParameter);
    }
}