using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightDirectionChange : MonoBehaviour
{
    public Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();

        Vector3 look = new Vector3(0.0f, 0.0f, 1.0f);
        Quaternion rot = Quaternion.AngleAxis(360.0f * 1.0f * Input.mousePosition.y / Screen.height, new Vector3(1.0f, 0.0f, 0.0f));
        Matrix4x4 rotMat = Matrix4x4.Rotate(rot);
        look = rotMat * look;

        //renderer.sharedMaterial.SetVector("_SunDirection", look);
    }
}
