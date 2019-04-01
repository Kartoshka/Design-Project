using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;

namespace MadGoat_SSAA
{
    #region Enums
    public enum Mode
    {
        SSAA,
        ResolutionScale,
        PerAxisScale,
        AdaptiveResolution,
        Custom
    }
    public enum SSAAMode
    {
        SSAA_OFF = 0,
        SSAA_HALF = 1,
        SSAA_X2 = 2,
        SSAA_X4 = 3
    }
    public enum Filter
    {
        NEAREST_NEIGHBOR,
        BILINEAR,
        BICUBIC
    }
    public enum ImageFormat
    {
        JPG,
        PNG,
        #if UNITY_5_6_OR_NEWER
        EXR
        #endif
    }
    public enum EditorPanoramaRes
    {
        Square128 = 128,
        Square256 = 256,
        Square512 = 512,
        Square1024 = 1024,
        Square2048 = 2048,
        Square4096 = 4096,

    }
    #endregion

    #region Classes
    [System.Serializable]
    public class SsaaProfile
    {
        [HideInInspector]
        public float multiplier;

        public bool useFilter;
        [Tooltip("Which type of filtering to be used (only applied if useShader is true)")]
        public Filter filterType = Filter.BILINEAR;
        [Tooltip("The sharpness of the filtered image (only applied if useShader is true)")]
        [Range(0, 1)]
        public float sharpness;
        [Tooltip("The distance between the samples (only applied if useShader is true)")]
        [Range(0.5f, 2f)]
        public float sampleDistance;

        public SsaaProfile(float mul, bool useDownsampling)
        {
            multiplier = mul;

            useFilter = useDownsampling;
            sharpness = useDownsampling ? 0.85f : 0;
            sampleDistance = useDownsampling ? 0.65f : 0;
        }
        public SsaaProfile(float mul, bool useDownsampling, Filter filterType, float sharp, float sampleDist)
        {
            multiplier = mul;

            this.filterType = filterType;
            useFilter = useDownsampling;
            sharpness = useDownsampling ? sharp : 0;
            sampleDistance = useDownsampling ? sampleDist : 0;
        }
    }
    [System.Serializable]
    public class ScreenshotSettings
    {
        [HideInInspector]
        public bool takeScreenshot = false;

        [Range(1, 4)]
        public int screenshotMultiplier = 1;
        public Vector2 outputResolution = new Vector2(1920, 1080);

        public bool useFilter = true;
        [Range(0, 1)]
        public float sharpness = 0.85f;
    }
    [System.Serializable]
    public class PanoramaSettings
    {
        public PanoramaSettings(int size, int mul)
        {
            panoramaMultiplier = mul;
            panoramaSize = size;
        }
        public int panoramaSize;

        [Range(1,4)]
        public int panoramaMultiplier;

        public bool useFilter = true;
        [Range(0, 1)]
        public float sharpness = 0.85f;
    }
    public class DebugData
    {
        public MadGoatSSAA instance;

        public Mode renderMode
        {
            get { return instance.renderMode; }
        }
        public float multiplier
        {
            get { return instance.multiplier; }
        }
        public bool fssaa
        {
            get { return instance.ssaaUltra; }
        }

        // Constructor
        public DebugData(MadGoatSSAA instance)
        {
            this.instance = instance;
        }
    }
    #endregion

    public static class MadGoatSSAA_Utils
    {
        // Don't forget to change me when pushing updates!
        public const string ssaa_version = "1.8.2 (+ SRP BETA 8)"; 

        /// <summary>
        /// Makes this camera's settings match the other camera and assigns a custom target texture
        /// </summary>
        public static void CopyFrom(this Camera current, Camera other, RenderTexture rt)
        {
            current.CopyFrom(other);
            current.targetTexture = rt;
        }
        public static bool DetectSRP()
        {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
            return GraphicsSettings.renderPipelineAsset != null;
#else
            return false;
#endif
        }
#if UNITY_EDITOR
        public static void GrabRenderLayer()
        {
            //  https://forum.unity3d.com/threads/adding-layer-by-script.41970/reply?quote=2274824
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            bool ExistLayer = false;
            for (int i = 10; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                
                if (layerSP.stringValue == "SSAA_RENDER")
                {
                    ExistLayer = true;
                    break;
                }

            }
            for (int j = 10; j < layers.arraySize; j++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(j);
                if (layerSP.stringValue == "" && !ExistLayer)
                {
                    layerSP.stringValue = "SSAA_RENDER";
                    tagManager.ApplyModifiedProperties();

                    break;
                }
            }
        }
#endif
    }
}