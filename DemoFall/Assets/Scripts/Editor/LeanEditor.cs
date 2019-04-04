using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

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
            string path = AssetDatabase.GetAssetPath(lmg.NormalMap);
            string textureName = Path.GetFileNameWithoutExtension(path);

            Texture2D[] leanMaps = LeanMapGenerator.NormalMapToLeanMaps(lmg.NormalMap, textureName);

            //Set Lean maps in material
            Renderer render = lmg.GetComponent<Renderer>();
            if(render!=null) {
                Material mat = render.sharedMaterial;
                Shader leanShader = Shader.Find("Custom/LEAN");
                if(mat.shader == leanShader) {
                    mat.SetTexture("_Lean1", leanMaps[0]);
                    mat.SetTexture("_Lean2", leanMaps[1]);
                }
            }
        }
    }
}
