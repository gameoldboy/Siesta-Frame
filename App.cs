using SiestaFrame.Object;
using SiestaFrame.Rendering;
using SiestaFrame.SceneManagement;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Diagnostics;
using System.Numerics;
using Unity.Mathematics;

namespace SiestaFrame
{
    public class App
    {
        public static App Instance { get; private set; }

        public SiestaWindow MainWindow { get; private set; }
        public Audio.Engine AudioEngine { get; private set; }
        public SceneManager SceneManager { get; private set; }

        Vector2 LastMousePosition;

        public App()
        {
            Instance = this;
        }

        public unsafe void Init()
        {
            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
            Silk.NET.Input.Sdl.SdlInput.RegisterPlatform();

            MainWindow = new SiestaWindow("Siesta Frame *Demo v0.02*", new Vector2D<int>(1280, 720));

            MainWindow.Load += onLoad;
            MainWindow.Update += onUpdate;
            MainWindow.Render += onRender;
            MainWindow.GUI += onGUI;
            MainWindow.Resize += onResize;
            MainWindow.FocusChanged += onFocusChanged;
            MainWindow.Closing += onClose;

            AudioEngine = new Audio.Engine();
            AudioEngine.PlaySound(@"Assets\Sounds\Sugoi Kawai Desu Ne!.ogg");

            MainWindow.Wait();
        }

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

            GraphicsAPI.GL.ClearColor(0.85f, 0.88f, 0.9f, 1f);

            SceneManager = new SceneManager();
            SceneManager.LoadScene(new Scene("Demo Scene"));
            var scene = SceneManager.Instance.CurrentScene;

            var box = Utilities.LoadModel("box.obj");
            var suzanne = Utilities.LoadModel("Suzanne.obj");
            var nanosuit = Utilities.LoadModel(@"nanosuit\nanosuit.obj");
            var head = Utilities.LoadModel(@"lpshead\head.OBJ");
            var floor = Utilities.LoadModel("floor.obj");

            foreach (var material in floor.Materials)
            {
                material.BaseColor = new float4(1f, 1f, 1f, 1f);
                material.BaseMap = SceneManager.AddTexture("checkerboard.gobt");
                material.MatCapColor = new float3(0f, 0f, 0f);
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
                material.BaseColor = new float4(1f, 1f, 1f, 1f);
                material.MatCapColor = new float3(10f, 10f, 10f);
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

            mainLight.EulerAngles = new float3(35f, 195f, 0f);
            mainLightDir = new Vector3(mainLight.EulerAngles.x, mainLight.EulerAngles.y, mainLight.EulerAngles.z);

            box.Transform.Position = new float3(-0f, 1f, 0f);
            nanosuit.Transform.Position = new float3(-2f, 0f, 0f);
            suzanne.Transform.Position = new float3(2f, 1f, 0f);
            head.Transform.Position = new float3(4f, 1f, 0f);

            mainCamera.Aspect = (float)MainWindow.Window.Size.X / MainWindow.Window.Size.Y;
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
            const float s = 0.1f;
            const float r = MathF.PI * 2f;
            const float u = 1.5f;
            var tx = math.sin((float)MainWindow.Window.Time * s % 1f * r) * u;
            var ty = math.sin((float)MainWindow.Window.Time * s % 1f * r + r * 0.333f) * u;
            var tz = math.sin((float)MainWindow.Window.Time * s % 1f * r + r * 0.667f) * u;

            SceneManager.CurrentScene.Entites[4].Transform.Rotation =
                MathHelper.Rotate(new float3(tx * deltaTime, ty * deltaTime, tz * deltaTime),
                SceneManager.CurrentScene.Entites[4].Transform.Rotation);

            var moveSpeed = 20f * deltaTime;

            var mainLight = SceneManager.Instance.CurrentScene.MainLight;
            var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;

            mainCamera.ApplyYawPitch();
            mainLight.EulerAngles = new float3(mainLightDir.X, mainLightDir.Y, mainLightDir.Z);

            foreach (var keyboard in MainWindow.InputContext.Keyboards)
            {
                if (keyboard.IsKeyPressed(Key.ShiftLeft))
                {
                    moveSpeed /= 8;
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

            //Debug.WriteLine(camera.Transform.Position);
        }

        int FrameCount = 0;
        string framesPerSecond = "?";
        double FrameTimer = 0;

        unsafe void onRender(float deltaTime)
        {
            SceneManager.Instance.CurrentScene.Render();
        }

        Vector3 mainLightDir;

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
            ImGuiNET.ImGui.Begin("FPS");
            ImGuiNET.ImGui.Text(framesPerSecond);
            ImGuiNET.ImGui.Text("Main Light Direction");
            ImGuiNET.ImGui.DragFloat3("", ref mainLightDir);
            ImGuiNET.ImGui.End();
        }

        void onClose()
        {
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

        private void onResize(Vector2D<int> size)
        {
            var mainCamera = SceneManager.Instance.CurrentScene.MainCamera;
            mainCamera.Aspect = (float)size.X / size.Y;
        }

        bool windowFocus = true;

        void onFocusChanged(bool focus)
        {
            windowFocus = focus;
        }
    }
}
