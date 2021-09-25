using ImGuiNET;
using SiestaFrame.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Glfw = Silk.NET.GLFW.Glfw;
using GLFW = Silk.NET.GLFW;
using GLFW_Window = Silk.NET.Windowing.Window;
using GlfwProvider = Silk.NET.GLFW.GlfwProvider;

namespace SiestaFrame
{
    public class SiestaWindow
    {
        public IWindow Window { get; private set; }

        readonly Glfw glfw = GlfwProvider.GLFW.Value;

        public IInputContext InputContext;
        public ImGuiController Controller;

        Vector2D<int> size;
        bool focus = true;
        bool lastFocus = true;

        Task WindowTask { get; }
        Task RenderingTask { get; }

        public SiestaWindow(string title, Vector2D<int> size, int msaaSamples = 4)
        {
            var options = WindowOptions.Default;
            //var api = options.API;
            //var version = api.Version;
            //version.MajorVersion = 4;
            //version.MinorVersion = 6;
            //api.Version = version;
            //options.API = api;
            this.size = size;
            options.Size = size;
            options.Title = title;
            options.IsVisible = false;

            glfw.WindowHint(GLFW.WindowHintInt.Samples, msaaSamples);

            WindowTask = new Task(() => windowTask(options));
            RenderingTask = new Task(() => renderingTask());

            WindowTask.Start();
        }

        public event Action Load;
        public event Action<float> Update;
        public event Action<float> Render;
        public event Action<float> GUI;
        public event Action<bool> FocusChanged;
        public event Action<Vector2D<int>> Resize;
        public event Action Closing;

        readonly object taskLock = new object();

        unsafe void windowTask(WindowOptions options)
        {
            Window = GLFW_Window.Create(options);

            Window.Load += onLoad;
            Window.Update += onUpdate;
            Window.Render += onRender;
            Window.FocusChanged += onFocusChanged;

            Window.Initialize();
            // 释放掉主线程的GL上下文
            glfw.MakeContextCurrent(null);
            // 启动渲染线程，防止窗口事件阻塞渲染
            RenderingTask.Start();
            // 窗口事件轮询
            Window.Run
            (
                () =>
                {
                    Thread.Sleep(1);
                    Window.DoEvents();
                }
            );

            Window.DoEvents();
            lock (taskLock)
            {
                Window.Reset();
            }
        }

        unsafe void renderingTask()
        {
            glfw.MakeContextCurrent((GLFW.WindowHandle*)Window.Handle);
            Window.Run
            (
                () =>
                {
                    lock (taskLock)
                    {
                        if (!Window.IsClosing)
                        {
                            Window.DoUpdate();
                        }
                        if (!Window.IsClosing)
                        {
                            Window.DoRender();
                        }
                    }
                }
            );
            onClose();
        }

        void onClose()
        {
            Controller.Dispose();
            InputContext.Dispose();

            Closing?.Invoke();
        }

        unsafe void onLoad()
        {
            Window.IsVisible = true;
            Window.Center();

            var icon = Utilities.LoadIcon(AppResource.logo);
            Window.SetWindowIcon(ref icon);

            var config = new ImGuiFontConfig("simhei.ttf", 12);
            Controller = new ImGuiController(
                Rendering.GraphicsAPI.GL = GL.GetApi(Window),
                Window,
                InputContext = Window.CreateInput(),
                config
            );
            var io = ImGui.GetIO();
            io.NativePtr->IniFilename = null;

            Rendering.GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            Rendering.GraphicsAPI.GL.Enable(EnableCap.CullFace);
            Rendering.GraphicsAPI.GL.Enable(EnableCap.Multisample);

            var version = Rendering.GraphicsAPI.GL.GetStringS(GLEnum.Version);
            var maxElementsVertices = Rendering.GraphicsAPI.GL.GetInteger(GLEnum.MaxElementsVertices);
            var maxElementsIndices = Rendering.GraphicsAPI.GL.GetInteger(GLEnum.MaxElementsIndices);
            var maxVertexUniformComponents = Rendering.GraphicsAPI.GL.GetInteger(GLEnum.MaxVertexUniformComponents);
            var maxFragmentUniformComponents = Rendering.GraphicsAPI.GL.GetInteger(GLEnum.MaxFragmentUniformComponents);
            var maxUniformBlockSize = Rendering.GraphicsAPI.GL.GetInteger(GLEnum.MaxUniformBlockSize);

            Debug.WriteLine($"OpenGl Version:{version}");
            Debug.WriteLine($"MaxElementsVertices:{maxElementsVertices}, MaxElementsIndices:{maxElementsIndices}");
            Debug.WriteLine($"MaxVertexUniformComponents:{maxVertexUniformComponents}, MaxFragmentUniformComponents:{maxFragmentUniformComponents} ,maxUniformBlockSize:{maxUniformBlockSize}");

            Load?.Invoke();
        }

        void onUpdate(double deltaTime)
        {
            if (size != Window.Size)
            {
                Rendering.GraphicsAPI.GL.Viewport(Window.Size);
                Resize?.Invoke(Window.Size);
                size = Window.Size;
            }
            if (focus != lastFocus)
            {
                FocusChanged?.Invoke(focus);
                lastFocus = focus;
            }

            Update?.Invoke((float)deltaTime);
        }

        void onRender(double deltaTime)
        {
            var deltaTimef = (float)deltaTime;
            Controller.Update(deltaTimef);

            Rendering.GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Rendering.GraphicsAPI.GL.Enable(EnableCap.FramebufferSrgb);
            Render?.Invoke(deltaTimef);

            GUI?.Invoke(deltaTimef);
            Rendering.GraphicsAPI.GL.Disable(EnableCap.FramebufferSrgb);
            Controller.Render();
        }

        void onFocusChanged(bool focus)
        {
            this.focus = focus;
        }

        public void Wait() => WindowTask.Wait();
    }
}
