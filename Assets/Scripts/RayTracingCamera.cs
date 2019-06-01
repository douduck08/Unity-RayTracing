using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RayTracingCamera : MonoBehaviour {

    [Header ("Quality Settings")]
    [SerializeField] int renderTextureWidth = 1024;
    [SerializeField] int renderTextureHeight = 1024;
    [SerializeField] int superSampling = 8;
    [SerializeField] ComputeShader rayTracingKernals;

    [Header ("Light Settings")]
    [SerializeField, Range (0f, 1f)] float lightBounceRatio = 0.5f;

    [Header ("Other Settings")]
    [SerializeField] bool denoise;
    [SerializeField, Range (1f, 256f)] float denoiseExponent = 128f;
    [SerializeField] Shader denoiseShader;
    Material denoiseMaterial;

    Camera renderCamera;
    RenderTexture renderResult;
    RenderTexture historyResult;

    Vector3[] frustumCorners = new Vector3[4];
    Matrix4x4 cameraFrustumCorners;

    int initCameraRaysKernelID;
    int rayTraceKernelID;
    int normalizeSamplesKernelID;

    ComputeBuffer fibonacciBuffer;
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

        var sphericalFibonacci = SphericalFibonacci ();
        fibonacciBuffer = new ComputeBuffer (sphericalFibonacci.Length, 12);
        fibonacciBuffer.SetData (sphericalFibonacci);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "sphericalSampleBuffer", fibonacciBuffer);

        rayBuffer = new ComputeBuffer (renderTextureWidth * renderTextureHeight * superSampling, StructDataSize.Ray);
        rayTracingKernals.SetBuffer (initCameraRaysKernelID, "rayBuffer", rayBuffer);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "rayBuffer", rayBuffer);
        rayTracingKernals.SetBuffer (normalizeSamplesKernelID, "rayBuffer", rayBuffer);

        sphereBuffer = new ComputeBuffer (RayTracingObjectManager.MAX_OBJECT_COUNT, StructDataSize.Sphere);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "sphereBuffer", sphereBuffer);

        planeBuffer = new ComputeBuffer (RayTracingObjectManager.MAX_OBJECT_COUNT, StructDataSize.Plane);
        rayTracingKernals.SetBuffer (rayTraceKernelID, "planeBuffer", planeBuffer);

        UpdateLightParameters ();
    }

    void OnDisable () {
        if (fibonacciBuffer != null) fibonacciBuffer.Release ();
        if (rayBuffer != null) rayBuffer.Release ();
        if (sphereBuffer != null) sphereBuffer.Release ();
        if (planeBuffer != null) planeBuffer.Release ();
    }

    void OnValidate () {
        UpdateLightParameters ();
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
        if (denoise) {
            if (denoiseMaterial == null) {
                denoiseMaterial = new Material (denoiseShader);
            }
            denoiseMaterial.SetFloat ("_Exponent", denoiseExponent);
            Graphics.Blit (renderResult, dest, denoiseMaterial);
        } else {
            Graphics.Blit (renderResult, dest);
        }
    }

    // internal methods
    void UpdateLightParameters () {
        rayTracingKernals.SetFloat ("bounceRatio", lightBounceRatio);
    }

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

    Vector3[] SphericalFibonacci () {
        // Ref: https://github.com/jcowles/gpu-ray-tracing/blob/master/Assets/Scripts/RayTracer.cs

        Vector3[] output = new Vector3[4096];
        double n = output.Length / 2;
        double pi = Mathf.PI;
        double dphi = pi * (3 - System.Math.Sqrt (5));
        double phi = 0;
        double dz = 1 / n;
        double z = 1 - dz / 2.0f;

        for (int j = 0; j < n; j++) {
            double zj = z;
            double thetaj = System.Math.Acos (zj);
            double phij = phi % (2 * pi);
            z = z - dz;
            phi = phi + dphi;

            // spherical -> cartesian, with r = 1
            output[j] = new Vector3 (
                (float) (System.Math.Cos (phij) * System.Math.Sin (thetaj)),
                (float) (zj),
                (float) (System.Math.Sin (thetaj) * System.Math.Sin (phij))
            );
        }

        // The code above only covers a hemisphere, this mirrors it into a sphere.
        for (int i = 0; i < n; i++) {
            var vz = output[i];
            vz.y *= -1;
            output[output.Length - i - 1] = vz;
        }
        return output;
    }
}