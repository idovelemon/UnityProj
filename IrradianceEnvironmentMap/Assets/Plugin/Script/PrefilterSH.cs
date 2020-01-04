// PrefilterSH.cs
//
// Author: i_dovelemon[1322600812@qq.com], 2020-1-1
//
// Prefilter environment to compute 9 SH coefficient
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefilterSH : MonoBehaviour
{
    public Cubemap env = null;

    public void Prefilter()
    {
        if (env == null)
        {
            Debug.LogError("Assign environment map first");
        }

        List<Color[]> envMapData = new List<Color[]>();
        envMapData.Add(env.GetPixels(CubemapFace.PositiveX));
        envMapData.Add(env.GetPixels(CubemapFace.NegativeX));
        envMapData.Add(env.GetPixels(CubemapFace.PositiveZ));
        envMapData.Add(env.GetPixels(CubemapFace.NegativeZ));
        envMapData.Add(env.GetPixels(CubemapFace.PositiveY));
        envMapData.Add(env.GetPixels(CubemapFace.NegativeY));
        List<Color> coefficient = PrefilterUtil.PrefilterSH(envMapData, env.width);
        Debug.Log("Result SH coefficient is: " + 
            coefficient[0] + " " +
            coefficient[1] + " " +
            coefficient[2] + " " +
            coefficient[3] + " " +
            coefficient[4] + " " +
            coefficient[5] + " " +
            coefficient[6] + " " +
            coefficient[7] + " " +
            coefficient[8]);

        // Update SH coeffcient
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.SetVector("_SH_L00", new Vector4(coefficient[0].r, coefficient[0].g, coefficient[0].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L11", new Vector4(coefficient[1].r, coefficient[1].g, coefficient[1].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L10", new Vector4(coefficient[2].r, coefficient[2].g, coefficient[2].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L1_1", new Vector4(coefficient[3].r, coefficient[3].g, coefficient[3].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L22", new Vector4(coefficient[4].r, coefficient[4].g, coefficient[4].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L21", new Vector4(coefficient[5].r, coefficient[5].g, coefficient[5].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L20", new Vector4(coefficient[6].r, coefficient[6].g, coefficient[6].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L2_1", new Vector4(coefficient[7].r, coefficient[7].g, coefficient[7].b, 1.0f));
            renderer.sharedMaterial.SetVector("_SH_L2_2", new Vector4(coefficient[8].r, coefficient[8].g, coefficient[8].b, 1.0f));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
