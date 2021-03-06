﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent (typeof (Camera))]
public class RayTracingCamera : MonoBehaviour {

    public const int MAX_OBJECT_COUNT = 1024;
    public const int TRANSFORM_DATA_STRIDE = 4 * 6 * sizeof (float);
    public const int RAY_DATA_STRIDE = 3 * 4 * sizeof (float);

    [Header ("Shaders")]
    [SerializeField] ComputeShader initializeRaysCS;
    [SerializeField] ComputeShader resetRaysCS;
    [SerializeField] ComputeShader prepareCS;
    [SerializeField] ComputeShader rayTracingCS;
    [SerializeField] ComputeShader normalizeResultCS;
    [SerializeField] Shader denoiseShader;

    int initializeRaysKernelID;
    int resetRaysKernelID;
    int prepareKernelID;
    int rayTracingKernelID;
    int normalizeResultKernelID;

    [Header ("Quality Settings")]
    [SerializeField] int renderTextureWidth = 1024;
    [SerializeField] int renderTextureHeight = 1024;
    // [SerializeField] int superSampling = 8;

    [Header ("Lighting Settings")]
    [SerializeField] Light sunLight;
    [SerializeField] Color skyColor;
    [SerializeField, Range (0f, 1f)] float lightBounceRatio = 0.5f;

    [Header ("Other Settings")]
    [SerializeField] bool denoise;
    [SerializeField, Range (1f, 256f)] float denoiseExponent = 128f;

    Camera renderCamera;
    Vector3[] frustumCorners = new Vector3[4];
    Matrix4x4 cameraFrustumCorners;

    RenderTexture renderResult;
    Vector2Int threadGroup = new Vector2Int ();

    int fibonacciOffset = 0;
    float randOffset = 0f;

    ComputeBuffer fibonacciBuffer;
    ComputeBuffer rayBuffer;
    ComputeBuffer sampleBuffer;
    ComputeBuffer shapeBuffer;
    ComputeBuffer transformBuffer;

    bool sceneChanged = true;
    Material denoiseMaterial;

    // initializing part
    void OnEnable () {
        if (!Init ()) {
            enabled = false;
            return;
        }

        UpdateLightingParameters ();
    }

    void OnDisable () {
        ReleaseComputeBuffer (ref fibonacciBuffer);
        ReleaseComputeBuffer (ref rayBuffer);
        ReleaseComputeBuffer (ref sampleBuffer);
        ReleaseComputeBuffer (ref shapeBuffer);
        ReleaseComputeBuffer (ref transformBuffer);
    }

    void OnValidate () {
        sceneChanged = true;
        UpdateLightingParameters ();
    }

    // rendering part
    void LateUpdate () {
        UpdateCameraParameters ();
        UpdateRandomParameters ();
        UpdateComputeBuffer ();
    }

    void OnPreCull () {
        if (sceneChanged) {
            sceneChanged = false;
            initializeRaysCS.Dispatch (initializeRaysKernelID, threadGroup.x, threadGroup.y, 1);
            prepareCS.Dispatch (prepareKernelID, Mathf.CeilToInt (MAX_OBJECT_COUNT / 128.0f), 1, 1);
        } else {
            resetRaysCS.Dispatch (resetRaysKernelID, threadGroup.x, threadGroup.y, 1);
        }
        rayTracingCS.Dispatch (rayTracingKernelID, threadGroup.x, threadGroup.y, 1);
        normalizeResultCS.Dispatch (normalizeResultKernelID, threadGroup.x, threadGroup.y, 1);
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
    bool Init () {
        renderCamera = GetComponent<Camera> ();
        renderCamera.cullingMask = 0;

        if (!TryGetKernels ()) {
            return false;
        }

        SetupResultRenderTexture ();
        SetupComputerBuffers ();
        return true;
    }

    bool TryGetKernels () {
        return
        TryGetKernel ("CSMain", ref initializeRaysCS, ref initializeRaysKernelID) &&
        TryGetKernel ("CSMain", ref resetRaysCS, ref resetRaysKernelID) &&
        TryGetKernel ("CSMain", ref prepareCS, ref prepareKernelID) &&
        TryGetKernel ("CSMain", ref rayTracingCS, ref rayTracingKernelID) &&
        TryGetKernel ("CSMain", ref normalizeResultCS, ref normalizeResultKernelID);
    }

    void SetupResultRenderTexture () {
        renderResult = new RenderTexture (renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.DefaultHDR);
        renderResult.enableRandomWrite = true;
        renderResult.Create ();

        threadGroup.x = Mathf.CeilToInt (renderTextureWidth / 128.0f);
        threadGroup.y = renderTextureHeight;

        initializeRaysCS.SetTexture (initializeRaysKernelID, "_Result", renderResult);
        resetRaysCS.SetTexture (resetRaysKernelID, "_Result", renderResult);
        rayTracingCS.SetTexture (rayTracingKernelID, "_Result", renderResult);
        normalizeResultCS.SetTexture (normalizeResultKernelID, "_Result", renderResult);
    }

    void SetupComputerBuffers () {
        fibonacciBuffer = CreateSphericalFibonacciBuffer ();
        rayBuffer = new ComputeBuffer (renderTextureWidth * renderTextureHeight, RAY_DATA_STRIDE, ComputeBufferType.Default);
        sampleBuffer = new ComputeBuffer (renderTextureWidth * renderTextureHeight, 4 * sizeof (float), ComputeBufferType.Default);
        shapeBuffer = new ComputeBuffer (MAX_OBJECT_COUNT, ShapeData.Stride, ComputeBufferType.Default);
        transformBuffer = new ComputeBuffer (MAX_OBJECT_COUNT, TRANSFORM_DATA_STRIDE, ComputeBufferType.Default);

        initializeRaysCS.SetBuffer (initializeRaysKernelID, "_RayBuffer", rayBuffer);
        initializeRaysCS.SetBuffer (initializeRaysKernelID, "_SampleBuffer", sampleBuffer);

        resetRaysCS.SetBuffer (resetRaysKernelID, "_RayBuffer", rayBuffer);

        prepareCS.SetBuffer (prepareKernelID, "_ShapeBuffer", shapeBuffer);
        prepareCS.SetBuffer (prepareKernelID, "_TransformData", transformBuffer);

        rayTracingCS.SetBuffer (rayTracingKernelID, "_RayBuffer", rayBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_SphericalSampleBuffer", fibonacciBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_ShapeBuffer", shapeBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_TransformData", transformBuffer);

        normalizeResultCS.SetBuffer (normalizeResultKernelID, "_RayBuffer", rayBuffer);
        normalizeResultCS.SetBuffer (normalizeResultKernelID, "_SampleBuffer", sampleBuffer);
    }

    void UpdateLightingParameters () {
        if (sunLight != null) {
            rayTracingCS.SetVector ("_SunColor", sunLight.color);
            rayTracingCS.SetVector ("_SunDirection", sunLight.transform.forward);
        }
        rayTracingCS.SetVector ("_SkyColor", skyColor);
        rayTracingCS.SetFloat ("_BounceRatio", lightBounceRatio);
    }

    void UpdateCameraParameters () {
        renderCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), renderCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraFrustumCorners.SetRow (0, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[0])));
        cameraFrustumCorners.SetRow (1, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[1])));
        cameraFrustumCorners.SetRow (2, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[2])));
        cameraFrustumCorners.SetRow (3, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[3])));

        initializeRaysCS.SetMatrix ("_CameraFrustumCorners", cameraFrustumCorners);
        initializeRaysCS.SetVector ("_WorldSpaceCameraPos", renderCamera.transform.position);
        resetRaysCS.SetMatrix ("_CameraFrustumCorners", cameraFrustumCorners);
        resetRaysCS.SetVector ("_WorldSpaceCameraPos", renderCamera.transform.position);


    }

    void UpdateRandomParameters () {
        fibonacciOffset = (fibonacciOffset + 179) % 4096;
        rayTracingCS.SetInt ("_SphericalSampleOffset", fibonacciOffset);

        randOffset = (randOffset + 0.19f) % 1f;
        rayTracingCS.SetFloat ("_RandOffset", randOffset);
    }

    void UpdateComputeBuffer () {
        var count = 0;

        if (RayTracingShape.UpdateComputeBufferIfNeeded (ref shapeBuffer, out count)) {
            prepareCS.SetInt ("_ShapeNumber", count);
            rayTracingCS.SetInt ("_ShapeNumber", count);
            RayTracingShape.RebuildAabbTree ();
            sceneChanged = true;
        }
    }

    #region Utility Functions
    // Utility Functions
    static void ReleaseComputeBuffer (ref ComputeBuffer cb) {
        if (cb != null) {
            cb.Release ();
            cb = null;
        }
    }

    static bool TryGetKernel (string kernelName, ref ComputeShader cs, ref int kernelID) {
        if (!cs.HasKernel (kernelName)) {
            Debug.LogError (kernelName + " kernel not found in " + cs.name + "!");
            return false;
        }

        kernelID = cs.FindKernel (kernelName);
        return true;
    }

    static Vector3[] SphericalFibonacci () {
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
                (float)(System.Math.Cos (phij) * System.Math.Sin (thetaj)),
                (float)(zj),
                (float)(System.Math.Sin (thetaj) * System.Math.Sin (phij))
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

    static ComputeBuffer CreateSphericalFibonacciBuffer () {
        var buffer = new ComputeBuffer (4096, 12);
        buffer.SetData (SphericalFibonacci ());
        return buffer;
    }
    #endregion
}