using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

static class ShaderResource
{
    public static int DepthTexIdentifier = Shader.PropertyToID("_CameraDepthTexture");
    public static int ColorTexIdentifier = Shader.PropertyToID("_CameraColorTexture");
    public static int HashTexIdentifier = Shader.PropertyToID("_HashTexture");
    public static int ReflectionTexIdentifier = Shader.PropertyToID("_ReflectionTexture");
}

public class SSPRHashPass : ScriptableRenderPass
{
    private string profilerTag;
    private ComputeShader hashShader;

    public SSPRHashPass(string profilerTag, RenderPassEvent renderPassEvent, ComputeShader compute)
    {
        this.renderPassEvent = renderPassEvent;
        this.profilerTag = profilerTag;
        this.hashShader = compute;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
        using (new ProfilingSample(cmd, profilerTag))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;

            // Downsample
            int reflWidth = width / 1;
            int reflHeight = height / 1;

            RenderTextureDescriptor desc = new RenderTextureDescriptor();
            desc.width = reflWidth;
            desc.height = reflHeight;
            desc.enableRandomWrite = true;
            desc.msaaSamples = 1;
            desc.dimension = TextureDimension.Tex2D;
            desc.colorFormat = RenderTextureFormat.RInt;
            cmd.GetTemporaryRT(ShaderResource.HashTexIdentifier, desc);

            Matrix4x4 view = renderingData.cameraData.camera.worldToCameraMatrix;
            Matrix4x4 proj = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, true);
            Matrix4x4 vp = proj * view;
            Matrix4x4 invVP = (proj * view).inverse;

            cmd.SetComputeMatrixParam(hashShader, "VPMatrix", vp);
            cmd.SetComputeMatrixParam(hashShader, "InvVPMatrix", invVP);
            cmd.SetComputeIntParam(hashShader, "Width", width);
            cmd.SetComputeIntParam(hashShader, "Height", height);
            cmd.SetComputeIntParam(hashShader, "ReflectWidth", reflWidth);
            cmd.SetComputeIntParam(hashShader, "ReflectHeight", reflHeight);

            // Clear Hash Texture
            int kernel = hashShader.FindKernel("SSPRClear_Main");
            uint threadGroupSizeX = 0, threadGroupSizeY = 0, threadGroupSizeZ = 0;
            hashShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

            cmd.SetComputeTextureParam(hashShader, kernel, "ClearHashTexture", ShaderResource.HashTexIdentifier);
            cmd.DispatchCompute(hashShader, kernel, reflWidth / (int)threadGroupSizeX + 1, height / (int)threadGroupSizeY + 1, 1);

            // Compute Hash Texture
            kernel = hashShader.FindKernel("SSPRHash_Main");
            hashShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

            cmd.SetComputeTextureParam(hashShader, kernel, "DepthTex", ShaderResource.DepthTexIdentifier);
            cmd.SetComputeTextureParam(hashShader, kernel, "HashResult", ShaderResource.HashTexIdentifier);
            cmd.DispatchCompute(hashShader, kernel, reflWidth / (int)threadGroupSizeX + 1, reflHeight / (int)threadGroupSizeY + 1, 1);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

public class SSPRResolvePass : ScriptableRenderPass
{
    private string profilerTag = null;
    private ComputeShader resolveShader;

    public SSPRResolvePass(string profilerTag, RenderPassEvent renderPassEvent, ComputeShader compute)
    {
        this.renderPassEvent = renderPassEvent;
        this.profilerTag = profilerTag;
        this.resolveShader = compute;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
        using (new ProfilingSample(cmd, profilerTag))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;

            // Downsample
            int reflWidth = width / 1;
            int reflHeight = height / 1;

            RenderTextureDescriptor desc = new RenderTextureDescriptor();
            desc.width = reflWidth;
            desc.height = reflHeight;
            desc.enableRandomWrite = true;
            desc.msaaSamples = 1;
            desc.dimension = TextureDimension.Tex2D;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            cmd.GetTemporaryRT(ShaderResource.ReflectionTexIdentifier, desc);

            cmd.SetComputeIntParam(resolveShader, "ReflectWidth", reflWidth);
            cmd.SetComputeIntParam(resolveShader, "ReflectHeight", reflHeight);

            int kernel = resolveShader.FindKernel("SSPRResolve_Main");

            uint threadGroupSizeX = 0, threadGroupSizeY = 0, threadGroupSizeZ = 0;
            resolveShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

            cmd.SetComputeTextureParam(resolveShader, kernel, "HashTexture", ShaderResource.HashTexIdentifier);
            cmd.SetComputeTextureParam(resolveShader, kernel, "ColorTexture", ShaderResource.ColorTexIdentifier);
            cmd.SetComputeTextureParam(resolveShader, kernel, "ReflectionTexture", ShaderResource.ReflectionTexIdentifier);
            cmd.DispatchCompute(resolveShader, kernel, reflWidth / (int)threadGroupSizeX + 1, reflHeight / (int)threadGroupSizeY + 1, 1);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

public class ScreenSpacePlanarReflection : ScriptableRendererFeature
{
    public ComputeShader ssprHashCompute;
    public ComputeShader ssprResolveCompute;

    SSPRHashPass ssprHashPass;
    SSPRResolvePass ssprResolvePass;

    public override void Create()
    {
        ssprHashPass = new SSPRHashPass("ScreenSpacePlannarReflection Hash Pass", RenderPassEvent.BeforeRenderingPostProcessing, ssprHashCompute);
        ssprResolvePass = new SSPRResolvePass("ScreenSpacePlannarReflection Pass", RenderPassEvent.BeforeRenderingPostProcessing, ssprResolveCompute);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(ssprHashPass);
        renderer.EnqueuePass(ssprResolvePass);
    }
}
