using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particles3D : MonoBehaviour
{
    public uint particleCountSize = 1;
    public float particleZoneSize = 2.0f;
    public float particleBornZoneSize = 0.5f;
    public ComputeShader particleSys;
    public Material particleMaterial;
    public float curlAmt = 0.001f;
    public float noiseScale = 1.0f;
    public Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);

    private ComputeBuffer argsBuffer;
    private ComputeBuffer positionBuffer;

    // Start is called before the first frame update
    void Start()
    {
        argsBuffer = new ComputeBuffer(1, 4 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[4];
        args[0] = 6 * particleCountSize * particleCountSize * particleCountSize;
        args[1] = 1;
        args[2] = 0;
        args[3] = 0;
        argsBuffer.SetData(args);

        positionBuffer = new ComputeBuffer((int)(particleCountSize * particleCountSize * particleCountSize), 3 * sizeof(float));

        Vector3[] position = new Vector3[particleCountSize * particleCountSize * particleCountSize];
        float step = particleBornZoneSize / particleCountSize;
        for (int i = 0; i < particleCountSize; i++)
        {
            for (int j = 0; j < particleCountSize; j++)
            {
                for (int k = 0; k < particleCountSize; k++)
                {
                    int index = (int)(i * particleCountSize * particleCountSize + j * particleCountSize + k);
                    position[index].x = -particleBornZoneSize / 2.0f + k * step;
                    position[index].y = -particleBornZoneSize / 2.0f + j * step;
                    position[index].z = -particleBornZoneSize / 2.0f + i * step;
                }
            }
        }
        positionBuffer.SetData(position);
    }

    // Update is called once per frame
    void Update()
    {
        // Compute all particle's position
        int kernelId = particleSys.FindKernel("CSMain");
        particleSys.SetVector("FixedVel", new Vector4(vel.x, vel.y, vel.z, 0.0f));
        particleSys.SetFloat("ParticleSize", (float)particleCountSize);
        particleSys.SetFloat("ParticleZoneSize", particleZoneSize);
        particleSys.SetFloat("ParticleBornZoneSize", particleBornZoneSize);
        particleSys.SetFloat("CurlMount", curlAmt);
        particleSys.SetFloat("Time", 1.0f / 60.0f);
        particleSys.SetFloat("NoiseScale", noiseScale);
        particleSys.SetBuffer(kernelId, "Result", positionBuffer);
        particleSys.Dispatch(kernelId, (int)particleCountSize / 16, (int)particleCountSize / 8, (int)particleCountSize / 8);

        // Setup material
        particleMaterial.SetBuffer("positionBuffer", positionBuffer);

        // Draw particles
        Graphics.DrawProceduralIndirect(particleMaterial
            , new Bounds(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(10.0f, 10.0f, 10.0f))
            , MeshTopology.Triangles
            , argsBuffer);
    }
}
