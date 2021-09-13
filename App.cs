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

        BufferObject<float> VBO;
        BufferObject<uint> EBO;
        VertexArrayObject<float, uint> VAO;

        Shader shader;
        Texture texture;

        Camera camera;
        Transform box;

        Vector2 LastMousePosition;

        readonly float[] vertices =
       {
           //X      Y      Z      U     V
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
        };

        readonly uint[] indices =
        {
            0, 1, 3,
            1, 2, 3
        };

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

            EBO = new BufferObject<uint>(indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(VBO, EBO);

            // 顶点坐标
            VAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            // 顶点UV
            VAO.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            // 顶点颜色
            //VAO.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, 9, 5);

            shader = new Shader("vert.glsl", "frag.glsl");

            texture = new Texture(AppResource.logo);

            camera = new Camera();
            camera.Aspect = (float)MainWindow.Size.X / MainWindow.Size.Y;
            camera.Transform.EulerAngles = new(0, 180f, 0);
            camera.UpdateYawPaitch();

            box = new Transform();
            //box.Right = new Vector3(0.5f, 0.5f, 0.5f);

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

            box.Rotation = MathHelper.Rotate(new float3(tx * deltaTimef, ty * deltaTimef, tz * deltaTimef), box.Rotation);

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

        unsafe void onRender(double deltaTime)
        {
            var deltaTimef = (float)deltaTime;

            controller.Update(deltaTimef);

            Graphics.GL.Enable(EnableCap.DepthTest);
            Graphics.GL.ClearColor(0.85f, 0.87f, 0.89f, 1f);
            Graphics.GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            VAO.Bind();
            texture.Bind();
            shader.Use();
            shader.SetInt("uTexture0", 0);
            shader.SetMatrix("uModel", box.ViewMatrix);
            shader.SetMatrix("uView", camera.ViewMatrix);
            shader.SetMatrix("uProjection", camera.ProjectionMatrix);

            //Debug.WriteLine(camera.ViewMatrix);

            Graphics.GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

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
            VBO.Dispose();
            EBO.Dispose();
            VAO.Dispose();
            shader.Dispose();
            texture.Dispose();
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
