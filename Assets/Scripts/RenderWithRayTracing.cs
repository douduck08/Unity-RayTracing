﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RenderWithRayTracing : MonoBehaviour {

    [SerializeField] int renderTextureWidth = 1024;
    [SerializeField] int renderTextureHeight = 1024;
    [SerializeField] int superSampling = 8;
    [SerializeField] ComputeShader rayTracingKernals;

    Camera renderCamera;
    RenderTexture renderResult;
    RenderTexture historyResult;

    Vector3[] frustumCorners = new Vector3[4];
    Vector4 renderParameter;
    Matrix4x4 cameraRayParameter;

    int initCameraRaysKernelID;
    int rayTraceKernelID;
    int normalizeSamplesKernelID;

    ComputeBuffer rayBuffer;
    ComputeBuffer sphereBuffer;

    // initializing part
    void OnEnable () {
        renderCamera = GetComponent<Camera> ();

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
    }

    void OnDisable () {
        if (rayBuffer != null) rayBuffer.Release ();
        if (sphereBuffer != null) sphereBuffer.Release ();
    }

    // renderint part
    void OnPreRender () {
        UpdateKernalParameters ();

        rayTracingKernals.SetInt ("sphereNumber", RayTracingObjectManager.instance.sphereNumber);
        sphereBuffer.SetData (RayTracingObjectManager.instance.sphereDataArray);
    }

    void OnRenderObject () {
        rayTracingKernals.Dispatch (initCameraRaysKernelID, Mathf.CeilToInt (renderTextureWidth / 8.0f), Mathf.CeilToInt (renderTextureHeight / 8.0f), superSampling);
        rayTracingKernals.Dispatch (rayTraceKernelID, Mathf.CeilToInt (renderTextureWidth / 8.0f), Mathf.CeilToInt (renderTextureHeight / 8.0f), superSampling);
        rayTracingKernals.Dispatch (normalizeSamplesKernelID, Mathf.CeilToInt (renderTextureWidth / 8.0f), Mathf.CeilToInt (renderTextureHeight / 8.0f), 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderResult, dest);
    }

    // internal methods
    void UpdateKernalParameters () {
        renderParameter = new Vector4 (renderResult.width, renderResult.height, 1f / renderResult.width, 1f / renderResult.height);
        rayTracingKernals.SetVector ("renderParameter", renderParameter);

        renderCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), renderCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraRayParameter.SetRow (0, transform.position);
        cameraRayParameter.SetRow (1, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[0])));
        cameraRayParameter.SetRow (2, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[3]) - Vector3.Normalize (frustumCorners[0])));
        cameraRayParameter.SetRow (3, transform.localToWorldMatrix.MultiplyVector (Vector3.Normalize (frustumCorners[1]) - Vector3.Normalize (frustumCorners[0])));
        rayTracingKernals.SetMatrix ("cameraRayParameter", cameraRayParameter);
    }
}