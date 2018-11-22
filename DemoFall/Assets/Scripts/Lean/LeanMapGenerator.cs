using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

// Reference: https://www.csee.umbc.edu/~olano/papers/lean/lean.pdf
public class LeanMapGenerator : MonoBehaviour {
    public Texture2D NormalMap;
    public float Smoothness;
    public float scale = 1.0f;
    public float s = 1.0f;

    private void Start()
    {
        Debug.Log(NormalMap.format.ToString());
    }

    public void GenerateAndSetLeanMap()
    {
        // Create a texture the size of the screen, RGB24 format
        int width = NormalMap.width;
        int height = NormalMap.height;

        string path = AssetDatabase.GetAssetPath(NormalMap);
        string textureName = Path.GetFileNameWithoutExtension(path);
        path = path.Substring(0, path.Length - Path.GetFileName(path).Length);

        Texture2D lean1Tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Texture2D lean2Tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        Color[] pix = NormalMap.GetPixels();
        Color[] lean_1_colors = new Color[pix.Length];
        Color[] lean_2_colors = new Color[pix.Length];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color pixel = pix[x + y * width];

                Color lean1Write = lean_1_colors[x + y * width];
                Color lean2Write = lean_2_colors[x + y * width];

                Vector3 normal = UnpackNormal(pixel);
                Color packed24 = PackNormal(normal);

                //B Map (Equation 3)
                Vector2 B = new Vector2(normal.x, normal.y) / (normal.z * scale);
                //M Map (Equation 6)
                Vector3 M = new Vector3(B.x * B.x + (1.0f / s), B.y*B.y + (1.0f / s), B.x*B.y);

                //Encode the normal in the first lean map at x y z
                lean1Write.r = packed24.r;
                lean1Write.g = packed24.g;
                lean1Write.b = packed24.b;

                lean1Write.a = 0.5f * M.z + 0.5f;

                lean2Write.r = 0.5f * B.x + 0.5f;
                lean2Write.g = 0.5f * B.y + 0.5f;
                lean2Write.b = M.x;
                lean2Write.a = M.y;

                lean_1_colors[x + y * width] = lean1Write;
                lean_2_colors[x + y * width] = lean2Write;
            }
        }

        lean1Tex.SetPixels(lean_1_colors);
        lean1Tex.Apply();

        lean2Tex.SetPixels(lean_2_colors);
        lean2Tex.Apply();

        // Encode texture into PNG
        byte[] bytes_lean1 = lean1Tex.EncodeToPNG();
        byte[] bytes_lean2 = lean2Tex.EncodeToPNG();


        Object.DestroyImmediate(lean1Tex);
        Object.DestroyImmediate(lean2Tex);

        // For testing purposes, also write to a file in the project folder
        File.WriteAllBytes(path + textureName + "Lean1.png", bytes_lean1);
        File.WriteAllBytes(path + textureName + "Lean2.png", bytes_lean2);

        Material m = GetComponent<Renderer>().sharedMaterial;
        lean1Tex = Resources.Load<Texture2D>("Textures/" + textureName + "Lean1");
        lean2Tex = Resources.Load<Texture2D>("Textures/" + textureName + "Lean2");
        m.SetTexture("_Lean1", lean1Tex);
        m.SetTexture("_Lean2", lean2Tex);

    }


    private static Vector3 UnpackNormal(Color colorData){
        Vector3 result = new Vector3();

        result.x = colorData.a * 2.0f - 1.0f;
        result.y = colorData.g * 2.0f - 1.0f;
        result.z = Mathf.Sqrt(1 - result.x * result.x - result.y * result.y);

        return result;
      }

    private static Color PackNormal(Vector3 normal)
    {
        Color result = new Color();

        result.r = normal.x * 0.5f + 0.5f;
        result.g = normal.y * 0.5f + 0.5f;
        result.b = normal.z;

        return result;
    }
}
