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

    [System.Serializable]
    public struct MaterialStruct
    {
        public Material Auto_LOD;
        public Material Fixed_LOD;
    }

    public GameObject[] Objects;
    private int m_activeObject;

    public MaterialStruct[] Materials;
    private int m_activeMaterial;

    public LEANMaps[] Textures;
    private int m_activeTexture;

    private float m_currentLOD;
    private bool m_autoLOD = true;

    public void Start() {
        EnableObject(0);
        SetAutoLOD(m_autoLOD);
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

            Material mat = Materials[m_activeMaterial].Auto_LOD;
            mat.SetTexture("_NormalMap", Textures[m_activeTexture].NormalMap);
            mat.SetTexture("_Lean1", Textures[m_activeTexture].Lean1);
            mat.SetTexture("_Lean2", Textures[m_activeTexture].Lean2);
            mat.SetColor("_Albedo", Textures[m_activeTexture].Albedo);

            mat = Materials[m_activeMaterial].Fixed_LOD;
            mat.SetTexture("_NormalMap", Textures[m_activeTexture].NormalMap);
            mat.SetTexture("_Lean1", Textures[m_activeTexture].Lean1);
            mat.SetTexture("_Lean2", Textures[m_activeTexture].Lean2);
            mat.SetColor("_Albedo", Textures[m_activeTexture].Albedo);
        }
    }

    private Material GetActiveMaterial() {
        return this.m_autoLOD ? Materials[m_activeMaterial].Auto_LOD : Materials[m_activeMaterial].Fixed_LOD;
    }

    public void SetMaterial(int index) {
        if (index < Materials.Length) {
            m_activeMaterial = index;
            SetTexture(m_activeTexture);
            SetLOD(m_currentLOD);
            Renderer renderer = Objects[m_activeObject].GetComponent<Renderer>();
            renderer.sharedMaterial = GetActiveMaterial();
        }
    }

    public void SetLOD(float value) {
        m_currentLOD = value;
        Materials[m_activeMaterial].Fixed_LOD.SetFloat("_LOD", value);
    }

    public void SetAutoLOD(bool value) {
        m_autoLOD = value;
        SetMaterial(m_activeMaterial);
    }
}
