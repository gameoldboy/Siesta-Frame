using SiestaFrame.Rendering;
using SiestaFrame.SceneManagement;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Shader = SiestaFrame.Rendering.Shader;

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

            MainWindow = new SiestaWindow("Siesta Frame *Demo v0.02*", new Vector2D<int>(1280, 720));

            MainWindow.Load += onLoad;
            MainWindow.Update += onUpdate;
            MainWindow.Render += onRender;
            MainWindow.GUI += onGUI;
            MainWindow.Resize += onResize;
            MainWindow.FocusChanged += onFocusChanged;
            MainWindow.Closing += onClose;

            AudioEngine = new Audio.Engine();
            //AudioEngine.PlaySound(@"Assets/Sounds/Sugoi Kawai Desu Ne!.ogg");

            MainWindow.Wait();
        }

        Shader postPocessingShader;
        int postPocessingMapLocation;
        int postPocessingMatrixModelLocation;
        int postPocessingMatrixViewLocation;
        int postPocessingMatrixProjectionLocation;
        readonly float[] postPocessingVertices = { -1f, -1f, 0f, 0f, 0f, 1, -1, 0f, 1f, 0f, -1f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f };
        readonly uint[] postPocessingIndices = { 0u, 1u, 2u, 1u, 3u, 2u };
        BufferObject<float> postPocessingVBO;
        BufferObject<uint> postPocessingEBO;
        VertexArrayObject<float, uint> postPocessingVAO;

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

            SceneManager = new SceneManager();
            SceneManager.LoadScene(new Scene("Demo Scene"));
            var scene = SceneManager.Instance.CurrentScene;

            postPocessingShader = SceneManager.AddCommonShader("PostProcessingVert.glsl", "PostProcessingFrag.glsl");
            postPocessingMapLocation = postPocessingShader.GetUniformLocation("_BaseMap");
            postPocessingMatrixModelLocation = postPocessingShader.GetUniformLocation("MatrixModel");
            postPocessingMatrixViewLocation = postPocessingShader.GetUniformLocation("MatrixView");
            postPocessingMatrixProjectionLocation = postPocessingShader.GetUniformLocation("MatrixProjection");
            postPocessingVBO = new BufferObject<float>(postPocessingVertices, BufferTargetARB.ArrayBuffer);
            postPocessingEBO = new BufferObject<uint>(postPocessingIndices, BufferTargetARB.ElementArrayBuffer);
            postPocessingVAO = new VertexArrayObject<float, uint>(postPocessingVBO, postPocessingEBO);
            postPocessingVAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 5, 0);
            postPocessingVAO.VertexAttributePointer<float>(1, 2, VertexAttribPointerType.Float, 5, 3);
            GraphicsAPI.GL.BindVertexArray(0);

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

            mainLight.Position = new float3(0f, 5, 0f);
            mainLight.EulerAngles = new float3(45f, -30f, 0f);
            mainLightDir = new Vector3(mainLight.EulerAngles.x, mainLight.EulerAngles.y, mainLight.EulerAngles.z);

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

            //Console.WriteLine(camera.Transform.Position);
        }

        int FrameCount = 0;
        string framesPerSecond = "?";
        double FrameTimer = 0;

        unsafe void onRender(float deltaTime)
        {
            var shadowMapFrameBuffer = GraphicsAPI.GL.GenFramebuffer();
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowMapFrameBuffer);
            var shadowMap = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, shadowMap);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, 4096u, 4096u, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.DrawBuffer(GLEnum.None);
            GraphicsAPI.GL.ReadBuffer(GLEnum.None);
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMap, 0);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, 4096u, 4096u);
            SceneManager.Instance.CurrentScene.RenderShadowMap();
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GraphicsAPI.GL.DeleteFramebuffer(shadowMapFrameBuffer);

            MainWindow.BindFrameBuffer(out var colorTexture, out var depthTexture);
            GraphicsAPI.GL.ClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, (uint)MainWindow.Width, (uint)MainWindow.Height);
            SceneManager.Instance.CurrentScene.Render(shadowMap);
            GraphicsAPI.GL.DeleteTexture(shadowMap);

            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsAPI.GL.Disable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(MainWindow.Window.Size);
            postPocessingVAO.Bind();
            postPocessingShader.Use();
            GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorTexture);
            postPocessingShader.SetInt(postPocessingMapLocation, 0);
            postPocessingShader.SetMatrix(postPocessingMatrixViewLocation, MathHelper.LookAt(math.forward(), float3.zero, math.up()));
            var aspect = (float)MainWindow.Window.Size.X / MainWindow.Window.Size.Y;
            if (aspect > MainWindow.Aspect)
            {
                postPocessingShader.SetMatrix(postPocessingMatrixModelLocation, float4x4.Scale(-MainWindow.Aspect, 1f, 1f));
                postPocessingShader.SetMatrix(postPocessingMatrixProjectionLocation, MathHelper.ortho(-aspect, aspect, -1f, 1f, 0.1f, 2f));
            }
            else
            {
                postPocessingShader.SetMatrix(postPocessingMatrixModelLocation, float4x4.Scale(-1f, 1f / MainWindow.Aspect, 1f));
                postPocessingShader.SetMatrix(postPocessingMatrixProjectionLocation, MathHelper.ortho(-1f, 1f, -1f / aspect, 1f / aspect, 0.1f, 2f));
            }
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)postPocessingIndices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.BindVertexArray(0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.UseProgram(0);
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
        }

        bool windowFocus = true;

        void onFocusChanged(bool focus)
        {
            windowFocus = focus;
        }
    }
}
