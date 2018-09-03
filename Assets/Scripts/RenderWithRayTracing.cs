using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RenderWithRayTracing : MonoBehaviour {

    public ComputeShader rayTracingShader;
    int kernelID;

    RenderTexture renderTarget;
    int width;
    int height;

    void Start () {
        var camera = GetComponent<Camera> ();
        width = camera.pixelWidth;
        height = camera.pixelHeight;
        renderTarget = new RenderTexture (width, height, 0);
        renderTarget.enableRandomWrite = true;
        renderTarget.Create ();

        kernelID = rayTracingShader.FindKernel ("CSMain");
        rayTracingShader.SetTexture (kernelID, "result", renderTarget);
        rayTracingShader.SetFloat ("screenWidth", width);
        rayTracingShader.SetFloat ("screenHeight", height);
    }

    void OnPreRender () {
        rayTracingShader.Dispatch (kernelID, width / 8, height / 8, 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderTarget, dest);
    }
}