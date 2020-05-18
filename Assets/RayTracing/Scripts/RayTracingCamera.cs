using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent (typeof (Camera))]
public class RayTracingCamera : MonoBehaviour {

    public const int MAX_OBJECT_COUNT = 64;
    public const int RAY_STRUCT_SIZE = 64;
    // struct Ray {
    //     float3 origin;
    //     float3 direction;
    //     float3 color;
    //     float3 emission;
    //     float3 output;
    //     int count;
    // }

    [Header ("Shaders")]
    [SerializeField] ComputeShader initializeRaysCS;
    [SerializeField] ComputeShader resetRaysCS;
    [SerializeField] ComputeShader rayTracingCS;
    [SerializeField] ComputeShader normalizeResultCS;
    [SerializeField] Shader denoiseShader;

    int initializeRaysKernelID;
    int resetRaysKernelID;
    int rayTracingKernelID;
    int normalizeResultKernelID;

    [Header ("Quality Settings")]
    [SerializeField] int renderTextureWidth = 1024;
    [SerializeField] int renderTextureHeight = 1024;
    [SerializeField] int superSampling = 8;

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
    ComputeBuffer fibonacciBuffer;
    ComputeBuffer rayBuffer;
    ComputeBuffer sphereBuffer;
    ComputeBuffer planeBuffer;
    ComputeBuffer boxBuffer;

    bool needInitRay = true;
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
        ReleaseComputeBuffer (ref sphereBuffer);
        ReleaseComputeBuffer (ref planeBuffer);
        ReleaseComputeBuffer (ref boxBuffer);
    }

    void OnValidate () {
        needInitRay = true;
        UpdateLightingParameters ();
    }

    // rendering part
    void LateUpdate () {
        UpdateKernalParameters ();
        UpdateComputeBuffer ();
    }

    void OnPreCull () {
        if (needInitRay) {
            needInitRay = false;
            initializeRaysCS.Dispatch (initializeRaysKernelID, threadGroup.x, threadGroup.y, superSampling);
        } else {
            resetRaysCS.Dispatch (resetRaysKernelID, threadGroup.x, threadGroup.y, superSampling);
        }
        rayTracingCS.Dispatch (rayTracingKernelID, threadGroup.x, threadGroup.y, superSampling);
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

        renderResult = new RenderTexture (renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.DefaultHDR);
        renderResult.enableRandomWrite = true;
        renderResult.Create ();

        threadGroup.x = Mathf.CeilToInt (renderTextureWidth / 128.0f);
        threadGroup.y = renderTextureHeight;

        fibonacciBuffer = CreateSphericalFibonacciBuffer ();
        rayBuffer = new ComputeBuffer (renderTextureWidth * renderTextureHeight * superSampling, RAY_STRUCT_SIZE);
        sphereBuffer = new ComputeBuffer (MAX_OBJECT_COUNT, RayTracingSphere.DATA_SIZE);
        planeBuffer = new ComputeBuffer (MAX_OBJECT_COUNT, RayTracingPlane.DATA_SIZE);
        boxBuffer = new ComputeBuffer (MAX_OBJECT_COUNT, RayTracingBox.DATA_SIZE);

        initializeRaysCS.SetTexture (initializeRaysKernelID, "_Result", renderResult);
        initializeRaysCS.SetBuffer (initializeRaysKernelID, "_RayBuffer", rayBuffer);
        resetRaysCS.SetTexture (resetRaysKernelID, "_Result", renderResult);
        resetRaysCS.SetBuffer (resetRaysKernelID, "_RayBuffer", rayBuffer);

        rayTracingCS.SetTexture (rayTracingKernelID, "_Result", renderResult);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_RayBuffer", rayBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_SphericalSampleBuffer", fibonacciBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_SphereBuffer", sphereBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_PlaneBuffer", planeBuffer);
        rayTracingCS.SetBuffer (rayTracingKernelID, "_BoxBuffer", boxBuffer);

        normalizeResultCS.SetTexture (normalizeResultKernelID, "_Result", renderResult);
        normalizeResultCS.SetBuffer (normalizeResultKernelID, "_RayBuffer", rayBuffer);
        return true;
    }

    bool TryGetKernels () {
        return TryGetKernel ("CSMain", ref initializeRaysCS, ref initializeRaysKernelID) &&
        TryGetKernel ("CSMain", ref resetRaysCS, ref resetRaysKernelID) &&
        TryGetKernel ("CSMain", ref rayTracingCS, ref rayTracingKernelID) &&
        TryGetKernel ("CSMain", ref normalizeResultCS, ref normalizeResultKernelID);
    }

    void UpdateLightingParameters () {
        if (sunLight != null) {
            rayTracingCS.SetVector ("_SunColor", sunLight.color);
            rayTracingCS.SetVector ("_SunDirection", sunLight.transform.forward);
        }
        rayTracingCS.SetVector ("_SkyColor", skyColor);
        rayTracingCS.SetFloat ("_BounceRatio", lightBounceRatio);
    }

    void UpdateKernalParameters () {
        renderCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), renderCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraFrustumCorners.SetRow (0, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[0])));
        cameraFrustumCorners.SetRow (1, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[1])));
        cameraFrustumCorners.SetRow (2, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[2])));
        cameraFrustumCorners.SetRow (3, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[3])));

        initializeRaysCS.SetMatrix ("_CameraFrustumCorners", cameraFrustumCorners);
        initializeRaysCS.SetVector ("_WorldSpaceCameraPos", renderCamera.transform.position);
        resetRaysCS.SetMatrix ("_CameraFrustumCorners", cameraFrustumCorners);
        resetRaysCS.SetVector ("_WorldSpaceCameraPos", renderCamera.transform.position);

        fibonacciOffset = (fibonacciOffset + Mathf.CeilToInt (1793 * Time.deltaTime)) % 4096;
        rayTracingCS.SetInt ("_SphericalSampleOffset", fibonacciOffset);

    }

    void UpdateComputeBuffer () {
        var count = 0;
        if (RayTracingSphere.UpdateComputeBufferIfNeeded (ref sphereBuffer, out count)) {
            rayTracingCS.SetInt ("_SphereNumber", count);
        }
        if (RayTracingPlane.UpdateComputeBufferIfNeeded (ref planeBuffer, out count)) {
            rayTracingCS.SetInt ("_PlaneNumber", count);
        }
        if (RayTracingBox.UpdateComputeBufferIfNeeded (ref boxBuffer, out count)) {
            rayTracingCS.SetInt ("_BoxNumber", count);
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