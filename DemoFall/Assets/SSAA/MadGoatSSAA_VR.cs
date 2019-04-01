using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MadGoat_SSAA;
using System;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

// Important!!
// When uninstalling post processing stack from the project, make sure 
// that the UNITY_POST_PROCESSING_STACK_V2 directive is removed from
// the player settings. Failing to do so will result in errors.

#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
namespace MadGoat_SSAA
{
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
    [RequireComponent(typeof(PostProcessLayer))]
#endif
    public class MadGoatSSAA_VR : MadGoatSSAA
    {
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

        private bool ssaaUltraOld = false;
        private int resSumOld = 0;
        private int resSumCurrent = 0;

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

#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
        private PostProcessVolume processVolume;
        private GameObject processVolumeObject;
#endif
        private CommandBuffer processBuffer;
        private LayerMask oldPostLayer;
        private RenderTexture buff;
        private RenderTexture buff2;

        protected override void Init()
        {
            if (currentCamera == null)
                currentCamera = GetComponent<Camera>();

#if UNITY_2017_2_OR_NEWER
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = multiplier;
#else
            UnityEngine.VR.VRSettings.renderScale = multiplier;
#endif
            material_bl = new Material(bilinearshader);
            material_bc = new Material(bicubicshader);
            material_nn = new Material(neighborshader);
            material_def = new Material(defaultshader);

            Material_current = material_def;
            Material_old = Material_current;

            if (MadGoatSSAA_Utils.DetectSRP())
                SetupSRPCB();
            else
                SetupStandardCB();
        }
        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
#endif

            if (dbgData == null)
                dbgData = new DebugData(this);

            Init();

            StartCoroutine(AdaptiveTask());
#if UNITY_2019_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            // only on SRP
            if (!MadGoatSSAA_Utils.DetectSRP()) return;
            //UnityEngine.Experimental.Rendering.RenderPipeline.BeginCameraRendering(GetComponent<Camera>());
            UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += OnBeginCameraRender;
#elif UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            // only on SRP
            if (!MadGoatSSAA_Utils.DetectSRP()) return;
            RenderPipeline.BeginCameraRendering(GetComponent<Camera>());
            RenderPipeline.beginCameraRendering += OnBeginCameraRender;
#endif
        }
        private void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
#endif
            FpsData.Update();
            SendDbgInfo();
        }
        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
#endif
            // Handle VR device cleaning
#if UNITY_2017_2_OR_NEWER
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1f;
#else
            UnityEngine.VR.VRSettings.renderScale = 1f;
#endif
            // Handle cleaning
            if (MadGoatSSAA_Utils.DetectSRP())
                ClearSRPCB();
            else
                ClearStandardCB();

            // Handle SRP render event cleaning
#if UNITY_2019_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            // only on SRP
            if (!MadGoatSSAA_Utils.DetectSRP()) return;
            UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering -= OnBeginCameraRender;
#elif UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            // only on SRP
            if (!MadGoatSSAA_Utils.DetectSRP()) return;
            RenderPipeline.beginCameraRendering -= OnBeginCameraRender;
#endif
        }

        protected override void OnBeginCameraRender(Camera cam)
        {
            if (cam != currentCamera || !enabled)
                return;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                return;
#endif

            try
            {

                // Handle the resolution multiplier
#if UNITY_2017_2_OR_NEWER
                if (!UnityEngine.XR.XRDevice.isPresent)
                    throw new Exception("VRDevice not present or not detected");
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = multiplier;
#else
                if (!UnityEngine.VR.VRDevice.isPresent)
                    throw new Exception("VRDevice not present or not detected");
                UnityEngine.VR.VRSettings.renderScale = multiplier;
#endif

                // Change the material by the filter type
                ChangeMaterial(filterType);
                
                // Update the materials properties
                if (MadGoatSSAA_Utils.DetectSRP())
                    UpdateSRPCB();
                else
                    UpdateSdanrdardCB();
            }
            catch (Exception ex)
            {
                Debug.LogError("Something went wrong. SSAA has been set to off and the plugin was disabled");
                Debug.LogError(ex);
                SetAsSSAA(SSAAMode.SSAA_OFF);
                enabled = false;
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
#if UNITY_2017_2_OR_NEWER
            resSumCurrent = UnityEngine.XR.XRSettings.eyeTextureWidth + UnityEngine.XR.XRSettings.eyeTextureWidth;
#else
            resSumCurrent = UnityEngine.VR.VRSettings.eyeTextureWidth + UnityEngine.VR.VRSettings.eyeTextureWidth;
#endif
            // if material must be changed we have to reset the command buffer
            if (Material_current != Material_old || ssaaUltraOld != ssaaUltra || resSumOld != resSumCurrent)
            {
                Debug.Log("a");
                resSumOld = resSumCurrent;
                ssaaUltraOld = ssaaUltra;
                Material_old = Material_current;
                PostVolumePassOld = PostVolumePass;
                
                ClearStandardCB();
                SetupStandardCB();
            }
        }

        private void SetupStandardCB()
        {
            processBuffer = new CommandBuffer();
            if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.AfterImageEffects))).Find(x => x.name == "SSAA_VR_APPLY") == null)
            {
                // set up buffer rt
                if (buff)
                    buff.Release();
                if (buff2)
                    buff2.Release();
#if UNITY_2017_2_OR_NEWER
                buff = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureWidth*2, UnityEngine.XR.XRSettings.eyeTextureHeight, 24, RenderTextureFormat.ARGBHalf);
                buff2 = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureWidth*2, UnityEngine.XR.XRSettings.eyeTextureHeight, 24, RenderTextureFormat.ARGBHalf);

                //buff = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureWidth, UnityEngine.XR.XRSettings.eyeTextureHeight, 24, RenderTextureFormat.ARGBHalf);
                //buff2 = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureWidth, UnityEngine.XR.XRSettings.eyeTextureHeight, 24, RenderTextureFormat.ARGBHalf);
#else
                buff = new RenderTexture(UnityEngine.VR.VRSettings.eyeTextureWidth, UnityEngine.VR.VRSettings.eyeTextureHeight, 24, RenderTextureFormat.ARGBHalf);
                buff2 = new RenderTexture(UnityEngine.VR.VRSettings.eyeTextureWidth, UnityEngine.VR.VRSettings.eyeTextureHeight, 24, RenderTextureFormat.ARGBHalf);
#endif
                RenderTargetIdentifier buffId = new RenderTargetIdentifier(buff);
                RenderTargetIdentifier buff2Id = new RenderTargetIdentifier(buff2);

                // fix for singlepass issue in u2018
#if UNITY_2017_2_OR_NEWER
                buff.vrUsage = VRTextureUsage.TwoEyes;
                buff2.vrUsage = VRTextureUsage.TwoEyes;
#endif
                // Setup Standard CB
                processBuffer.Clear();
                processBuffer.name = "SSAA_VR_APPLY";
                processBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                // Downsampling 
                processBuffer.Blit(BuiltinRenderTextureType.CameraTarget, buffId, Material_current, 0);
                            
                // FSSAA
                processBuffer.Blit(buffId, buff2Id, multiplier > 1 && ssaaUltra && renderMode != Mode.AdaptiveResolution ? FXAA_FSS_Mat : Material_def, 0);

                // Final
                processBuffer.Blit(buff2Id, BuiltinRenderTextureType.CameraTarget);

                // Register cb to camera
                currentCamera.AddCommandBuffer(CameraEvent.AfterImageEffects, processBuffer);
            }
        }
        private void UpdateSdanrdardCB()
        {
#if UNITY_2017_2_OR_NEWER
            Material_current.SetFloat("_ResizeWidth", UnityEngine.XR.XRSettings.eyeTextureWidth);
            Material_current.SetFloat("_ResizeHeight", UnityEngine.XR.XRSettings.eyeTextureHeight);
#else
            Material_current.SetFloat("_ResizeWidth", UnityEngine.VR.VRSettings.eyeTextureWidth);
            Material_current.SetFloat("_ResizeHeight", UnityEngine.VR.VRSettings.eyeTextureHeight);
#endif
            Material_current.SetFloat("_Sharpness", sharpness);
            Material_current.SetFloat("_SampleDistance", sampleDistance);

            FXAA_FSS_Mat.SetVector("_QualitySettings", new Vector3(1.0f, 0.063f, 0.0312f));
            FXAA_FSS_Mat.SetVector("_ConsoleSettings", new Vector4(0.5f, 2.0f, 0.125f, 0.04f));
            FXAA_FSS_Mat.SetFloat("_Intensity", fssaaIntensity);

            //Debug.Log(Material_current.shader.name);
        }
        private void ClearStandardCB()
        {
            if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.AfterImageEffects))).Find(x => x.name == "SSAA_VR_APPLY") != null)
            {
                //RenderTexture.ReleaseTemporary(buff);
                currentCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, processBuffer);
            }
        }

        private void SetupSRPCB()
        {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
         
            // Setup SRP CB
            processVolumeObject = new GameObject("RenderCameraObject");
            processVolumeObject.transform.SetParent(transform);
            processVolumeObject.transform.position = Vector3.zero;
            processVolumeObject.transform.rotation = new Quaternion(0, 0, 0, 0);
            processVolumeObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            processVolume = processVolumeObject.AddComponent<PostProcessVolume>();
            processVolume.sharedProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
            processVolume.sharedProfile.AddSettings<SsaaVRSamplingUber>();
            processVolume.isGlobal = true;

            // get the first empty render layer
#if UNITY_EDITOR
            MadGoatSSAA_Utils.GrabRenderLayer();
#endif
            oldPostLayer = GetComponent<PostProcessLayer>().volumeLayer;
            GetComponent<PostProcessLayer>().volumeLayer |= 1 << LayerMask.NameToLayer("SSAA_RENDER");
            processVolumeObject.layer = LayerMask.NameToLayer("SSAA_RENDER");
#endif
        }
        private void UpdateSRPCB()
        {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).resizeWidth.value = Screen.width;
            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).resizeHeight.value = Screen.width;
            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).sharpness.value = sharpness;
            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).sampleDistance.value = sampleDistance;

            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).useFXAA.value = ssaaUltra;
            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).intensityFXAA.value = fssaaIntensity;

            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).shaderPass.value = PostVolumePass;
            ((SsaaVRSamplingUber)processVolume.profile.settings[0]).flip.value = false;
#endif
        }
        private void ClearSRPCB()
        {
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            Destroy(processVolume);
            GetComponent<PostProcessLayer>().volumeLayer = oldPostLayer;
#endif
        }


        /// <summary>
        /// Set the multiplier of each screen axis independently. does not use downsampling filter.
        /// </summary>
        public override void SetAsAxisBased(float MultiplierX, float MultiplierY)
        {
            Debug.LogWarning("NOT SUPPORTED IN VR MODE.\nX axis will be used as global multiplier instead.");
            base.SetAsAxisBased(MultiplierX, MultiplierY);
        }
        /// <summary>
        ///  Set the multiplier of each screen axis independently while using the downsampling filter.
        /// </summary>
        public override void SetAsAxisBased(float MultiplierX, float MultiplierY, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            Debug.LogWarning("NOT SUPPORTED IN VR MODE.\nX axis will be used as global multiplier instead.");
            base.SetAsAxisBased(MultiplierX, MultiplierY, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Returns a ray from a given screenpoint
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Ray ScreenPointToRay(Vector3 position)
        {
            return currentCamera.ScreenPointToRay(position);
        }
        /// <summary>
        /// Transforms position from screen space into world space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 ScreenToWorldPoint(Vector3 position)
        {
            return currentCamera.ScreenToWorldPoint(position);
        }
        /// <summary>
        /// Transforms postion from screen space into viewport space.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 ScreenToViewportPoint(Vector3 position)
        {
            return currentCamera.ScreenToViewportPoint(position);
        }
        /// <summary>
        /// Transforms position from world space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 WorldToScreenPoint(Vector3 position)
        {
            return currentCamera.WorldToScreenPoint(position);
        }
        /// <summary>
        /// Transforms position from viewport space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public override Vector3 ViewportToScreenPoint(Vector3 position)
        {
            return currentCamera.ViewportToScreenPoint(position);
        }


        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier. The screenshot is saved at the given path in PNG format.
        /// </summary>
        public override void TakeScreenshot(string path, Vector2 Size, int multiplier)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier and use the bicubic downsampler. The screenshot is saved at the given path in PNG format. 
        /// </summary>
        public override void TakeScreenshot(string path, Vector2 Size, int multiplier, float sharpness)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public override void TakePanorama(string path, int size)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" supersampled by "multiplier"
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public override void TakePanorama(string path, int size, int multiplier)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" using downsampling shader
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public override void TakePanorama(string path, int size, int multiplier, float sharpness)
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Sets up the screenshot module to use the PNG image format. This enables transparency in output images.
        /// </summary>
        public override void SetScreenshotModuleToPNG()
        {
            Debug.LogWarning("Not available in VR mode");
        }
        /// <summary>
        /// Sets up the screenshot module to use the JPG image format. Quality is parameter from 1 to 100 and represents the compression quality of the JPG file. Incorrect quality values will be clamped.
        /// </summary>
        /// <param name="quality"></param>
        public override void SetScreenshotModuleToJPG(int quality)
        {
            Debug.LogWarning("Not available in VR mode");
        }
#if UNITY_5_6_OR_NEWER
        /// <summary>
        /// Sets up the screenshot module to use the EXR image format. The EXR32 bool parameter dictates whether to use or not 32 bit exr encoding. This method is only available in Unity 5.6 and newer.
        /// </summary>
        /// <param name="EXR32"></param>
        public override void SetScreenshotModuleToEXR(bool EXR32)
        {
            Debug.LogWarning("Not available in VR mode");
        }
#endif
    }
}