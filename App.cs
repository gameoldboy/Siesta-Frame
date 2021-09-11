using SiestaFrame.Entity;
using SiestaFrame.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace SiestaFrame
{
    public class App
    {
        public static App Instance { get; private set; }

        public IWindow MainWindow { get; private set; }

        BufferObject<float> VBO;
        BufferObject<uint> EBO;
        VertexArrayObject<float, uint> VAO;

        Rendering.Shader shader;
        Rendering.Texture texture;
        //Setup the camera's location, and relative up and right directions
        private static Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 3.0f);
        private static Vector3 CameraTarget = Vector3.Zero;
        private static Vector3 CameraDirection = Vector3.Normalize(CameraPosition - CameraTarget);
        private static Vector3 CameraRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, CameraDirection));
        private static Vector3 CameraUp = Vector3.Cross(CameraDirection, CameraRight);

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

        public void Init()
        {
            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
            Silk.NET.Input.Sdl.SdlInput.RegisterPlatform();

            foreach (var s in Window.Platforms.Select
                (x => $"IsApplicable: {x.IsApplicable} | IsViewOnly: {x.IsViewOnly}"))
                Debug.WriteLine(s);

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "Siesta Frame";
            options.WindowBorder = WindowBorder.Fixed;

            MainWindow = Window.Create(options);

            MainWindow.Load += onLoad;
            MainWindow.Update += onUpdate;
            MainWindow.Render += onRender;
            MainWindow.Closing += onClose;

            MainWindow.Run();
        }

        unsafe void onLoad()
        {
            var icon = Utilities.LoadIcon(AppResource.logo);
            MainWindow.SetWindowIcon(ref icon);

            MainWindow.Center();

            IInputContext input = MainWindow.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
            }

            Graphics.GL = GL.GetApi(MainWindow);

            EBO = new BufferObject<uint>(indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(VBO, EBO);

            // 顶点坐标
            VAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            // 顶点UV
            VAO.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            // 顶点颜色
            //VAO.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, 9, 5);

            shader = new Rendering.Shader("vert.glsl", "frag.glsl");

            texture = new Rendering.Texture(AppResource.logo);

            //gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }

        void onUpdate(double obj)
        {

        }

        unsafe void onRender(double obj)
        {
            Graphics.GL.Enable(EnableCap.DepthTest);
            Graphics.GL.ClearColor(0.85f, 0.87f, 0.89f, 1f);
            Graphics.GL.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            VAO.Bind();
            texture.Bind();
            shader.Use();
            shader.SetInt("uTexture0", 0);

            var difference = (float)(MainWindow.Time * 100);

            var model = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));
            var view = Matrix4x4.CreateLookAt(CameraPosition, CameraTarget, CameraUp);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), (float)MainWindow.Size.X / MainWindow.Size.Y, 0.1f, 100.0f);

            shader.SetMatrix("uModel", model);
            shader.SetMatrix("uView", view);
            shader.SetMatrix("uProjection", projection);

            Graphics.GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        void onClose()
        {
            VBO.Dispose();
            EBO.Dispose();
            VAO.Dispose();
            shader.Dispose();
            texture.Dispose();
        }

        void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                MainWindow.Close();
            }
        }
    }
}
