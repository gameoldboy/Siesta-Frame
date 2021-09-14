using SiestaFrame.Entity;
using SiestaFrame.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Unity.Mathematics;
using Shader = SiestaFrame.Rendering.Shader;
using Texture = SiestaFrame.Rendering.Texture;

namespace SiestaFrame
{
    public class App
    {
        public static App Instance { get; private set; }

        public IWindow MainWindow { get; private set; }

        ImGuiController controller;
        IInputContext inputContext;
        IKeyboard primaryKeyboard;

        Shader shader;
        Texture texture;

        Camera camera;
        Mesh Suzanne;

        Vector2 LastMousePosition;

        public App()
        {
            Instance = this;
        }

        public unsafe void Init()
        {
            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
            Silk.NET.Input.Sdl.SdlInput.RegisterPlatform();

            foreach (var s in Window.Platforms.Select
                (x => $"IsApplicable: {x.IsApplicable} | IsViewOnly: {x.IsViewOnly}"))
                Debug.WriteLine(s);

            var options = WindowOptions.Default;
            //options.API = new GraphicsAPI(
            //    ContextAPI.OpenGL, ContextProfile.Core,
            //    ContextFlags.ForwardCompatible, new APIVersion(4, 6));
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "Siesta Frame";
            options.WindowBorder = WindowBorder.Fixed;
            //options.VSync = false;
            Silk.NET.GLFW.GlfwProvider.GLFW.Value.WindowHint(Silk.NET.GLFW.WindowHintInt.Samples, 4);

            MainWindow = Window.Create(options);

            MainWindow.Load += onLoad;
            MainWindow.Update += onUpdate;
            MainWindow.Render += onRender;
            MainWindow.Closing += onClose;
            MainWindow.FramebufferResize += onFramebufferResize;

            MainWindow.Run();
        }

        unsafe void onLoad()
        {
            var icon = Utilities.LoadIcon(AppResource.logo);
            MainWindow.SetWindowIcon(ref icon);

            MainWindow.Center();

            controller = new ImGuiController(
                Graphics.GL = GL.GetApi(MainWindow),
                MainWindow,
                inputContext = MainWindow.CreateInput()
            );
            primaryKeyboard = inputContext.Keyboards.FirstOrDefault();
            for (int i = 0; i < inputContext.Keyboards.Count; i++)
            {
                inputContext.Keyboards[i].KeyDown += KeyDown;
            }
            for (int i = 0; i < inputContext.Mice.Count; i++)
            {
                inputContext.Mice[i].Cursor.CursorMode = CursorMode.Raw;
                inputContext.Mice[i].MouseUp += onMouseUp;
                inputContext.Mice[i].MouseDown += onMouseDown;
                inputContext.Mice[i].MouseMove += onMouseMove;
                inputContext.Mice[i].Scroll += onMouseWheel;
            }

            Graphics.GL.Enable(EnableCap.Multisample);
            Graphics.GL.Enable(EnableCap.FramebufferSrgb);
            clearColor = math.pow(clearColor, 2.2f);

            Suzanne = ModelLoader.Load(@"Models\Suzanne.obj");

            shader = new Shader("vert.glsl", "frag.glsl");

            texture = new Texture(@"Textures\5C4E41_CCCDD6_9B979B_B1AFB0-512px.png");

            camera = new Camera();
            camera.Aspect = (float)MainWindow.Size.X / MainWindow.Size.Y;
            camera.Transform.EulerAngles = new(0, 180f, 0);
            camera.UpdateYawPaitch();

            //Graphics.GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }

        void onUpdate(double deltaTime)
        {
            var deltaTimef = (float)deltaTime;

            const float s = 0.1f;
            const float r = MathF.PI * 2f;
            const float u = 1.5f;
            var tx = math.sin((float)MainWindow.Time * s % 1f * r) * u;
            var ty = math.sin((float)MainWindow.Time * s % 1f * r + r * 0.333f) * u;
            var tz = math.sin((float)MainWindow.Time * s % 1f * r + r * 0.667f) * u;

            //box.Rotation = MathHelper.Rotate(new float3(tx * deltaTimef, ty * deltaTimef, tz * deltaTimef), box.Rotation);

            var moveSpeed = 2.5f * deltaTimef;

            camera.Transform.EulerAngles = new float3(camera.Pitch, camera.Yaw, 0);

            if (primaryKeyboard.IsKeyPressed(Key.W))
            {
                camera.Transform.Position += camera.Transform.Forward * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.S))
            {
                camera.Transform.Position -= camera.Transform.Forward * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.A))
            {
                camera.Transform.Position -= camera.Transform.Right * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.D))
            {
                camera.Transform.Position += camera.Transform.Right * moveSpeed;
            }
        }

        int FrameCount = 0;
        string framesPerSecond = "?";
        double FrameTimer = 0;

        float4 clearColor = new float4(0.85f, 0.87f, 0.89f, 1f);

        unsafe void onRender(double deltaTime)
        {
            var deltaTimef = (float)deltaTime;

            controller.Update(deltaTimef);

            Graphics.GL.Enable(EnableCap.DepthTest);
            Graphics.GL.ClearColor(clearColor.x, clearColor.y, clearColor.z, clearColor.w);
            Graphics.GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            Suzanne.VAO.Bind();
            shader.Use();

            shader.SetMatrix("uModel", float4x4.identity);
            shader.SetMatrix("uView", camera.ViewMatrix);
            shader.SetMatrix("uProjection", camera.ProjectionMatrix);

            texture.Bind();
            shader.SetInt("uTexture0", 0);

            shader.SetVector("viewPos", camera.Transform.Position);

            //Debug.WriteLine(camera.ViewMatrix);

            Graphics.GL.DrawElements(PrimitiveType.Triangles, (uint)Suzanne.Indices.Length, DrawElementsType.UnsignedInt, null);
            Graphics.GL.BindVertexArray(0);

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
            ImGuiNET.ImGui.End();

            controller.Render();
        }

        void onClose()
        {
            Suzanne.VBO.Dispose();
            Suzanne.EBO.Dispose();
            Suzanne.VAO.Dispose();
            shader.Dispose();
            texture?.Dispose();
            controller.Dispose();
            inputContext.Dispose();
        }

        void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                MainWindow.Close();
            }
        }

        void onMouseUp(IMouse mouse, MouseButton mouseButton)
        {
        }

        void onMouseDown(IMouse mouse, MouseButton mouseButton)
        {
        }

        void onMouseMove(IMouse mouse, Vector2 position)
        {
            var lookSensitivity = 0.1f;
            if (LastMousePosition == default) { LastMousePosition = position; }
            else
            {
                var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
                var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
                LastMousePosition = position;

                camera.Yaw = (camera.Yaw + xOffset) % 360;
                camera.Pitch = Math.Clamp(camera.Pitch + yOffset, -89.0f, 89.0f);
            }
        }

        void onMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            camera.FOV = Math.Clamp(camera.FOV - scrollWheel.Y, 1.0f, 45f);
        }

        void onFramebufferResize(Vector2D<int> size)
        {
            Graphics.GL.Viewport(size);
        }
    }
}
