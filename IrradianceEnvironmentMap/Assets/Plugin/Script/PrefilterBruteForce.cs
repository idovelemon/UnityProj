using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PrefilterBruteForce : MonoBehaviour
{
    public Cubemap env = null;
    public int irradianceMapSize = 16;

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
        List<Color[]> irradianceMap = PrefilterUtil.PrefilterBruteForce(envMapData, env.width, irradianceMapSize);

        // Create cube texture
        Cubemap cube = new Cubemap(irradianceMapSize, TextureFormat.ARGB32, false);
        cube.SetPixels(irradianceMap[0], CubemapFace.PositiveX);
        cube.SetPixels(irradianceMap[1], CubemapFace.NegativeX);
        cube.SetPixels(irradianceMap[2], CubemapFace.PositiveZ);
        cube.SetPixels(irradianceMap[3], CubemapFace.NegativeZ);
        cube.SetPixels(irradianceMap[4], CubemapFace.PositiveY);
        cube.SetPixels(irradianceMap[5], CubemapFace.NegativeY);
        cube.Apply();

        AssetDatabase.CreateAsset(cube, "Assets/Resources/irradianceMap.cubemap");
        AssetDatabase.SaveAssets();

        cube = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Resources/irradianceMap.cubemap");

        // Assign it to material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.SetTexture("_MainTex", cube);
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
