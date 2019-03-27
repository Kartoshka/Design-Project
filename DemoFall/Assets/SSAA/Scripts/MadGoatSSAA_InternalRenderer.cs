using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
using UnityEngine.Rendering.PostProcessing;
#endif
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && SSAA_HDRP
using UnityEngine.Experimental.Rendering.HDPipeline;
#endif
namespace MadGoat_SSAA
{
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class MadGoatSSAA_InternalRenderer : MonoBehaviour
    {
        // Render Multiplier used by main
        private float multiplier;
        public float Multiplier
        {
            get
            {
                return multiplier;
            }

            set
            {
                multiplier = value;
            }
        }

        // Shader Parameters
        private float sharpness;
        public float Sharpness
        {
            get
            {
                return sharpness;
            }

            set
            {
                sharpness = value;
            }
        }
        private bool useShader;
        public bool UseShader
        {
            get
            {
                return useShader;
            }

            set
            {
                useShader = value;
            }
        }
        private float sampleDistance;
        public float SampleDistance
        {
            get
            {
                return sampleDistance;
            }

            set
            {
                sampleDistance = value;
            }
        }
        private bool flipImage;
        public bool FlipImage
        {
            get
            {
                return flipImage;
            }
            set
            {
                flipImage = value;
            }
        }

        // Cameras
        private Camera main;
        public Camera Main
        {
            get
            {
                return main;
            }

            set
            {
                main = value;
            }
        }
        private Camera current;
        public Camera Current
        {
            get
            {
                return current;
            }

            set
            {
                current = value;
            }
        }

        // Shader Setup
        [SerializeField]
        private Shader _bilinearshader;
        public Shader bilinearshader
        {
            get
            {
                if (_bilinearshader == null)
                    _bilinearshader = Shader.Find("Hidden/SSAA_Bilinear");

                return _bilinearshader;
            }
        }
        [SerializeField]
        private Shader _bicubicshader;
        public Shader bicubicshader
        {
            get
            {
                if (_bicubicshader == null)
                    _bicubicshader = Shader.Find("Hidden/SSAA_Bicubic");

                return _bicubicshader;
            }
        }
        [SerializeField]
        private Shader _neighborshader;
        public Shader neighborshader
        {
            get
            {
                if (_neighborshader == null)
                {
                    _neighborshader = Shader.Find("Hidden/SSAA_Nearest");
                }
                return _neighborshader;
            }
        }
        [SerializeField]
        private Shader _defaultshader;
        public Shader defaultshader
        {
            get
            {
                if (_defaultshader == null)
                {
                    _defaultshader = Shader.Find("Hidden/SSAA_Def");
                }
                return _defaultshader;
            }
        }

        // Materials Instances
        private Material material_bl; // Bilinear Material
        public Material Material_bc
        {
            get { return material_bc; }
        }
        private Material material_bc; // Bicubic
        public Material Material_bl
        {
            get { return material_bl; }
        }
        private Material material_nn; // Nearest Neighbor
        public Material Material_nn
        {
            get
            {
                return material_nn;
            }
        }
        private Material material_def; // Default
        public Material Material_def
        {
            get
            {
                return material_def;
            }
        }

        private Material material_current;
        public Material Material_current
        {
            get
            {
                return material_current;
            }

            set
            {
                material_current = value;
            }
        }
        private Material material_old = null;
        public Material Material_old
        {
            get
            {
                return material_old;
            }

            set
            {
                material_old = value;
            }
        }

        // Command buffers & misc
        private CommandBuffer copyCB;
        public CommandBuffer CopyCB
        {
            get
            {
                return copyCB;
            }

            set
            {
                copyCB = value;
            }
        }

        private CommandBuffer grabCB;
        public CommandBuffer GrabCB
        {
            get
            {
                return grabCB;
            }

            set
            {
                grabCB = value;
            }
        }

        private int postVolumePass = 0;
        public int PostVolumePass
        {
            get
            {
                return postVolumePass;
            }

            set
            {
                postVolumePass = value;
            }
        }

        private int postVolumePassOld = 0;
        public int PostVolumePassOld
        {
            get
            {
                return postVolumePassOld;
            }

            set
            {
                postVolumePassOld = value;
            }
        }

#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
        private PostProcessVolume postVolume;
        public PostProcessVolume PostVolume
        {
            get
            {
                return postVolume;
            }

            set
            {
                postVolume = value;
            }
        }
     
#endif
        // Main SSAA instance
        private MadGoatSSAA mainComponent;
        public MadGoatSSAA MainComponent
        {
            get
            {
                return mainComponent;
            }

            set
            {
                mainComponent = value;
            }
        }

        private RenderTexture baseRt;

        // screenshot function
        private void ScreenShootGrab()
        {
            if (MainComponent.screenshotSettings.takeScreenshot)
            {
                // Default material for screenshots is bicubic (we don't care about performance here, so we use whats best)
                Material material = new Material(bicubicshader);

                // buffer to store texture
                RenderTexture buff = new RenderTexture((int)MainComponent.screenshotSettings.outputResolution.x, (int)MainComponent.screenshotSettings.outputResolution.y, 24, RenderTextureFormat.ARGB32);
                bool sRGBWrite = GL.sRGBWrite;
                // enable srgb conversion for blit - fixes the color issue
                GL.sRGBWrite = true;
                // setup shader
                if (MainComponent.screenshotSettings.useFilter)
                {
                    material.SetFloat("_ResizeWidth", (int)MainComponent.screenshotSettings.outputResolution.x);
                    material.SetFloat("_ResizeHeight", (int)MainComponent.screenshotSettings.outputResolution.y);
                    material.SetFloat("_Sharpness", 0.85f);
                    Graphics.Blit(Main.targetTexture, buff, material, 0);
                }
                else // or blit as it is
                {
                    Graphics.Blit(Main.targetTexture, buff);
                }
                DestroyImmediate(material);
                RenderTexture.active = buff;

                // Copy from active texture to buffer
                Texture2D screenshotBuffer = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, false);
                screenshotBuffer.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);

                // Create path if not available and write the screenshot to disk
                (new FileInfo(MainComponent.screenshotPath)).Directory.Create();

                if (MainComponent.imageFormat == ImageFormat.PNG)
                    File.WriteAllBytes(MainComponent.screenshotPath + GetScreenshotName + ".png", screenshotBuffer.EncodeToPNG());
                else if (MainComponent.imageFormat == ImageFormat.JPG)
                    File.WriteAllBytes(MainComponent.screenshotPath + GetScreenshotName + ".jpg", screenshotBuffer.EncodeToJPG(MainComponent.JPGQuality));
#if UNITY_5_6_OR_NEWER
                else
                    File.WriteAllBytes(MainComponent.screenshotPath + GetScreenshotName + ".exr", screenshotBuffer.EncodeToEXR(MainComponent.EXR32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
#endif

                // Clean stuff
                RenderTexture.active = null;
                buff.Release();

                // restore the sRGBWrite to older state so it doesn't interfere with user's setting
                GL.sRGBWrite = sRGBWrite;

                DestroyImmediate(screenshotBuffer);
                MainComponent.screenshotSettings.takeScreenshot = false;
            }
        }

        public void ChangeMaterial(Filter Type)
        {
            Material_old = Material_current;
            PostVolumePassOld = PostVolumePass;

            // Point material_current to the given material
            switch (Type)
            {
                case Filter.NEAREST_NEIGHBOR:
                    Material_current = Material_nn;
                    PostVolumePass = 1; // for srp
                    break;
                case Filter.BILINEAR:
                    Material_current = Material_bl;
                    PostVolumePass = 2; // for srp
                    break;
                case Filter.BICUBIC:
                    Material_current = Material_bc;
                    PostVolumePass = 3; // for srp
                    break;
            }

            // Hanle the correct pass
            if ((!useShader || multiplier == 1) && Material_current != Material_def)
            {
                Material_current = Material_def;
                PostVolumePass = 0;
            }

            // if material must be changed we have to reset the command buffer
            if (Material_current != Material_old)
            {
                Material_old = Material_current;
                PostVolumePassOld = PostVolumePass;
                
                ClearCB();
                SetupCB();
            }
        }
        
        // Command buffer setup
        private void SetupCB()
        {
            CopyCB = new CommandBuffer();
            GrabCB = new CommandBuffer();
            if ((new List<CommandBuffer>(Current.GetCommandBuffers(CameraEvent.AfterEverything))).Find(x => x.name == "SSAA_COMPOSITION") == null)
            {
                CopyCB.Clear();
                GrabCB.Clear();

                CopyCB.name = "SSAA_COMPOSITION";
                CopyCB.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                GrabCB.name = "SSAA_GRAB";
                GrabCB.SetRenderTarget(BuiltinRenderTextureType.CurrentActive);

                RenderTargetIdentifier idBuff = new RenderTargetIdentifier(Main.targetTexture);
                RenderTargetIdentifier idBg = new RenderTargetIdentifier(baseRt);

                if (baseRt)
                    baseRt.Release();

                // fix unity editor startup errors
                baseRt = new RenderTexture(Screen.width == 0 ? 64 : Screen.width, Screen.height == 0 ? 64 : Screen.height, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
               

                GrabCB.Blit(BuiltinRenderTextureType.CameraTarget, idBg);
                CopyCB.SetGlobalTexture("_BaseTex", idBg);
                CopyCB.Blit(idBuff, BuiltinRenderTextureType.CameraTarget, Material_current, 0);
       
                Current.AddCommandBuffer(CameraEvent.BeforeImageEffects, GrabCB);
                Current.AddCommandBuffer(CameraEvent.AfterEverything, CopyCB);
            }
        }
        private void UpdateCB()
        {
            Material_current.SetFloat("_ResizeWidth", Screen.width);
            Material_current.SetFloat("_ResizeHeight", Screen.height);
            Material_current.SetFloat("_Sharpness", Sharpness);
            Material_current.SetFloat("_SampleDistance", SampleDistance);
            if (Screen.width != baseRt.width || Screen.height != baseRt.height)
            {
                baseRt.Release();
                baseRt.width = Screen.width;
                baseRt.height = Screen.height;
                baseRt.Create();
            }
        }
        private void ClearCB()
        {
            if ((new List<CommandBuffer>(Current.GetCommandBuffers(CameraEvent.AfterEverything))).Find(x => x.name == "SSAA_COMPOSITION") != null)
            {
                //RenderTexture.ReleaseTemporary(buff);
                Current.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, GrabCB);
                Current.RemoveCommandBuffer(CameraEvent.AfterEverything, CopyCB);
            }
        }
        private void SetupCBSRP()
        {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
            // setup post processing on internal camera

            // get the first empty render layer
#if UNITY_EDITOR
            MadGoatSSAA_Utils.GrabRenderLayer();
#endif
            gameObject.layer = LayerMask.NameToLayer("SSAA_RENDER");

            PostProcessLayer pl;
            SphereCollider trigger;

            if ((pl = GetComponent<PostProcessLayer>()) == null)
            {
                pl = gameObject.AddComponent<PostProcessLayer>();
                pl.volumeLayer = 1 << LayerMask.NameToLayer("SSAA_RENDER");
                pl.volumeTrigger = transform;
            }
            if ((PostVolume = GetComponent<PostProcessVolume>()) == null)
            {
                PostVolume = gameObject.AddComponent<PostProcessVolume>();
                PostVolume.isGlobal = false;
            }

            if (!PostVolume.sharedProfile)
                PostVolume.sharedProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
            if (PostVolume.sharedProfile.settings.Count == 0)
            {
                PostVolume.sharedProfile.AddSettings<SsaaSamplingUber>();
            }

            if ((trigger = gameObject.GetComponent<SphereCollider>()) == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.0001f;
            }
            // determine if flipping is required? HDRP

#endif
        }
        private void UpdateCBSRP()
        {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)

            // in case of user error
            if(gameObject.layer != LayerMask.NameToLayer("SSAA_RENDER"))
                gameObject.layer = LayerMask.NameToLayer("SSAA_RENDER");
            
            // setup shader pass
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).shaderPass.value = PostVolumePass;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).sourceTex.value = Main.targetTexture;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).useFXAA.value = 
                MainComponent.ssaaUltra 
                && MainComponent.multiplier > 1 
                && MainComponent.renderMode != Mode.AdaptiveResolution;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).intensityFXAA.value = MainComponent.fssaaIntensity;

            // setup shader specs
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).resizeWidth.value = Screen.width;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).resizeHeight.value = Screen.height;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).sharpness.value = Sharpness;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).sampleDistance.value = SampleDistance;
            ((SsaaSamplingUber)PostVolume.profile.settings[0]).flip.value = MainComponent.flipImageFix && GraphicsSettings.renderPipelineAsset.ToString().Contains("(UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipelineAsset)");
#endif
        }

        // Events from main behaviour
        public void OnMainEnable()
        {
            mainComponent = Main.GetComponent<MadGoatSSAA>();
            material_bl = new Material(bilinearshader);
            material_bc = new Material(bicubicshader);
            material_nn = new Material(neighborshader);
            material_def = new Material(defaultshader);
            Material_current = material_def;

            if (MadGoatSSAA_Utils.DetectSRP())
                SetupCBSRP();
            else
                SetupCB();
        }
        public void OnMainDisable()
        {
            if (!MadGoatSSAA_Utils.DetectSRP())
            {
                if ((new List<CommandBuffer>(Current.GetCommandBuffers(CameraEvent.AfterEverything))).Find(x => x.name == "SSAA_COMPOSITION") != null)
                {
                    Current.RemoveCommandBuffer(CameraEvent.AfterEverything, CopyCB);
                }
                copyCB.Clear();
            }
        }

        // Events from main renderer
        public void OnMainRender()
        {
            // Set up camera for hdrp
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && SSAA_HDRP
            if (MadGoatSSAA_Utils.DetectSRP())
            {
                HDAdditionalCameraData data = GetComponent<HDAdditionalCameraData>();
                HDAdditionalCameraData dataMain = Main.GetComponent<HDAdditionalCameraData>();
                if (data == null)
                    data = Current.gameObject.AddComponent<HDAdditionalCameraData>();
                data.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                data.clearDepth = true;
                data.volumeLayerMask = 0;

            }
#endif
            

            // set up command buffers
            if (MadGoatSSAA_Utils.DetectSRP())
                // for SRP
                UpdateCBSRP();
            else
                // for legacy
                UpdateCB();
        }
        public void OnMainRenderEnded()
        {
            // listen for screenshot
            ScreenShootGrab();
        }

        public string GetScreenshotName // generate a string for the filename of the screenshot
        {
            get
            {
                return (MainComponent.useProductName? Application.productName : MainComponent.namePrefix) + "_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmssff") + "_" +
                    MainComponent.screenshotSettings.outputResolution.y.ToString() + "p";
            }
        }
    }
}