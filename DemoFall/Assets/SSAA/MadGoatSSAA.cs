using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

// Important!!
// When uninstalling post processing stack from the project, make sure 
// that the UNITY_POST_PROCESSING_STACK_V2 directive is removed from
// the player settings. Failing to do so will result in errors.

#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.Rendering;
#endif

namespace MadGoat_SSAA
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class MadGoatSSAA : MonoBehaviour
    {
        // Renderer
        public Mode renderMode = Mode.SSAA;
        public float multiplier = 1f;
        public float multiplierVertical = 1f;
        public bool fssaaAlpha;
        // SSAA 
        public SsaaProfile SSAA_X2 = new SsaaProfile(1.5f, true, Filter.BILINEAR, .8f, .5f);
        public SsaaProfile SSAA_X4 = new SsaaProfile(2f, true, Filter.BICUBIC, .725f, .95f);
        public SsaaProfile SSAA_HALF = new SsaaProfile(.5f, true, Filter.NEAREST_NEIGHBOR, 0, 0);
        public SSAAMode ssaaMode = SSAAMode.SSAA_OFF;
        public bool ssaaUltra=false;
        [Range(0, 1)]
        public float fssaaIntensity = 1;

        public RenderTextureFormat textureFormat = RenderTextureFormat.ARGBHalf;

        // Downsampler
        public bool useShader = true;
        public Filter filterType = Filter.BILINEAR;
        public float sharpness = 0.8f;
        public float sampleDistance = 1f;

        // Adaptive Resolution
        public bool useVsyncTarget = false;
        public int targetFramerate = 60;
        public float minMultiplier = 0.5f;
        public float maxMultiplier = 1.5f;

        // Screenshots
        public string screenshotPath = "Assets/SuperSampledSceenshots/";
        public string namePrefix = "SSAA";
        public bool useProductName = false;
        public ImageFormat imageFormat;
        [Range(0,100)]
        public int JPGQuality = 90;
        public bool EXR32 = false;

        // FSSAA
        private Shader _FXAA_FSS;
        protected Shader FXAA_FSS
        {
            get
            {
                if (_FXAA_FSS == null)
                    _FXAA_FSS = Shader.Find("Hidden/SSAA/FSS");

                return _FXAA_FSS;
            }
        }
        private Material _FXAA_FSS_Mat; // Default
        protected Material FXAA_FSS_Mat
        {
            get
            {
                if (_FXAA_FSS_Mat == null)
                    _FXAA_FSS_Mat = new Material(FXAA_FSS);

                return _FXAA_FSS_Mat;
            }
        }

        private CommandBuffer fssCb;
        protected CommandBuffer FssCb
        {
            get
            {
                return fssCb;
            }

            set
            {
                fssCb = value;
            }
        }

        private RenderTexture fxaaFlip;
        protected RenderTexture FxaaFlip
        {
            get
            {
                return fxaaFlip;
            }

            set
            {
                fxaaFlip = value;
            }
        }

        private RenderTexture fxaaFlop;
        protected RenderTexture FxaaFlop
        {
            get
            {
                return fxaaFlop;
            }

            set
            {
                fxaaFlop = value;
            }
        }


        private Shader _grabAlpha;
        protected Shader GrabAlpha
        {
            get
            {
                if (_grabAlpha == null)
                    _grabAlpha = Shader.Find("Hidden/SSAA_Alpha");

                return _grabAlpha;
            }
        }
        private Material _grabAlphaMat; // Default
        protected Material GrabAlphaMat
        {
            get
            {
                if (_grabAlphaMat == null)
                    _grabAlphaMat = new Material(GrabAlpha);

                return _grabAlphaMat;
            }
        }

        private CommandBuffer grabAlphaCB;
        protected CommandBuffer GrabAlphaCB
        {
            get
            {
                return grabAlphaCB;
            }

            set
            {
                grabAlphaCB = value;
            }
        }
        private CommandBuffer pasteAlphaCB;
        protected CommandBuffer PasteAlphaCB
        {
            get
            {
                return pasteAlphaCB;
            }

            set
            {
                pasteAlphaCB = value;
            }
        }

        public RenderTexture grabAlphaRT;
        public RenderTexture pasteAlphaRT;

        // Misc
        [SerializeField]
        protected Camera currentCamera;
        protected Camera renderCamera;
        protected GameObject renderCameraObject;
        protected MadGoatSSAA_InternalRenderer SSAA_Internal;
        private Rect tempRect;
        
        private Texture2D _sphereTemp;
        private Texture2D sphereTemp
        {
            get
            {
                if (_sphereTemp != null)
                    return _sphereTemp;
                _sphereTemp = new Texture2D(2, 2);
                return _sphereTemp;
            }
        }

        protected FramerateSampler FpsData = new FramerateSampler();
        public DebugData dbgData;

        // Misc settings
        public bool mouseCompatibilityMode = false;
        public bool exposeInternalRender = false;
        public bool flipImageFix = false;
        public RenderTexture targetTexture;
        public GameObject madGoatDebugger;

        // Screenshot Module
        public ScreenshotSettings screenshotSettings = new ScreenshotSettings();
        public PanoramaSettings panoramaSettings = new PanoramaSettings(1024,1);


#region MonoBehaviour Events Methods
        private void OnEnable()
        {
            if (dbgData == null)
                dbgData = new DebugData(this);

            currentCamera = GetComponent<Camera>();

            Init();
            StartCoroutine(AdaptiveTask());
            StartCoroutine(OnEndCameraRender());

#if UNITY_2019_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
            if (MadGoatSSAA_Utils.DetectSRP())
            {
                //UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering(GetComponent<Camera>());
                UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += OnBeginCameraRender;
            }
#elif UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
            if (MadGoatSSAA_Utils.DetectSRP())
            {
                RenderPipeline.BeginCameraRendering(GetComponent<Camera>());
                RenderPipeline.beginCameraRendering += OnBeginCameraRender;
            }
#endif
            SSAA_Internal.OnMainEnable();
        }
        private void Update()
        {
            // in case 3rd party systems tried to remove the target texture
            if(!currentCamera.targetTexture)
            {
                Debug.LogWarning("Something went wrong with the target texture. Restarting SSAA...");
                Refresh();
                return;
            }

            currentCamera.targetTexture.filterMode = (filterType == Filter.NEAREST_NEIGHBOR && useShader) ? FilterMode.Point : FilterMode.Trilinear;

            renderCameraObject.hideFlags = exposeInternalRender ? HideFlags.None : HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            renderCamera.enabled = currentCamera.enabled;
            int layer = renderCamera.gameObject.layer;
            renderCamera.CopyFrom(currentCamera, null);
          
            // Nothing is drawn on output camera, so the performance hit is minimal, we only need it to output the render (Graphics.Blit)
            renderCamera.cullingMask = mouseCompatibilityMode? -1 : 0;
            renderCamera.clearFlags = currentCamera.clearFlags;
            
            renderCamera.targetTexture = targetTexture;
            renderCamera.depth = currentCamera.depth - 1;
            renderCamera.gameObject.layer = layer;

            // Set render settings
            SSAA_Internal.Multiplier = multiplier;
            SSAA_Internal.Sharpness = sharpness;
            SSAA_Internal.UseShader = useShader;
            SSAA_Internal.SampleDistance = sampleDistance;

            SSAA_Internal.ChangeMaterial(filterType);
            FpsData.Update();
            SendDbgInfo();
        }
        private void OnDisable()
        {
            SSAA_Internal.OnMainDisable();
            SSAA_Internal.enabled = false;

            currentCamera.targetTexture.Release();
            currentCamera.targetTexture = null;

            // DestroyImmediate doesn't work well with the new prefab system.
            //DestroyImmediate(renderCameraObject.gameObject);
            renderCamera.enabled = false;

#if UNITY_2019_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            // only on SRP
            if (MadGoatSSAA_Utils.DetectSRP())
            {
                UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering -= OnBeginCameraRender;
            }
#elif UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2  && (SSAA_HDRP || SSAA_LWRP)
            // only on SRP
            if (MadGoatSSAA_Utils.DetectSRP())
            {
                RenderPipeline.beginCameraRendering -= OnBeginCameraRender;
            }
#endif
            if (!MadGoatSSAA_Utils.DetectSRP())
            {
                // remove the command buffer
                if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_FSS") != null)
                {
                    
                    currentCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, (new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_FSS"));
                    FssCb.Clear();
                }

                // remove the command buffer
                if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_Grab_Alpha") != null)
                {

                    currentCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, (new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_Grab_Alpha"));
                    GrabAlphaCB.Clear();
                    currentCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, (new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.AfterEverything))).Find(x => x.name == "SSAA_Apply_Alpha"));
                    PasteAlphaCB.Clear();
                }
            }
        }
#endregion

#region Render Events Methods
        private void OnPreRender() // for standard pipeline
        {
            OnBeginCameraRender(currentCamera);
        }
        protected virtual void OnBeginCameraRender(Camera cam) // for scriptable pipelines
        {
            if (cam != currentCamera || !enabled)
                return;

            // Setup the aspect ratio
            currentCamera.aspect = (Screen.width * currentCamera.rect.width) / (Screen.height * currentCamera.rect.height);
            // Cache current camera rect and set it to fullscreen
            // Render Texture doesn't seem to like incomplete camera renders for some reason.
            //tempRect = currentCamera.rect;
            //currentCamera.rect = new Rect(0, 0, 1, 1);
            // If a screenshot is queued 
            if (screenshotSettings.takeScreenshot)
            {   // Setup for the screenshot and stop there.
                SetupScreenshotRender(screenshotSettings.screenshotMultiplier, false);
                return;
            }

            if (Screen.width * multiplier != currentCamera.targetTexture.width || Screen.height * (renderMode == Mode.PerAxisScale ? multiplierVertical : multiplier) != currentCamera.targetTexture.height)
            {
                SetupRender();
                // reset the alpha
                // remove the command buffer
                if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_Grab_Alpha") != null)
                {

                    currentCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, (new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_Grab_Alpha"));
                    GrabAlphaCB.Clear();
                    currentCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, (new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.AfterEverything))).Find(x => x.name == "SSAA_Apply_Alpha"));
                    PasteAlphaCB.Clear();
                }
            }
            currentCamera.targetTexture.Release();
            currentCamera.targetTexture.Create();

            // setup alpha
            DoAlphaCommandBuffer();

            // setup fssaa
            if(multiplier > 1 && ssaaUltra && renderMode!=Mode.AdaptiveResolution && !MadGoatSSAA_Utils.DetectSRP())
            {
                DoFSSCommandBuffer();
            }
            else if(!MadGoatSSAA_Utils.DetectSRP())
            {
                // remove the command buffer
                if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_FSS") != null)
                {
                    currentCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, FssCb);
                    FssCb.Clear();
                }
            }

            SSAA_Internal.OnMainRender();
        }
        protected virtual IEnumerator OnEndCameraRender()
        {
            yield return new WaitForEndOfFrame();

            //currentCamera.rect = tempRect;
            SSAA_Internal.OnMainRenderEnded();

            if (enabled)
                StartCoroutine(OnEndCameraRender());
        }
#endregion

#region MadGoat SSAA Private API
        /// <summary>
        /// Initialize the SSAA system
        /// </summary>
        protected virtual void Init()
        {

            if (renderCameraObject == null)
            {
                if (GetComponentInChildren<MadGoatSSAA_InternalRenderer>())
                {
                    SSAA_Internal = GetComponentInChildren<MadGoatSSAA_InternalRenderer>();
                    renderCameraObject = SSAA_Internal.gameObject;
                    renderCamera = renderCameraObject.GetComponent<Camera>();
                }
                else
                {
                    //Setup new high resolution camera
                    renderCameraObject = new GameObject("RenderCameraObject");
                    renderCameraObject.transform.SetParent(transform);
                    renderCameraObject.transform.position = Vector3.zero;
                    renderCameraObject.transform.rotation = new Quaternion(0, 0, 0, 0);

                    // Setup components of new camera
                    renderCamera = renderCameraObject.AddComponent<Camera>();
                    SSAA_Internal = renderCameraObject.AddComponent<MadGoatSSAA_InternalRenderer>();
                }
                renderCameraObject.hideFlags = exposeInternalRender ? HideFlags.None : HideFlags.HideInHierarchy | HideFlags.HideInInspector;

                SSAA_Internal.Current = renderCamera;
                SSAA_Internal.Main = currentCamera;
                SSAA_Internal.enabled = true;

                // Copy settings from current camera
                renderCamera.CopyFrom(currentCamera);

                // Disable rendering on internal cam.
                // Nothing is drawn on main camera, performance hit is minimal
                renderCamera.cullingMask = 0;
                renderCamera.clearFlags = CameraClearFlags.Nothing;

                renderCamera.enabled = true;
            }
            else
            {
                SSAA_Internal.enabled = true;
                renderCamera.enabled = true;
            }

            currentCamera.targetTexture = new RenderTexture(1024, 1024, 24, textureFormat);
            currentCamera.targetTexture.Create();

            if (!MadGoatSSAA_Utils.DetectSRP())
            {
                FssCb = new CommandBuffer();
                GrabAlphaCB = new CommandBuffer();
                PasteAlphaCB = new CommandBuffer();
            }
        }
        /// <summary>
        /// Send the SSAA information to MadGoat Debugger
        /// </summary>
        protected void SendDbgInfo()
        {
            if (!Application.isPlaying|| !madGoatDebugger)
                return;

            string message = "SSAA: Render Res:"+ GetResolution()+ " [x"+ dbgData.multiplier + "] [FSSAA:" +dbgData.fssaa+ "] [Mode: "+dbgData.renderMode+"]";
            madGoatDebugger.SendMessage("SsaaListener", message);
        }

        /// <summary>
        /// Setup if the case, and update the FSSAA command buffer (In standard pipeline)
        /// </summary>
        /// 
        private void DoFSSCommandBuffer()
        {
            if (MadGoatSSAA_Utils.DetectSRP())
                return;

            FXAA_FSS_Mat.SetVector("_QualitySettings", new Vector3(1.0f, 0.063f, 0.0312f));
            FXAA_FSS_Mat.SetVector("_ConsoleSettings", new Vector4(0.5f, 2.0f, 0.125f, 0.04f));
            FXAA_FSS_Mat.SetFloat("_Intensity", fssaaIntensity);

            // For command buffers
            if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_FSS") == null)
            {
                FssCb.name = "SSAA_FSS";
                FssCb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                if(FxaaFlip)
                    FxaaFlip.Release();
                if(FxaaFlop)
                    FxaaFlop.Release();

                FxaaFlip = new RenderTexture(currentCamera.targetTexture.width, currentCamera.targetTexture.height, 1, RenderTextureFormat.ARGBHalf);
                FxaaFlop = new RenderTexture(currentCamera.targetTexture.width, currentCamera.targetTexture.height, 1, RenderTextureFormat.ARGBHalf);

                RenderTargetIdentifier idFxaaFlip = new RenderTargetIdentifier(FxaaFlip);
                RenderTargetIdentifier idFxaaFlop = new RenderTargetIdentifier(FxaaFlop);

                FssCb.Blit(BuiltinRenderTextureType.CameraTarget, idFxaaFlip);
                FssCb.Blit(idFxaaFlip, idFxaaFlop, _FXAA_FSS_Mat, 0);
                FssCb.Blit(idFxaaFlop, BuiltinRenderTextureType.CameraTarget);
                currentCamera.AddCommandBuffer(CameraEvent.BeforeImageEffects, FssCb);

            }
        }
        private void DoAlphaCommandBuffer()
        {
            if (MadGoatSSAA_Utils.DetectSRP())
                return;
            
            // For command buffers
            if ((new List<CommandBuffer>(currentCamera.GetCommandBuffers(CameraEvent.BeforeImageEffects))).Find(x => x.name == "SSAA_Grab_Alpha") == null)
            {
                GrabAlphaCB.name = "SSAA_Grab_Alpha";
                PasteAlphaCB.name = "SSAA_Apply_Alpha";
                GrabAlphaCB.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                PasteAlphaCB.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                if (grabAlphaRT)
                    grabAlphaRT.Release();

                grabAlphaRT = new RenderTexture(currentCamera.targetTexture.width, currentCamera.targetTexture.height, 1, RenderTextureFormat.ARGBHalf);
                
                RenderTargetIdentifier idGrab = new RenderTargetIdentifier(grabAlphaRT);
                GrabAlphaCB.Blit(BuiltinRenderTextureType.CameraTarget, idGrab,GrabAlphaMat,0);


                if (pasteAlphaRT)
                    pasteAlphaRT.Release();

                pasteAlphaRT = new RenderTexture(currentCamera.targetTexture.width, currentCamera.targetTexture.height, 1, RenderTextureFormat.ARGBHalf);

                RenderTargetIdentifier idPaste = new RenderTargetIdentifier(pasteAlphaRT);

                PasteAlphaCB.SetGlobalTexture("_MainTexA", idGrab);

                PasteAlphaCB.Blit(BuiltinRenderTextureType.CameraTarget, idPaste);
                PasteAlphaCB.Blit(idPaste, BuiltinRenderTextureType.CameraTarget, GrabAlphaMat, 1);

                currentCamera.AddCommandBuffer(CameraEvent.BeforeImageEffects, GrabAlphaCB);
                currentCamera.AddCommandBuffer(CameraEvent.AfterEverything, PasteAlphaCB);
                
            }
        }
        /// <summary>
        /// Renders a 360 panorama image and save to disk
        /// </summary>
        private void RenderPanorama()
        {
            // init
            enabled = false;
            int internalRes = panoramaSettings.panoramaSize * panoramaSettings.panoramaMultiplier;
            Cubemap resultCube = new Cubemap(internalRes, TextureFormat.ARGB32, false);
           
            RenderTexture buffer = RenderTexture.GetTemporary(panoramaSettings.panoramaSize, panoramaSettings.panoramaSize,24,RenderTextureFormat.ARGB32);

            // reset the render camera
            renderCamera.CopyFrom(currentCamera, null);
            SSAA_Internal.enabled = false;
            currentCamera.RenderToCubemap(resultCube);

            string folderPath = screenshotPath + "\\" + getName + "\\";
            (new FileInfo(folderPath)).Directory.Create();

            for (int i = 0; i < 6; i++)
            {
                sphereTemp.Resize(internalRes, internalRes);
                sphereTemp.SetPixels(Rotate90(Rotate90(resultCube.GetPixels((CubemapFace)i), internalRes),internalRes));
                sphereTemp.Apply();
               
                // no processing needs to be done if no supersampling
                if (panoramaSettings.panoramaMultiplier == 1)
                {

                    if (imageFormat == ImageFormat.PNG)
                        File.WriteAllBytes(folderPath+"Face_" + (CubemapFace)i + ".png", sphereTemp.EncodeToPNG());
                    else if (imageFormat == ImageFormat.JPG)
                        File.WriteAllBytes(folderPath + "Face_" + (CubemapFace)i + ".jpg", sphereTemp.EncodeToJPG(JPGQuality));
#if UNITY_5_6_OR_NEWER
                    else
                        File.WriteAllBytes(folderPath + "Face_" + (CubemapFace)i + ".exr", sphereTemp.EncodeToEXR(EXR32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
#endif
                    continue;
                }

                bool sRGBWrite = GL.sRGBWrite;
                // enable srgb conversion for blit - fixes the color issue
                GL.sRGBWrite = true;

                if (!panoramaSettings.useFilter)
                {
                    Graphics.Blit(sphereTemp, buffer);
                }
                else
                {
                    SSAA_Internal.Material_bc.SetFloat("_ResizeWidth", internalRes);
                    SSAA_Internal.Material_bc.SetFloat("_ResizeHeight", internalRes);

                    SSAA_Internal.Material_bc.SetFloat("_Sharpness", panoramaSettings.sharpness);
                    Graphics.Blit(sphereTemp, buffer, SSAA_Internal.Material_bc, 0);
                }
                RenderTexture.active = buffer;
                
                Texture2D screenshotBuffer = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.ARGB32, true, true);
                screenshotBuffer.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);

                if (imageFormat == ImageFormat.PNG)
                    File.WriteAllBytes(folderPath + "\\Face_" + (CubemapFace)i + ".png", screenshotBuffer.EncodeToPNG());
                else if (imageFormat == ImageFormat.JPG)
                    File.WriteAllBytes(folderPath + "\\Face_" + (CubemapFace)i + ".jpg", screenshotBuffer.EncodeToJPG(JPGQuality));
#if UNITY_5_6_OR_NEWER
                else
                    File.WriteAllBytes(folderPath + "\\Face_" + (CubemapFace)i + ".exr", screenshotBuffer.EncodeToEXR(EXR32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
#endif
                // restore the sRGBWrite to older state so it doesn't interfere with user's setting
                GL.sRGBWrite = sRGBWrite;
            }

            // Clean some allocated memory
            sphereTemp.Resize(2, 2);
            sphereTemp.Apply();
            RenderTexture.ReleaseTemporary(buffer);
            // SSAA can render again
            SSAA_Internal.enabled = true;
            enabled = true;
        }
        /// <summary>
        /// Setup the multiplier in adaptive mode
        /// </summary>
        /// <param name="fps"></param>
        private void SetupAdaptive(int fps)
        {
            int compFramerate = useVsyncTarget ? Screen.currentResolution.refreshRate : targetFramerate;
            if (fps < compFramerate - 5)
            {
                multiplier = Mathf.Clamp(multiplier - 0.1f, minMultiplier, maxMultiplier);
            }
            else if(fps> compFramerate + 10)
            {
                multiplier = Mathf.Clamp(multiplier + 0.1f, minMultiplier, maxMultiplier);
            }
        }
        /// <summary>
        /// Setup for SSAA renderer
        /// </summary>
        private void SetupRender()
        {
            try
            {
                currentCamera.targetTexture.Release();
                currentCamera.targetTexture.width = (int)(Screen.width * multiplier);
                currentCamera.targetTexture.height = (int)(Screen.height * (renderMode == Mode.PerAxisScale ? multiplierVertical : multiplier));
                currentCamera.targetTexture.Create();

            }
            catch (Exception ex)
            {
                Debug.LogError("Something went wrong. SSAA has been set to off");
                Debug.LogError(ex);
                SetAsSSAA(SSAAMode.SSAA_OFF);
            }
        }
        /// <summary>
        /// Setup for ScreenShot Render
        /// </summary>
        /// <param name="mul"></param>
        /// <param name="compatibilityMode"></param>
        private void SetupScreenshotRender(float mul, bool compatibilityMode)
        {
            try
            {
                // If taking a screenshot, the aspect ratio should be given by the screenshot resolution, not the screenres.
                currentCamera.aspect = screenshotSettings.outputResolution.x / screenshotSettings.outputResolution.y;

                currentCamera.targetTexture.Release();
                currentCamera.targetTexture.width = (int)(screenshotSettings.outputResolution.x * mul);
                currentCamera.targetTexture.height = (int)(screenshotSettings.outputResolution.y * mul);
                currentCamera.targetTexture.Create();
            }
            catch (Exception ex) { Debug.LogError(ex.ToString()); }
        }
        /// <summary>
        /// The adaptive mode coroutine
        /// </summary>
        /// <returns></returns>
        protected IEnumerator AdaptiveTask()
        {
            yield return new WaitForSeconds(2);
            if(renderMode == Mode.AdaptiveResolution)
                SetupAdaptive(FpsData.CurrentFps);

            if(enabled)
                StartCoroutine(AdaptiveTask());
        }

        /// <summary>
        /// Used to rotate the 360 panorama images
        /// </summary>
        private Color[] Rotate90(Color[] source, int n)
        {
            Color[] result = new Color[n * n];

            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    result[i * n + j] = source[(n - j - 1) * n + i];
                }
            }
            return result;
        }
        /// <summary>
        /// generate a string for the filename of the screenshot
        /// </summary>
        private string getName 
        {
            get
            {
                return (useProductName ? Application.productName : namePrefix )+ "_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmssff")+ "_" +
                    panoramaSettings.panoramaSize.ToString() + "p";
            }
        }

#endregion

#region MadGoat SSAA Public API
        /// <summary>
        /// Reinitialize the whole SSAA system.
        /// </summary>
        public void Refresh()
        {
            this.enabled = false;
            this.enabled = true;
            currentCamera.rect = new Rect(0, 0, 1, 1);
        }

        /// <summary>
        /// Set rendering mode to given SSAA mode
        /// </summary>
        public void SetAsSSAA(SSAAMode mode)
        {
            renderMode = Mode.SSAA;
            ssaaMode = mode;
            switch (mode)
            {
                case SSAAMode.SSAA_OFF:
                    multiplier = 1f;
                    useShader = false;
                    break;
                case SSAAMode.SSAA_HALF:
                    multiplier = SSAA_HALF.multiplier;
                    useShader = SSAA_HALF.useFilter;
                    sharpness = SSAA_HALF.sharpness;
                    filterType = SSAA_HALF.filterType;
                    sampleDistance = SSAA_HALF.sampleDistance;
                    break;
                case SSAAMode.SSAA_X2:
                    multiplier = SSAA_X2.multiplier;
                    useShader = SSAA_X2.useFilter;
                    sharpness = SSAA_X2.sharpness;
                    filterType = SSAA_X2.filterType;
                    sampleDistance = SSAA_X2.sampleDistance;
                    break;
                case SSAAMode.SSAA_X4:
                    multiplier = SSAA_X4.multiplier;
                    useShader = SSAA_X4.useFilter;
                    sharpness = SSAA_X4.sharpness;
                    filterType = SSAA_X4.filterType;
                    sampleDistance = SSAA_X4.sampleDistance;
                    break;
            }
        }

        /// <summary>
        /// Set the resolution scale to a given percent
        /// </summary>
        public void SetAsScale(int percent)
        {
            // check for invalid values
            percent = Mathf.Clamp(percent, 50, 200);

            renderMode = Mode.ResolutionScale;
            multiplier = percent / 100f;

            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the resolution scale to a given percent, and use custom downsampler settings
        /// </summary>
        public void SetAsScale(int percent, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            percent = Mathf.Clamp(percent, 50, 200);

            renderMode = Mode.ResolutionScale;
            multiplier = percent / 100f;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the operation mode as adaptive with target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            this.targetFramerate = targetFramerate;
            useVsyncTarget = false;
            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            useVsyncTarget = true;
            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the operation mode as adaptive with target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            this.targetFramerate = targetFramerate;
            useVsyncTarget = false;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public void SetAsAdaptive(float minMultiplier, float maxMultiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (minMultiplier < 0.1f) minMultiplier = 0.1f;
            if (maxMultiplier < minMultiplier) maxMultiplier = minMultiplier + 0.1f;

            this.minMultiplier = minMultiplier;
            this.maxMultiplier = maxMultiplier;
            useVsyncTarget = true;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }
        
        /// <summary>
        /// Set a custom resolution multiplier
        /// </summary>
        public void SetAsCustom(float Multiplier)
        {
            // check for invalid values
            if (Multiplier < 0.1f) Multiplier = 0.1f;

            renderMode = Mode.Custom;
            multiplier = Multiplier;

            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set a custom resolution multiplier, and use custom downsampler settings
        /// </summary>
        public void SetAsCustom(float Multiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (Multiplier < 0.1f) Multiplier = 0.1f;

            renderMode = Mode.Custom;
            multiplier = Multiplier;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the multiplier of each screen axis independently. does not use downsampling filter.
        /// </summary>
        /// <param name="MultiplierX"></param>
        /// <param name="MultiplierY"></param>
        public virtual void SetAsAxisBased(float MultiplierX, float MultiplierY)
        {
            // check for invalid values
            if (MultiplierX < 0.1f) MultiplierX = 0.1f;
            if (MultiplierY < 0.1f) MultiplierY = 0.1f;

            renderMode = Mode.PerAxisScale;
            multiplier = MultiplierX;
            multiplierVertical = MultiplierY;

            SetDownsamplingSettings(false);
        }
        /// <summary>
        /// Set the multiplier of each screen axis independently while using the downsampling filter.
        /// </summary>
        /// <param name="MultiplierX"></param>
        /// <param name="MultiplierY"></param>
        public virtual void SetAsAxisBased(float MultiplierX, float MultiplierY, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            // check for invalid values
            if (MultiplierX < 0.1f) MultiplierX = 0.1f;
            if (MultiplierY < 0.1f) MultiplierY = 0.1f;

            renderMode = Mode.PerAxisScale;
            multiplier = MultiplierX;
            multiplierVertical = MultiplierY;

            SetDownsamplingSettings(FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public void SetDownsamplingSettings(bool use)
        {
            useShader = use;
            filterType = use ? Filter.BILINEAR : Filter.NEAREST_NEIGHBOR;
            sharpness = use ? 0.85f : 0; // 0.85 should work fine for any resolution 
            sampleDistance = use ? 0.9f : 0; // 0.9 should work fine for any res
        }
        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public void SetDownsamplingSettings(Filter FilterType, float sharpnessfactor, float sampledist)
        {
            useShader = true;
            filterType = FilterType;
            sharpness = Mathf.Clamp(sharpnessfactor, 0, 1);
            sampleDistance = Mathf.Clamp(sampledist, 0.5f, 1.5f);
        }

        /// <summary>
        /// Enable or disable the ultra mode for super sampling.(FSS)
        /// </summary>
        /// <param name="enabled"></param>
        public void SetUltra(bool enabled)
        {
            ssaaUltra = enabled;
        }
        /// <summary>
        /// Set the intensity of the SSAA ultra effect (FSSAA intensity)
        /// </summary>
        /// <param name="intensity"></param>
        public void SetUltraIntensity(float intensity)
        {
            fssaaIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier. The screenshot is saved at the given path in PNG format.
        /// </summary>
        public virtual void TakeScreenshot(string path, Vector2 Size, int multiplier)
        {
            // Take screenshot with default settings
            screenshotSettings.takeScreenshot = true;
            screenshotSettings.outputResolution = Size;
            screenshotSettings.screenshotMultiplier = multiplier;
            screenshotPath = path;
            
            screenshotSettings.useFilter = false;
        }
        /// <summary>
        /// Take a screenshot of resolution Size (x is width, y is height) rendered at a higher resolution given by the multiplier and use the bicubic downsampler. The screenshot is saved at the given path in PNG format. 
        /// </summary>
        public virtual void TakeScreenshot(string path, Vector2 Size, int multiplier, float sharpness)
        {
            // Take screenshot with custom settings
            screenshotSettings.takeScreenshot = true;
            screenshotSettings.outputResolution = Size;
            screenshotSettings.screenshotMultiplier = multiplier;
            screenshotPath = path;
            screenshotSettings.useFilter = true;
            screenshotSettings.sharpness = Mathf.Clamp(sharpness, 0, 1);
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public virtual void TakePanorama(string path, int size)
        {
            panoramaSettings.useFilter = false;
            panoramaSettings.panoramaSize = size;
            panoramaSettings.panoramaMultiplier = 1;

            screenshotPath = path;
            RenderPanorama();
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" supersampled by "multiplier"
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public virtual void TakePanorama(string path, int size, int multiplier)
        {
            panoramaSettings.useFilter = false;
            panoramaSettings.panoramaSize = size;
            panoramaSettings.panoramaMultiplier = multiplier;
            
            screenshotPath = path;
            RenderPanorama();
        }
        /// <summary>
        /// Take a panorama screenshot of resolution "size"x"size" using downsampling shader
        /// </summary>
        /// <param name="path"></param>
        /// <param name="size"></param>
        public virtual void TakePanorama(string path, int size, int multiplier, float sharpness)
        {
            panoramaSettings.useFilter = true;
            panoramaSettings.panoramaSize = size;
            panoramaSettings.panoramaMultiplier = multiplier;
            panoramaSettings.sharpness = sharpness;

            screenshotPath = path;
            RenderPanorama();
        }

        /// <summary>
        /// Sets up the screenshot module to use the PNG image format. This enables transparency in output images.
        /// </summary>
        public virtual void SetScreenshotModuleToPNG()
        {
            this.imageFormat = ImageFormat.PNG;
        }
        /// <summary>
        /// Sets up the screenshot module to use the JPG image format. Quality is parameter from 1 to 100 and represents the compression quality of the JPG file. Incorrect quality values will be clamped.
        /// </summary>
        /// <param name="quality"></param>
        public virtual void SetScreenshotModuleToJPG(int quality)
        {
            this.imageFormat = ImageFormat.JPG;
            this.JPGQuality = Mathf.Clamp(1,100,quality);
        }
#if UNITY_5_6_OR_NEWER
        /// <summary>
        /// Sets up the screenshot module to use the EXR image format. The EXR32 bool parameter dictates whether to use or not 32 bit exr encoding. This method is only available in Unity 5.6 and newer.
        /// </summary>
        /// <param name="EXR32"></param>
        public virtual void SetScreenshotModuleToEXR(bool EXR32)
        {
            this.imageFormat = ImageFormat.EXR;
            this.EXR32 = EXR32;
        }
#endif

        /// <summary>
        /// Return string with current internal resolution
        /// </summary>
        /// <returns></returns>
        public virtual string GetResolution()
        {
            return (int)(Screen.width * multiplier) + "x" + (int)(Screen.height * multiplier);
        }

        // Global api
        /// <summary>
        /// Set rendering mode to given SSAA mode
        /// </summary>
        public static void SetAllAsSSAA(SSAAMode mode)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsSSAA(mode);
        }

        /// <summary>
        /// Set the resolution scale to a given percent
        /// </summary>
        public static void SetAllAsScale(int percent)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsScale(percent);
        }
        /// <summary>
        /// Set the resolution scale to a given percent, and use custom downsampler settings
        /// </summary>
        public static void SetAllAsScale(int percent, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsScale(percent, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the operation mode as adaptive with target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier, targetFramerate);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier);
        }
        /// <summary>
        /// Set the operation mode as adaptive with target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="targetFramerate"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier, int targetFramerate, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier, targetFramerate, FilterType, sharpnessfactor, sampledist);
        }
        /// <summary>
        /// Set the operation mode as adaptive with screen refresh rate as target frame rate and use downsampling filter.
        /// </summary>
        /// <param name="minMultiplier"></param>
        /// <param name="maxMultiplier"></param>
        /// <param name="FilterType"></param>
        /// <param name="sharpnessfactor"></param>
        /// <param name="sampledist"></param>
        public static void SetAllAsAdaptive(float minMultiplier, float maxMultiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAdaptive(minMultiplier, maxMultiplier, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set a custom resolution multiplier
        /// </summary>
        public static void SetAllAsCustom(float Multiplier)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsCustom(Multiplier);
        }
        /// <summary>
        /// Set a custom resolution multiplier, and use custom downsampler settings
        /// </summary>
        public static void SetAllAsCustom(float Multiplier, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsCustom(Multiplier, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the multiplier of each screen axis independently. does not use downsampling filter.
        /// </summary>
        /// <param name="MultiplierX"></param>
        /// <param name="MultiplierY"></param>
        public static void SetAllAsAxisBased(float MultiplierX, float MultiplierY)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAxisBased(MultiplierX, MultiplierY);
        }
        /// <summary>
        ///  Set the multiplier of each screen axis independently while using the downsampling filter.
        /// </summary>
        public static void SetAllAsAxisBased(float MultiplierX, float MultiplierY, Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetAsAxisBased(MultiplierX, MultiplierY, FilterType, sharpnessfactor, sampledist);
        }

        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public static void SetAllDownsamplingSettings(bool use)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetDownsamplingSettings(use);
        }
        /// <summary>
        /// Set the downsampling shader parameters. If the case, this should be called after setting the mode, otherwise it might get overrided. (ex: SSAA)
        /// </summary>
        public static void SetAllDownsamplingSettings(Filter FilterType, float sharpnessfactor, float sampledist)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetDownsamplingSettings(FilterType,sharpnessfactor,sampledist);
        }

        /// <summary>
        /// Enable or disable the ultra mode for super sampling.(FSS)
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetAllUltra(bool enabled)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetUltra(enabled);
        }
        /// <summary>
        /// Set the intensity of the SSAA ultra effect (FSSAA intensity)
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetAllUltraIntensity(float intensity)
        {
            foreach (MadGoatSSAA ssaa in FindObjectsOfType<MadGoatSSAA>())
                ssaa.SetUltraIntensity(intensity);
        }

        /// <summary>
        /// Returns a ray from a given screenpoint
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Ray ScreenPointToRay(Vector3 position)
        {
            return renderCamera.ScreenPointToRay(position);
        }
        /// <summary>
        /// Transforms postion from screen space into viewport space.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 ScreenToViewportPoint(Vector3 position)
        {
            return renderCamera.ScreenToViewportPoint(position);
        }
        /// <summary>
        /// Transforms position from screen space into world space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 ScreenToWorldPoint(Vector3 position)
        {
            return renderCamera.ScreenToWorldPoint(position);
        }
        /// <summary>
        /// Transforms position from world space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 WorldToScreenPoint(Vector3 position)
        {
            return renderCamera.WorldToScreenPoint(position);
        }
        /// <summary>
        /// Transforms position from viewport space to screen space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual Vector3 ViewportToScreenPoint(Vector3 position)
        {
            return renderCamera.ViewportToScreenPoint(position);
        }
#endregion
    }
}

