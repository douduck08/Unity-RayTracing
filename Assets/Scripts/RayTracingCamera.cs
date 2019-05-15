using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RayTracingCamera : MonoBehaviour {

    [SerializeField] int renderTextureWidth = 1024;
    [SerializeField] int renderTextureHeight = 1024;
    [SerializeField] int superSampling = 8;
    [SerializeField] ComputeShader rayTracingKernals;

    Camera renderCamera;
    RenderTexture renderResult;
    RenderTexture historyResult;

    Vector3[] frustumCorners = new Vector3[4];
    Matrix4x4 cameraFrustumCorners;

    int initCameraRaysKernelID;
    int rayTraceKernelID;
    int normalizeSamplesKernelID;

    ComputeBuffer rayBuffer;
    ComputeBuffer sphereBuffer;
    ComputeBuffer planeBuffer;

    // initializing part
    void OnEnable () {
        renderCamera = GetComponent<Camera> ();
        renderCamera.cullingMask = 0;

        initCameraRaysKernelID = rayTracingKernals.FindKernel ("InitCameraRays");
        rayTraceKernelID = rayTracingKernals.FindKernel ("RayTrace");
        normalizeSamplesKernelID = rayTracingKernals.FindKernel ("NormalizeSamples");

        renderResult = new RenderTexture (renderTextureWidth, renderTextureHeight, 0);
        renderResult.enableRandomWrite = true;
        renderResult.Create ();
        rayTracingKernals.SetTexture (initCameraRaysKernelID, "result", renderResult);
        rayTracingKernals.SetTexture (rayTraceKernelID, "result", renderResult);
        rayTracingKernals.SetTexture (normalizeSamplesKernelID, "result", renderResult);

        historyResult = new RenderTexture (renderTextureWidth, renderTextureHeight, 0);
        historyResult.Create ();

        rayBuffer = new ComputeBuffer (renderTextureWidth * renderTextureHeight * superSampling, StructDataSize.Ray);
        rayTracingKernals.SetBuffer (initCameraRaysKernelID, "rayBuffer", rayBuffer);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "rayBuffer", rayBuffer);
        rayTracingKernals.SetBuffer (normalizeSamplesKernelID, "rayBuffer", rayBuffer);

        sphereBuffer = new ComputeBuffer (RayTracingObjectManager.MAX_OBJECT_COUNT, StructDataSize.Sphere);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "sphereBuffer", sphereBuffer);

        planeBuffer = new ComputeBuffer (RayTracingObjectManager.MAX_OBJECT_COUNT, StructDataSize.Plane);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "planeBuffer", planeBuffer);
    }

    void OnDisable () {
        if (rayBuffer != null) rayBuffer.Release ();
        if (sphereBuffer != null) sphereBuffer.Release ();
    }

    // rendering part
    void OnPreRender () {
        UpdateKernalParameters ();
        UpdateComputeBuffer ();
    }

    void OnRenderObject () {
        var x = Mathf.CeilToInt (renderTextureWidth / 8.0f);
        var y = Mathf.CeilToInt (renderTextureHeight / 8.0f);
        rayTracingKernals.Dispatch (initCameraRaysKernelID, x, y, superSampling);
        rayTracingKernals.Dispatch (rayTraceKernelID, x, y, superSampling);
        rayTracingKernals.Dispatch (normalizeSamplesKernelID, x, y, 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderResult, dest);
    }

    // internal methods
    void UpdateKernalParameters () {
        renderCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), renderCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraFrustumCorners.SetRow (0, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[0])));
        cameraFrustumCorners.SetRow (1, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[1])));
        cameraFrustumCorners.SetRow (2, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[2])));
        cameraFrustumCorners.SetRow (3, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[3])));
        rayTracingKernals.SetMatrix ("cameraFrustumCorners", cameraFrustumCorners);
    }

    void UpdateComputeBuffer () {
        if (RayTracingObjectManager.instance.RebuildSphereArrayIfNeeded ()) {
            var spheres = RayTracingObjectManager.instance.sphereArray;
            sphereBuffer.SetData (spheres);
            rayTracingKernals.SetInt ("sphereNumber", spheres.Length);
        }
        if (RayTracingObjectManager.instance.RebuildPlaneArrayIfNeeded ()) {
            var planes = RayTracingObjectManager.instance.planeArray;
            planeBuffer.SetData (planes);
            rayTracingKernals.SetInt ("planeNumber", planes.Length);
        }
    }
}