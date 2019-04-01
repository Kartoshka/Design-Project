using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public class LeadrGenerator : MonoBehaviour {

    public Texture2D DisplacementMap;
    public float Scale;

	public static Texture2D DisplacementToNormalMap(Texture2D DisplacementMap, string textureName, float scale = 1.0f) {
        // Create a texture the size of the screen, RGB24 format
        int width = DisplacementMap.width;
        int height = DisplacementMap.height;

        string path = AssetDatabase.GetAssetPath(DisplacementMap);
        path = path.Substring(0, path.Length - Path.GetFileName(path).Length) + textureName + "_NM.exr";

        Texture2D normalMapTex = new Texture2D(width, height, TextureFormat.RGBAHalf, false);

        Color[] pix = DisplacementMap.GetPixels();

        //Reference code by Jonathan Dupuis, adapted for C# and Unity3D
        //URL: https://github.com/jdupuy/dj_brdf/blob/phd-2016/utils/dmap2nmap.cpp
        for (int i = 0; i < width; ++i) {
            for (int j = 0; j < height; ++j) {

                Color px_left = DisplacementMap.GetPixel(i - 1, j);
                Color px_right = DisplacementMap.GetPixel(i + 1, j);
                Color px_bottom = DisplacementMap.GetPixel(i, j + 1);
                Color px_top = DisplacementMap.GetPixel(i, j - 1);

                float slope_x =  0.5f * scale * (px_right.grayscale - px_left.grayscale);
                float slope_y =  0.5f * scale * (px_top.grayscale - px_bottom.grayscale);

                float nrm_sqr = 1.0f + slope_x * slope_x + slope_y * slope_y;
                float nrm_inv = 1.0f / Mathf.Sqrt(nrm_sqr);

                float nx = slope_x * nrm_inv;
                float ny = slope_y * nrm_inv;
                float nz = nrm_inv;
                float tmp1 = 0.5f * nx + 0.5f; 
                float tmp2 = 0.5f * ny + 0.5f;

                normalMapTex.SetPixel(i, j, new Color(tmp1, tmp2, nz));
            }
        }

        normalMapTex.Apply(false, false);

        // Encode texture into EXR
        byte[] bytes_normal_map = normalMapTex.EncodeToEXR();

        Object.DestroyImmediate(normalMapTex);

        File.WriteAllBytes(path, bytes_normal_map);
        AssetDatabase.Refresh();

        TextureImporter tImporter = AssetImporter.GetAtPath(path) as TextureImporter;

        tImporter.sRGBTexture = false;
        tImporter.textureCompression = TextureImporterCompression.Uncompressed;
        tImporter.alphaIsTransparency = false;
        tImporter.isReadable = true;
        tImporter.filterMode = FilterMode.Bilinear;

        tImporter.SaveAndReimport();

        Texture2D writtenTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
        return writtenTexture;
    }

    public static void DisplacementToLeanMap(Texture2D DisplacementMap, string textureName, float scale) {
        Texture2D normalMap = DisplacementToNormalMap(DisplacementMap, textureName, scale);
        if(normalMap!=null) {
            LeanMapGenerator.NormalMapToLeanMaps(normalMap, textureName);
        }
    }

}
