using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Glfw = Silk.NET.GLFW.Glfw;
using GLFW = Silk.NET.GLFW;
using GLFW_Window = Silk.NET.Windowing.Window;
using GlfwProvider = Silk.NET.GLFW.GlfwProvider;
using GraphicsAPI = SiestaFrame.Rendering.GraphicsAPI;

namespace SiestaFrame
{
    public class SiestaWindow
    {
        public IWindow Window { get; private set; }

        readonly Glfw glfw = GlfwProvider.GLFW.Value;

        public IInputContext InputContext;
        public ImGuiController Controller;

        Vector2D<int> size;
        Vector2D<int> windowSize;
        public int Width => size.X;
        public int Height => size.Y;
        public float Aspect { get; private set; }
        bool focus = true;
        bool lastFocus = true;

        Task WindowTask { get; }
        Task RenderingTask { get; }

        uint frameBuffer;
        uint colorAttachment;
        uint depthAttachment;

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
            windowSize = size;
            Aspect = (float)size.X / size.Y;
            options.Size = size;
            options.Title = title;
            options.WindowBorder = WindowBorder.Hidden;
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 0);
            options.PreferredDepthBufferBits = 0;
            options.PreferredStencilBufferBits = 0;
            //glfw.WindowHint(GLFW.WindowHintInt.Samples, 4);

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
            Window.Center();
            Window.WindowBorder = WindowBorder.Resizable;

            var icon = Utilities.LoadIcon(AppResource.logo);
            Window.SetWindowIcon(ref icon);

            var config = new ImGuiFontConfig("simhei.ttf", 12);
            Controller = new ImGuiController(
                GraphicsAPI.GL = GL.GetApi(Window),
                Window,
                InputContext = Window.CreateInput(),
                config
            );
            var io = ImGui.GetIO();
            io.NativePtr->IniFilename = null;

            frameBuffer = GraphicsAPI.GL.GenFramebuffer();
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);

            colorAttachment = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, (uint)Width, (uint)Height, 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorAttachment, 0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            depthAttachment = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, depthAttachment);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, (uint)Width, (uint)Width, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            //GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            //GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthAttachment, 0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            // rbo depth版本
            //depthAttachment = GraphicsAPI.GL.GenRenderbuffer();
            //GraphicsAPI.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthAttachment);
            //GraphicsAPI.GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent32f, (uint)Width, (uint)Height);
            //GraphicsAPI.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthAttachment);
            //GraphicsAPI.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            //GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Enable(EnableCap.CullFace);
            //GraphicsAPI.GL.Enable(EnableCap.Multisample);

            var version = GraphicsAPI.GL.GetStringS(GLEnum.Version);
            var maxElementsVertices = GraphicsAPI.GL.GetInteger(GLEnum.MaxElementsVertices);
            var maxElementsIndices = GraphicsAPI.GL.GetInteger(GLEnum.MaxElementsIndices);
            var maxVertexUniformComponents = GraphicsAPI.GL.GetInteger(GLEnum.MaxVertexUniformComponents);
            var maxFragmentUniformComponents = GraphicsAPI.GL.GetInteger(GLEnum.MaxFragmentUniformComponents);
            var maxUniformBlockSize = GraphicsAPI.GL.GetInteger(GLEnum.MaxUniformBlockSize);
            var maxUniformLocations = GraphicsAPI.GL.GetInteger(GLEnum.MaxUniformLocations);

            Console.WriteLine($"OpenGl Version:{version}");
            Console.WriteLine($"MaxElementsVertices:{maxElementsVertices}, MaxElementsIndices:{maxElementsIndices}");
            Console.WriteLine($"MaxVertexUniformComponents:{maxVertexUniformComponents}, MaxFragmentUniformComponents:{maxFragmentUniformComponents}, maxUniformLocations:{maxUniformLocations}, maxUniformBlockSize:{maxUniformBlockSize}");

            Load?.Invoke();
        }

        void onUpdate(double deltaTime)
        {
            if (windowSize != Window.Size)
            {
                Resize?.Invoke(Window.Size);
                windowSize = Window.Size;
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

            GraphicsAPI.GL.Enable(EnableCap.FramebufferSrgb);
            Render?.Invoke(deltaTimef);

            GUI?.Invoke(deltaTimef);
            GraphicsAPI.GL.Disable(EnableCap.FramebufferSrgb);
            Controller.Render();
        }

        void onFocusChanged(bool focus)
        {
            this.focus = focus;
        }

        public void Wait() => WindowTask.Wait();

        public void BindFrameBuffer(out uint color, out uint depth)
        {
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            color = colorAttachment;
            depth = depthAttachment;
        }
    }
}
