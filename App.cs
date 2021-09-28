using ImGuiNET;
using SiestaFrame.Rendering;
using SiestaFrame.SceneManagement;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using GlfwProvider = Silk.NET.GLFW.GlfwProvider;
using GraphicsAPI = SiestaFrame.Rendering.GraphicsAPI;

namespace SiestaFrame
{
    public class App
    {
        public static App Instance { get; private set; }

        public SiestaWindow MainWindow { get; private set; }
        public Audio.Engine AudioEngine { get; private set; }
        public SceneManager SceneManager { get; private set; }

        Vector2 LastMousePosition;

#if DEBUG && WIN32
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
#endif

        public App()
        {
            Instance = this;
        }

        public unsafe void Init()
        {
#if DEBUG && WIN32
            AllocConsole();
#endif
            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
            Silk.NET.Input.Sdl.SdlInput.RegisterPlatform();

            MainWindow = new SiestaWindow("Siesta Frame *Demo v0.02*", new int2(1280, 720));

            MainWindow.Load += onLoad;
            MainWindow.Update += onUpdate;
            MainWindow.Render += onRender;
            MainWindow.GUI += onGUI;
            MainWindow.Resize += onResize;
            MainWindow.ResizeInternal += onResizeInternal;
            MainWindow.FocusChanged += onFocusChanged;
            MainWindow.Closing += onClose;

            AudioEngine = new Audio.Engine();
            //AudioEngine.PlaySound(@"Assets/Sounds/Sugoi Kawai Desu Ne!.ogg");

            MainWindow.Wait();
        }

        ShadowMap shadowMap;
        MotionVector motionVector;
        TemporalAntiAliasing temporalAntiAliasing;
        PostProcessing postProcessing;

        float4 clearColor = new float4(0.85f, 0.88f, 0.9f, 1f);

        unsafe void onLoad()
        {
            foreach (var keyboard in MainWindow.InputContext.Keyboards)
            {
                keyboard.KeyDown += KeyDown;
                keyboard.KeyUp += KeyUp;
            }
            foreach (var mouse in MainWindow.InputContext.Mice)
            {
                mouse.Cursor.CursorMode = CursorMode.Raw;
                mouse.MouseUp += onMouseUp;
                mouse.MouseDown += onMouseDown;
                mouse.MouseMove += onMouseMove;
                mouse.Scroll += onMouseWheel;
            }

            GraphicsAPI.GL.ClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);

            shadowMap = new ShadowMap(4096, 4096);
            postProcessing = new PostProcessing();
            motionVector = new MotionVector();
            temporalAntiAliasing = new TemporalAntiAliasing();

            SceneManager = new SceneManager();
            SceneManager.LoadScene(new Scene("Demo Scene"));
            var scene = SceneManager.Instance.CurrentScene;

            var box = Utilities.LoadModel("box.obj");
            var suzanne = Utilities.LoadModel("Suzanne.obj");
            var nanosuit = Utilities.LoadModel(@"nanosuit/nanosuit.obj");
            var head = Utilities.LoadModel(@"lpshead/head.OBJ");
            var floor = Utilities.LoadModel("floor.obj");

            foreach (var material in floor.Materials)
            {
                material.BaseColor = new float4(1f, 1f, 1f, 1f);
                material.BaseMap = SceneManager.AddTexture("checkerboard.gobt");
                material.MatCapColor = new float3(0.05f, 0.05f, 0.05f);
                material.SpecularColor = new float4(0f, 0f, 0f, 1f);
                material.TilingOffset = new float4(20f, 20f, 0f, 0f);
            }
            foreach (var material in box.Materials)
            {
                material.Mode = Material.BlendMode.Alpha;
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
            }
            foreach (var material in suzanne.Materials)
            {
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
            }
            foreach (var material in nanosuit.Materials)
            {
                material.BaseColor = new float4(4f, 4f, 4f, 1f);
                //material.BaseMap = Rendering.Texture.White;
                //material.MatCapColor = new float3(0.2f, 0.2f, 0.2f);
                material.SpecularColor = new float4(0.5f, 0.5f, 0.5f, 0.5f);
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
            }
            foreach (var material in head.Materials)
            {
                material.SpecularColor = new float4(0.1f, 0.1f, 0.1f, 0.01f);
                material.NormalScale = 0.5f;
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
            }

            var mainLight = SceneManager.Instance.CurrentScene.MainLight;
            var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;

            mainLight.ShadowRange = 10f;
            shadowRange = mainLight.ShadowRange;
            mainLight.Transform.EulerAngles = new float3(45f, 130f, 0f);
            mainLightDir = new Vector3(mainLight.Transform.EulerAngles.x, mainLight.Transform.EulerAngles.y, mainLight.Transform.EulerAngles.z);

            box.Transform.Position = new float3(-0f, 1f, 0f);
            nanosuit.Transform.Position = new float3(-2f, 0f, 0f);
            suzanne.Transform.Position = new float3(2f, 1f, 0f);
            head.Transform.Position = new float3(4f, 1f, 0f);

            mainCamera.Transform.Position = new float3(0f, 1f, 3f);
            mainCamera.Transform.EulerAngles = new float3(0f, 180f, 0f);
            mainCamera.UpdateYawPitch();

            scene.Entites.Add(floor);
            scene.Entites.Add(suzanne);
            scene.Entites.Add(nanosuit);
            scene.Entites.Add(head);
            scene.Entites.Add(box);

            //Graphics.GraphicsAPI.GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }

        void onUpdate(float deltaTime)
        {
            const float timeScale = 0.1f;
            const float round = MathF.PI * 2f;
            const float speed = 1.5f;
            var tx = math.sin((float)MainWindow.Window.Time * timeScale % 1f * round) * speed;
            var ty = math.sin((float)MainWindow.Window.Time * timeScale % 1f * round + round * 0.333f) * speed;
            var tz = math.sin((float)MainWindow.Window.Time * timeScale % 1f * round + round * 0.667f) * speed;

            SceneManager.CurrentScene.Entites[4].Transform.Rotation =
                MathHelper.Rotate(new float3(tx * deltaTime, ty * deltaTime, tz * deltaTime),
                SceneManager.CurrentScene.Entites[4].Transform.Rotation);

            var moveSpeed = 2.5f * deltaTime;

            var mainLight = SceneManager.Instance.CurrentScene.MainLight;
            var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;

            mainCamera.ApplyYawPitch();
            mainLight.Transform.EulerAngles = new float3(mainLightDir.X, mainLightDir.Y, mainLightDir.Z);
            mainLight.ShadowRange = shadowRange;

            foreach (var keyboard in MainWindow.InputContext.Keyboards)
            {
                if (keyboard.IsKeyPressed(Key.ShiftLeft))
                {
                    moveSpeed *= 8;
                }
                if (keyboard.IsKeyPressed(Key.W))
                {
                    mainCamera.Transform.Position += mainCamera.Transform.Forward * moveSpeed;
                }
                if (keyboard.IsKeyPressed(Key.S))
                {
                    mainCamera.Transform.Position -= mainCamera.Transform.Forward * moveSpeed;
                }
                if (keyboard.IsKeyPressed(Key.A))
                {
                    mainCamera.Transform.Position -= mainCamera.Transform.Right * moveSpeed;
                }
                if (keyboard.IsKeyPressed(Key.D))
                {
                    mainCamera.Transform.Position += mainCamera.Transform.Right * moveSpeed;
                }
            }

            //Console.WriteLine(camera.Transform.Position);
        }

        int FrameCount = 0;
        string framesPerSecond = "?";
        double FrameTimer = 0;

        unsafe void onRender(float deltaTime)
        {
            shadowMap.RenderShadowMap(SceneManager.Instance.CurrentScene);

            temporalAntiAliasing.PreTemporalAntiAliasing(SceneManager.Instance.CurrentScene.MainCamera);

            MainWindow.BindFrameBuffer(MainWindow.ColorAttachment, MainWindow.DepthAttachment);
            GraphicsAPI.GL.ClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, (uint)MainWindow.Width, (uint)MainWindow.Height);
            SceneManager.Instance.CurrentScene.Render(shadowMap, temporalAntiAliasing);

            motionVector.RenderMotionVector(SceneManager.Instance.CurrentScene);

            temporalAntiAliasing.DoTemporalAntiAliasing(postProcessing, MainWindow.ColorAttachment, MainWindow.DepthAttachment, motionVector);

            postProcessing.DoPostProcessing(MainWindow.ColorAttachment, MainWindow.DepthAttachment, motionVector);
        }

        Vector3 mainLightDir;
        float shadowRange;
        bool vSync = true;
        bool fullScreen = false;

        void onGUI(float deltaTime)
        {
            FrameTimer += deltaTime;
            FrameCount++;
            if (FrameTimer > 1)
            {
                FrameTimer -= 1;
                framesPerSecond = FrameCount.ToString();
                FrameCount = 0;
            }

            ImGui.Begin("Controller");
            ImGui.Text($"FPS:{framesPerSecond}");
            ImGui.SliderFloat3("Main Light Direction", ref mainLightDir, 0, 360f);
            if (ImGui.Checkbox("垂直同步", ref vSync))
            {
                MainWindow.Window.VSync = vSync;
            }
            ImGui.SliderFloat("阴影范围", ref shadowRange, 1f, 100f);
            if (ImGui.Button("720P"))
            {
                MainWindow.AllocRenderTexture(1280, 720);
                MainWindow.SetFullScreen(fullScreen, vSync);
            }
            ImGui.SameLine();
            if (ImGui.Button("1080P"))
            {
                MainWindow.AllocRenderTexture(1920, 1080);
                MainWindow.SetFullScreen(fullScreen, vSync);
            }
            ImGui.SameLine();
            if (ImGui.Button("1440P"))
            {
                MainWindow.AllocRenderTexture(2560, 1440);
                MainWindow.SetFullScreen(fullScreen, vSync);
            }
            ImGui.SameLine();
            if (ImGui.Button("2160P"))
            {
                MainWindow.AllocRenderTexture(3840, 2160);
                MainWindow.SetFullScreen(fullScreen, vSync);
            }
            if (ImGui.Checkbox("全屏", ref fullScreen))
            {
                MainWindow.SetFullScreen(fullScreen, vSync);
            }
            ImGui.Text("移动：WASD，加速：LeftShift，操作：LeftAlt");
            ImGui.End();
        }

        void onClose()
        {
            shadowMap.Dispose();
            motionVector.Dispose();
            temporalAntiAliasing.Dispose();
            postProcessing.Dispose();
            SceneManager.Instance.CurrentScene.Dispose();
            SceneManager.UnloadCommonPool();
        }

        void KeyUp(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.AltLeft)
            {
                foreach (var mouse in MainWindow.InputContext.Mice)
                {
                    mouse.Cursor.CursorMode = CursorMode.Raw;
                    canMouseMove = true;
                }
            }
        }

        void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                MainWindow.Window.Close();
            }
            if (arg2 == Key.AltLeft)
            {
                foreach (var mouse in MainWindow.InputContext.Mice)
                {
                    mouse.Cursor.CursorMode = CursorMode.Normal;
                    canMouseMove = false;
                }
            }
        }

        void onMouseUp(IMouse mouse, MouseButton mouseButton)
        {
        }

        void onMouseDown(IMouse mouse, MouseButton mouseButton)
        {
        }

        bool canMouseMove = true;

        void onMouseMove(IMouse mouse, Vector2 position)
        {
            var lookSensitivity = 0.1f;
            if (LastMousePosition == default) { LastMousePosition = position; }
            else
            {
                var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
                var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
                LastMousePosition = position;

                var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;
                if (windowFocus && canMouseMove)
                {
                    mainCamera.Yaw = (mainCamera.Yaw + xOffset) % 360;
                    mainCamera.Pitch = Math.Clamp(mainCamera.Pitch + yOffset, -89.0f, 89.0f);
                }
            }
        }

        void onMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;
            mainCamera.FOV = Math.Clamp(mainCamera.FOV - scrollWheel.Y, 1.0f, 45f);
        }

        void onResize(int2 size)
        {
        }

        void onResizeInternal(int2 size)
        {
            motionVector.Alloc();
            temporalAntiAliasing.Alloc();
        }

        bool windowFocus = true;

        void onFocusChanged(bool focus)
        {
            windowFocus = focus;
        }
    }
}
