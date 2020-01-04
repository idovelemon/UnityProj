// PrefilterBruteForceEditor.cs
//
// Author: i_dovelemon[1322600812@qq.com], 2020-1-1
//
// Editor for prefiter irradiance environment map with brute force method
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrefilterBruteForce))]
public class PrefilterBruteForceEditor : Editor
{
    private PrefilterBruteForce instance = null;

    private void OnEnable()
    {
        instance = target as PrefilterBruteForce;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();
        instance.env = (Cubemap)EditorGUILayout.ObjectField("Environment Map", instance.env, typeof(Cubemap), false);
        if (GUILayout.Button("Prefilter"))
        {
            instance.Prefilter();
        }
        EditorGUILayout.EndVertical();
    }
}