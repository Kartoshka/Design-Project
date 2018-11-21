using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LeanMapGenerator))]
public class LeanEditor : Editor {
    LeanMapGenerator lmg;

    private void OnEnable()
    {
        lmg = (LeanMapGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Generate New LEAN Map"))
        {
            lmg.GenerateAndSetLeanMap();
            //EditorUtility.DisplayProgressBar("Lean Generation", "test",)
        }
    }
}
