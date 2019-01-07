using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RenderWithRayTracing : MonoBehaviour {

    [SerializeField] int renderTextureSize = 1024;
    [SerializeField] ComputeShader rayTracingKernals;

    Camera mainCamera;
    RenderTexture renderTarget;

    Vector3[] frustumCorners = new Vector3[4];
    Vector4 renderParameter;
    Matrix4x4 cameraRayParameter;

    int initCameraRaysKernelID;
    int rayTracingKernelID;
    int normalizeSamplesKernelID;

    ComputeBuffer sphereBuffer;

    void OnEnable () {
        initCameraRaysKernelID = rayTracingKernals.FindKernel ("InitCameraRays");
        rayTracingKernelID = rayTracingKernals.FindKernel ("RayTracing");
        normalizeSamplesKernelID = rayTracingKernals.FindKernel ("NormalizeSamples");

        mainCamera = GetComponent<Camera> ();

        renderTarget = new RenderTexture (renderTextureSize, renderTextureSize, 0);
        renderTarget.enableRandomWrite = true;
        renderTarget.Create ();
        rayTracingKernals.SetTexture (rayTracingKernelID, "result", renderTarget);

        sphereBuffer = new ComputeBuffer (1000, RayTracingObjectDataSize.Sphere);
        rayTracingKernals.SetBuffer (rayTracingKernelID, "sphereBuffer", sphereBuffer);
    }

    void OnDisable () {
        if (sphereBuffer != null) {
            sphereBuffer.Release ();
        }
    }

    // render pipeline
    void OnPreRender () {
        UpdateKernalParameters ();

        rayTracingKernals.SetInt ("sphereNumber", RayTracingObjectManager.instance.sphereNumber);
        sphereBuffer.SetData (RayTracingObjectManager.instance.sphereDataArray);
    }

    void OnRenderObject () {
        rayTracingKernals.Dispatch (rayTracingKernelID, Mathf.CeilToInt (renderTextureSize / 8.0f), Mathf.CeilToInt (renderTextureSize / 8.0f), 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderTarget, dest);
    }

    // internal methods
    void UpdateKernalParameters () {
        renderParameter = new Vector4 (renderTarget.width, renderTarget.height, 1f / renderTarget.width, 1f / renderTarget.height);
        rayTracingKernals.SetVector ("renderParameter", renderParameter);

        mainCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), mainCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraRayParameter.SetRow (0, transform.position);
        cameraRayParameter.SetRow (1, Vector3.Normalize (frustumCorners[0]));
        cameraRayParameter.SetRow (2, Vector3.Normalize (frustumCorners[3]) - Vector3.Normalize (frustumCorners[0]));
        cameraRayParameter.SetRow (3, Vector3.Normalize (frustumCorners[1]) - Vector3.Normalize (frustumCorners[0]));
        rayTracingKernals.SetMatrix ("cameraRayParameter", cameraRayParameter);
    }
}