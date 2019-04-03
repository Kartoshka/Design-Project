using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoManager : MonoBehaviour {

    [System.Serializable]
    public struct LEANMaps
    {
        public Texture2D NormalMap;
        public Texture2D Lean1;
        public Texture2D Lean2;

        public Color Albedo;
    }

    public GameObject[] Objects;
    private int m_activeObject;

    public Material[] Materials;
    private int m_activeMaterial;

    public LEANMaps[] Textures;
    private int m_activeTexture;

    public void Start() {
        EnableObject(0);
    }

    public void EnableObject(int index) {
        if (index < Objects.Length) {
            m_activeObject = index;
            SetMaterial(m_activeMaterial);
            for (int i =0; i<Objects.Length; i++) {
                Objects[i].SetActive(i == index);
            }
        }
    }

    public void SetTexture(int index) {
        if(index < Textures.Length) {
            m_activeTexture = index;
            Material mat = Materials[m_activeMaterial];
            mat.SetTexture("_NormalMap", Textures[m_activeTexture].NormalMap);
            mat.SetTexture("_Lean1", Textures[m_activeTexture].Lean1);
            mat.SetTexture("_Lean2", Textures[m_activeTexture].Lean2);
            mat.SetColor("_Albedo", Textures[m_activeTexture].Albedo);
        }
    }

    public void SetMaterial(int index) {
        if (index < Materials.Length) {
            m_activeMaterial = index;
            SetTexture(m_activeTexture);
            Renderer renderer = Objects[m_activeObject].GetComponent<Renderer>();
            renderer.sharedMaterial = Materials[m_activeMaterial];
        }
    }
}
