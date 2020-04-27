using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particles : MonoBehaviour
{
    public uint particleCountSize = 1;
    public ComputeShader particleSys;
    public Material particleMaterial;
    public float curlAmt = 0.001f;
    public float noiseScale = 1.0f;
    public float noiseDepth = 1.0f;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer positionBuffer;

    // Start is called before the first frame update
    void Start()
    {
        argsBuffer = new ComputeBuffer(1, 4 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[4];
        args[0] = 6 * particleCountSize * particleCountSize;
        args[1] = 1;
        args[2] = 0;
        args[3] = 0;
        argsBuffer.SetData(args);

        positionBuffer = new ComputeBuffer((int)(particleCountSize * particleCountSize), 2 * sizeof(float));

        Vector2[] position = new Vector2[particleCountSize * particleCountSize];
        float step = 2.0f / particleCountSize;
        for (int i = 0; i < particleCountSize; i++)
        {
            for (int j = 0; j < particleCountSize; j++)
            {
                position[j * particleCountSize + i].x = -1.0f + j * step;
                position[j * particleCountSize + i].y = -1.0f + i * step;
            }
        }
        positionBuffer.SetData(position);
    }

    // Update is called once per frame
    void Update()
    {
        // Compute all particle's position
        int kernelId = particleSys.FindKernel("CSMain");
        particleSys.SetVector("FixedVel", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        particleSys.SetFloat("ParticleSize", (float)particleCountSize);
        particleSys.SetFloat("CurlMount", curlAmt);
        particleSys.SetFloat("Time", 1.0f / 60.0f);
        particleSys.SetFloat("NoiseScale", noiseScale);
        particleSys.SetFloat("NoiseDepth", noiseDepth);
        particleSys.SetBuffer(kernelId, "Result", positionBuffer);
        particleSys.Dispatch(kernelId, (int)particleCountSize / 32, (int)particleCountSize / 32, 1);

        // Setup material
        particleMaterial.SetBuffer("positionBuffer", positionBuffer);

        // Draw particles
        Graphics.DrawProceduralIndirect(particleMaterial
            , new Bounds(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10.0f, 10.0f, 10.0f))
            , MeshTopology.Triangles
            , argsBuffer);
    }
}
