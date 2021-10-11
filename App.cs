using BulletSharp;
using ImGuiNET;
using SiestaFrame.Object;
using SiestaFrame.Rendering;
using SiestaFrame.SceneManagement;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using GraphicsAPI = SiestaFrame.Rendering.GraphicsAPI;

namespace SiestaFrame
{
    public class App
    {
        public static App Instance { get; private set; }

        public SiestaWindow MainWindow { get; private set; }
        public Audio.Engine AudioEngine { get; private set; }
        public Physics.Simulation PhysicsSimulation { get; private set; }
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

            MainWindow = new SiestaWindow("Siesta Frame 🌸Demo v0.03🌸", new uint2(1280, 720));

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

            PhysicsSimulation = new Physics.Simulation();
            //PhysicsSimulation.FixedTimeStep = 0.02f;

            MainWindow.Wait();
        }

        ShadowMap shadowMap;
        GBuffer gBuffer;
        TemporalAntiAliasing temporalAntiAliasing;
        PostProcessing postProcessing;

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
                mouse.MouseMove += onMouseMove;
                mouse.Scroll += onMouseWheel;
            }

            GraphicsAPI.GL.ClearColor(0, 0, 0, 0);

            shadowMap = new ShadowMap(4096, 4096);
            postProcessing = new PostProcessing();
            gBuffer = new GBuffer();
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
                material.BaseColor = new float4(new float3(1.75f), 1f);
                material.BaseMap = SceneManager.AddTexture("checkerboard.gobt");
                material.MatCapColor = new float3(0.1f);
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
                //material.Mode = Material.BlendMode.AlphaHashed;
                material.BaseColor = new float4(1f, 1f, 1f, 0.5f);
                material.SpecularColor = new float4(10f, 10f, 10f, 1f);
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
                //material.MatCapColor = float3.zero;
            }
            foreach (var material in nanosuit.Materials)
            {
                material.BaseColor = new float4(8f, 8f, 8f, 1f);
                //material.BaseMap = Rendering.Texture.White;
                material.MatCapColor = new float3(1f);
                material.SpecularColor = new float4(0.5f, 0.5f, 0.5f, 0.5f);
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
            }
            foreach (var material in head.Materials)
            {
                material.SpecularColor = new float4(0.2f, 0.1f, 0.1f, 0.01f);
                material.NormalScale = 0.5f;
                material.MatCapMap = SceneManager.AddTexture("5C4E41_CCCDD6_9B979B_B1AFB0-512px.gobt");
            }

            var mainLight = scene.MainLight;
            var mainCamera = scene.MainCamera;

            mainLight.ShadowRange = 10f;
            shadowRange = mainLight.ShadowRange;
            mainLight.Transform.EulerAngles = new float3(45f, 130f, 0f);
            mainLightDir = new Vector3(mainLight.Transform.EulerAngles.x, mainLight.Transform.EulerAngles.y, mainLight.Transform.EulerAngles.z);

            floor.Transform.RigidOffsetPosition = new float3(0, -1f, 0);
            box.Transform.Position = new float3(0f, 1f, 0f);
            nanosuit.Transform.Position = new float3(-2f, 0f, 0f);
            suzanne.Transform.Position = new float3(2f, 1f, 0f);
            head.Transform.Position = new float3(4f, 1f, 0f);

            mainCamera.Transform.Position = new float3(0f, 1f, 3f);
            mainCamera.Transform.EulerAngles = new float3(0f, 180f, 0f);
            mainCamera.UpdateYawPitch();

            var floorshape = new BoxShape(50f, 1f, 50f);
            floor.Collision = PhysicsSimulation.AddCollision(floorshape, floor);
            floor.Collision.UserObject = null;
            scene.Entites.Add(floor);
            suzanne.Collision = PhysicsSimulation.AddCollision(suzanne);
            scene.Entites.Add(suzanne);
            nanosuit.Collision = PhysicsSimulation.AddCollision(nanosuit);
            scene.Entites.Add(nanosuit);
            head.Collision = PhysicsSimulation.AddCollision(head);
            scene.Entites.Add(head);
            for (int i = 0; i < 1000; i++)
            {
                var box2 = new Entity()
                {
                    Meshes = box.Meshes,
                    Materials = new Material[] { new Material() },
                    DrawType = Mesh.DrawType.GPUInstancing
                };
                box2.Transform.Position = new float3((i % 10 - 5) * 0.2f, 4f + i / 100 * 0.2f, (i / 10 % 10 - 5) * 0.2f);
                box2.Transform.Scale = new float3(0.2f);
                var shape = new BoxShape(0.1f);
                box2.RigidBody = PhysicsSimulation.AddRigidBody(shape, box2);
                scene.Entites.Add(box2);
                box2.Materials[0].Shader = SceneManager.AddShader("DefaultInstancedVert.glsl", "DefaultInstancedFrag.glsl");
                box2.Materials[0].UpdateShaderLocation();
                box2.Materials[0].BaseColor = new float4(1f, 0.2f, 0.5f, 1f);
            }
            var boxshape = new BoxShape(0.5f);
            box.Collision = PhysicsSimulation.AddCollision(boxshape, box);
            scene.Entites.Add(box);

            //GraphicsAPI.GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }

        void onUpdate(float deltaTime)
        {
            const float timeScale = 0.1f;
            const float round = MathF.PI * 2f;
            const float speed = 1.5f;
            var tx = math.sin((float)MainWindow.Window.Time * timeScale % 1f * round) * speed;
            var ty = math.sin((float)MainWindow.Window.Time * timeScale % 1f * round + round * 0.333f) * speed;
            var tz = math.sin((float)MainWindow.Window.Time * timeScale % 1f * round + round * 0.667f) * speed;

            var scene = SceneManager.Instance.CurrentScene;
            var box = scene.Entites[scene.Entites.Count - 1];
            box.Transform.Scale = new float3(tx * 0.5f);
            box.Transform.Rotation =
                 MathHelper.Rotate(new float3(tx * deltaTime, ty * deltaTime, tz * deltaTime),
                 box.Transform.Rotation);

            var moveSpeed = 2.5f * deltaTime;

            var mainLight = scene.MainLight;
            var mainCamera = scene.MainCamera;

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

            // 检测物理Sleep
            for (int i = 0; i < scene.Entites.Count; i++)
            {
                var entity = scene.Entites[i];
                if (entity.RigidBody == null)
                {
                    continue;
                }
                if (entity.RigidBody.MotionState != null && (
                    entity.RigidBody.ActivationState == ActivationState.IslandSleeping ||
                    entity.RigidBody.ActivationState == ActivationState.WantsDeactivation))
                {
                    var matrix = MathHelper.ToFloat4x4(entity.RigidBody.WorldTransform);
                    var transform = new RigidTransform(matrix);
                    transform.pos = new float3(0, 4f, 0);
                    entity.RigidBody.WorldTransform = MathHelper.ToMatrix(new float4x4(transform));
                    entity.RigidBody.Activate();
                    bool changeColor = MathHelper.Random.NextBool();
                    entity.Materials[0].BaseColor = new float4(1f, 0.2f, 0.5f, 1f) * (changeColor ? 0 : 1f);
                    entity.Materials[0].EmissiveColor = MathHelper.Random.NextFloat3() * (changeColor ? 4f : 0);
                }
            }
            scene.SyncCollision();
            scene.SyncRigidBody();

            PhysicsSimulation.Update(deltaTime);
        }

        int FrameCount = 0;
        string framesPerSecond = "?";
        double FrameTimer = 0;

        unsafe void onRender(float deltaTime)
        {
            var scene = SceneManager.Instance.CurrentScene;

            scene.CollectAndUpdateInstancedData();

            shadowMap.RenderShadowMap(scene);

            temporalAntiAliasing.PreTemporalAntiAliasing(scene.MainCamera);

            MainWindow.BindFrameBuffer(new uint[] { MainWindow.ColorAttachment, gBuffer.NormalTexture, gBuffer.MotionVectors }, MainWindow.DepthAttachment);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, MainWindow.Width, MainWindow.Height);
            scene.Render(shadowMap, temporalAntiAliasing);

            scene.ClearInstancedList();

            postProcessing.DoPostProcessing(MainWindow.ColorAttachment, MainWindow.DepthAttachment, shadowMap, gBuffer, temporalAntiAliasing);
        }

        Vector2 imguiWindowSize;
        bool mouseLB;
        Vector3 mainLightDir;
        float shadowRange;
        bool vSync = true;
        bool fullScreen = false;
        float bloomThreshold = 0.8f;
        float bloomIntensity = 1f;
        float ppExposure = 0;
        bool ppTonemapping = true;
        float ppContrast = 0.9f;
        float ppSaturation = 1.2f;
        float ppTemperature = 0;
        float ppTint = 0;

        unsafe void onGUI(float deltaTime)
        {
            FrameTimer += deltaTime;
            FrameCount++;
            if (FrameTimer > 1)
            {
                FrameTimer -= 1;
                framesPerSecond = FrameCount.ToString();
                FrameCount = 0;
            }

            var style = ImGui.GetStyle();
            if (canMouseMove)
            {
                var isMouseLBDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
                if (mouseLB != isMouseLBDown)
                {
                    if (isMouseLBDown)
                    {
                        onMouseDown(ImGui.GetMousePos(), MouseButton.Left);
                    }
                    mouseLB = isMouseLBDown;
                }
                style.NativePtr->Alpha = 0.7f;
                ImGui.Begin("调试面板", ImGuiWindowFlags.NoInputs);
            }
            else
            {
                style.NativePtr->Alpha = 1f;
                ImGui.Begin("调试面板");
                if (imguiWindowSize == default)
                {
                    imguiWindowSize = ImGui.GetWindowSize();
                    //imguiWindowSize.X += 150;
                    ImGui.SetWindowSize(imguiWindowSize);
                }
            }
            ImGui.Text($"帧/秒:{framesPerSecond}");
            ImGui.SliderFloat3("主光方向", ref mainLightDir, 0, 360f);
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
            if (ImGui.SliderFloat("Bloom阈值", ref bloomThreshold, 0, 2f))
            {
                postProcessing.Bloom.Threshold = bloomThreshold;
            }
            if (ImGui.SliderFloat("Bloom亮度", ref bloomIntensity, 0, 10f))
            {
                postProcessing.Bloom.Intensity = bloomIntensity;
            }
            if (ImGui.SliderFloat("曝光度", ref ppExposure, -10f, 10f))
            {
                postProcessing.Exposure = ppExposure;
            }
            if (ImGui.Checkbox("HDR色调映射", ref ppTonemapping))
            {
                postProcessing.Tonemapping = ppTonemapping;
            }
            if (ImGui.SliderFloat("对比度", ref ppContrast, 0, 2f) |
                ImGui.SliderFloat("饱和度", ref ppSaturation, 0, 2f) |
                ImGui.SliderFloat("色温", ref ppTemperature, -1, 1f) |
                ImGui.SliderFloat("色调", ref ppTint, -1, 1f))
            {
                postProcessing.ColorAdjustments.Contrast = ppContrast;
                postProcessing.ColorAdjustments.Saturation = ppSaturation;
                postProcessing.ColorAdjustments.Temperature = ppTemperature;
                postProcessing.ColorAdjustments.Tint = ppTint;
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
            gBuffer.Dispose();
            temporalAntiAliasing.Dispose();
            postProcessing.Dispose();
            SceneManager.Instance.CurrentScene.Dispose();
            SceneManager.UnloadCommonPool();
            AudioEngine.Dispose();
            PhysicsSimulation.Dispose();
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

        Entity currentSelect;

        void onMouseDown(Vector2 pos, MouseButton mouseButton)
        {
            var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;
            var rayStart = MathHelper.ToVector3(mainCamera.Transform.Position);
            var rayEnd = MathHelper.ToVector3(mainCamera.Transform.Position + mainCamera.Transform.Forward * 1000f);
            float3 hitPointWorld;
            float3 hitNormalWorld;
            using (var cb = new ClosestRayResultCallback(ref rayStart, ref rayEnd))
            {
                PhysicsSimulation.World.RayTestRef(ref rayStart, ref rayEnd, cb);
                if (cb.HasHit)
                {
                    hitPointWorld = MathHelper.ToFloat3(cb.HitPointWorld);
                    hitNormalWorld = MathHelper.ToFloat3(cb.HitNormalWorld);
                    hitNormalWorld = math.normalize(hitNormalWorld);

                    var entity = cb.CollisionObject.UserObject as Entity;
                    if (entity != null)
                    {
                        if (currentSelect == entity)
                        {
                            foreach (var material in currentSelect.Materials)
                            {
                                material.SelectedColor = float3.zero;
                            }
                            currentSelect = null;
                        }
                        else
                        {
                            if (currentSelect != null)
                            {
                                foreach (var material in currentSelect.Materials)
                                {
                                    material.SelectedColor = float3.zero;
                                }
                            }
                            foreach (var material in entity.Materials)
                            {
                                material.SelectedColor = new float3(4f, 2f, 0f);
                            }
                            currentSelect = entity;
                        }
                    }
                    else
                    {
                        if (currentSelect != null)
                        {
                            foreach (var material in currentSelect.Materials)
                            {
                                material.SelectedColor = float3.zero;
                            }
                        }
                        currentSelect = null;
                    }
                }
                else
                {
                    hitPointWorld = MathHelper.ToFloat3(rayEnd);
                    hitNormalWorld = float3.zero;
                    if (currentSelect != null)
                    {
                        foreach (var material in currentSelect.Materials)
                        {
                            material.SelectedColor = float3.zero;
                        }
                    }
                    currentSelect = null;
                }
            }
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

        void onResize(uint2 size)
        {
        }

        void onResizeInternal(uint2 size)
        {
            gBuffer.Alloc();
            temporalAntiAliasing.Alloc();
            postProcessing.Bloom.Alloc();
        }

        bool windowFocus = true;

        void onFocusChanged(bool focus)
        {
            windowFocus = focus;
        }
    }
}
