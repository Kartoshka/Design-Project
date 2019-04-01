using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(LeadrGenerator))]
public class LEADREditor : Editor
{
    LeadrGenerator lmg;
    private bool m_generated = false;
    private string filename = "";
    private void OnEnable() {
        lmg = (LeadrGenerator)target;
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (lmg.DisplacementMap!=null && GUILayout.Button("Generate Normal Map from Displacement Map")) {

            string path = AssetDatabase.GetAssetPath(lmg.DisplacementMap);
            filename = Path.GetFileNameWithoutExtension(path);

            LeadrGenerator.DisplacementToLeanMap(lmg.DisplacementMap, filename, lmg.Scale);
            
        }
    }
}