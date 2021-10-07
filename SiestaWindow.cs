using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
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

        uint2 size;
        Vector2D<int> windowSize;
        public uint Width => size.x;
        public uint Height => size.y;
        public uint2 ScreenMaxSize { get; private set; }
        public float Aspect { get; private set; }
        bool focus = true;
        bool lastFocus = true;

        Task WindowTask { get; }
        Task RenderingTask { get; }

        uint frameBuffer;
        public uint ColorAttachment { get; private set; }
        public uint DepthAttachment { get; private set; }
        public uint TempColorAttachment { get; private set; }
        public uint TempDepthAttachment { get; private set; }

        public SiestaWindow(string title, uint2 size)
        {
            var options = WindowOptions.Default;
            //var api = options.API;
            //var version = api.Version;
            //version.MajorVersion = 4;
            //version.MinorVersion = 6;
            //api.Version = version;
            //options.API = api;
            this.size = size;
            windowSize = new Vector2D<int>((int)size.x, (int)size.y);
            Aspect = (float)size.x / size.y;
            options.Size = new Vector2D<int>((int)size.x, (int)size.y);
            options.Title = title;
            options.WindowBorder = WindowBorder.Hidden;
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 0);
            options.PreferredDepthBufferBits = 0;
            options.PreferredStencilBufferBits = 0;

            WindowTask = new Task(() => windowTask(options));
            RenderingTask = new Task(() => renderingTask());

            WindowTask.Start();
        }

        public event Action Load;
        public event Action<float> Update;
        public event Action<float> Render;
        public event Action<float> GUI;
        public event Action<bool> FocusChanged;
        public event Action<uint2> Resize;
        public event Action Closing;
        public event Action<uint2> ResizeInternal;

        readonly object closeSyncLock = new object();
        EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

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
            while (!Window.IsClosing)
            {
                eventWaitHandle.WaitOne();
                if (isSetFullScreen)
                {
                    setFullScreen(fullScreen, vSync);
                    isSetFullScreen = false;
                }
                if (!Window.IsClosing)
                {
                    Window.DoEvents();
                }
            }

            Window.DoEvents();
            lock (closeSyncLock)
            {
                Window.Reset();
            }
        }

        unsafe void renderingTask()
        {
            glfw.MakeContextCurrent((GLFW.WindowHandle*)Window.Handle);
            // 渲染循环
            while (!Window.IsClosing)
            {
                lock (closeSyncLock)
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
                eventWaitHandle.Set();
            }
            onClose();
        }

        void onClose()
        {
            Controller.Dispose();
            InputContext.Dispose();
            GraphicsAPI.GL.DeleteFramebuffer(frameBuffer);
            GraphicsAPI.GL.DeleteTexture(ColorAttachment);
            GraphicsAPI.GL.DeleteTexture(DepthAttachment);
            GraphicsAPI.GL.DeleteTexture(TempColorAttachment);
            GraphicsAPI.GL.DeleteTexture(TempDepthAttachment);

            Closing?.Invoke();
        }

        unsafe void onLoad()
        {
            if (Window.VideoMode.Resolution.HasValue)
            {
                var maxSize = Window.VideoMode.Resolution.Value;
                ScreenMaxSize = new uint2((uint)maxSize.X, (uint)maxSize.Y);
            }
            else
            {
                ScreenMaxSize = new uint2(1920, 1080);
            }

            Window.Center(Window.Monitor);
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

            AllocRenderTexture();

            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Enable(EnableCap.CullFace);

            var version = GraphicsAPI.GL.GetStringS(GLEnum.Version);
            var maxElementsVertices = GraphicsAPI.GL.GetInteger(GLEnum.MaxElementsVertices);
            var maxElementsIndices = GraphicsAPI.GL.GetInteger(GLEnum.MaxElementsIndices);
            var maxVertexUniformComponents = GraphicsAPI.GL.GetInteger(GLEnum.MaxVertexUniformComponents);
            var maxFragmentUniformComponents = GraphicsAPI.GL.GetInteger(GLEnum.MaxFragmentUniformComponents);
            var maxUniformBlockSize = GraphicsAPI.GL.GetInteger(GLEnum.MaxUniformBlockSize);
            var maxUniformLocations = GraphicsAPI.GL.GetInteger(GLEnum.MaxUniformLocations);

            Console.WriteLine($"OpenGl Version:{version}");
            Console.WriteLine($"MaxElementsVertices:{maxElementsVertices}, MaxElementsIndices:{maxElementsIndices}");
            Console.WriteLine($"MaxVertexUniformComponents:{maxVertexUniformComponents}, MaxFragmentUniformComponents:{maxFragmentUniformComponents}, MaxUniformLocations:{maxUniformLocations}, MaxUniformBlockSize:{maxUniformBlockSize}");

            Load?.Invoke();
        }

        void onUpdate(double deltaTime)
        {
            if (windowSize != Window.Size)
            {
                Resize?.Invoke(new uint2((uint)Window.Size.X, (uint)Window.Size.Y));
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
            // 会导致GC
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

        public unsafe void AllocRenderTexture(uint width = 0, uint height = 0)
        {
            var size = new uint2(width, height);
            var resized = false;
            if (width > 0 && height > 0 && !this.size.Equals(size))
            {
                this.size = size;
                Aspect = (float)size.x / size.y;
                resized = true;
            }

            if (frameBuffer > 0)
            {
                GraphicsAPI.GL.DeleteFramebuffer(frameBuffer);
            }
            if (ColorAttachment > 0)
            {
                GraphicsAPI.GL.DeleteTexture(ColorAttachment);
            }
            if (DepthAttachment > 0)
            {
                GraphicsAPI.GL.DeleteTexture(DepthAttachment);
            }
            if (TempColorAttachment > 0)
            {
                GraphicsAPI.GL.DeleteTexture(TempColorAttachment);
            }
            if (TempDepthAttachment > 0)
            {
                GraphicsAPI.GL.DeleteTexture(TempDepthAttachment);
            }

            frameBuffer = GraphicsAPI.GL.GenFramebuffer();
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);

            ColorAttachment = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, ColorAttachment);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, Width, Height, 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorAttachment, 0);
            DepthAttachment = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, DepthAttachment);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, Width, Height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthAttachment, 0);
            TempColorAttachment = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, TempColorAttachment);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, Width, Height, 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            TempDepthAttachment = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, TempDepthAttachment);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, Width, Height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            if (resized)
            {
                ResizeInternal?.Invoke(size);
            }
        }

        public bool isSetFullScreen = false;
        public bool fullScreen = false;
        public bool vSync = true;

        public void SetFullScreen(bool fullScreen, bool vSync)
        {
            isSetFullScreen = true;
            this.fullScreen = fullScreen;
            this.vSync = vSync;
        }

        public unsafe void setFullScreen(bool fullScreen, bool vSync)
        {
            var monitor = glfw.GetMonitors(out var count)[Window.Monitor.Index];
            if (fullScreen)
            {
                glfw.SetWindowMonitor((GLFW.WindowHandle*)Window.Handle, monitor,
                    0, 0, (int)math.min(Width, ScreenMaxSize.x), (int)math.min(Height, ScreenMaxSize.y), (int)Window.VideoMode.RefreshRate);
                Window.VSync = vSync;
            }
            else
            {
                glfw.SetWindowMonitor((GLFW.WindowHandle*)Window.Handle, null, Window.Position.X, Window.Position.Y, (int)Width, (int)Height, 0);
                Window.VSync = vSync;
                Window.Center(Window.Monitor);
            }
        }

        public void BindFrameBuffer()
        {
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
        }

        public void BindFrameBuffer(uint colorAttachment, uint depthAttachment = 0, FramebufferTarget framebufferTarget = FramebufferTarget.Framebuffer)
        {
            GraphicsAPI.GL.BindFramebuffer(framebufferTarget, frameBuffer);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, colorAttachment);
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorAttachment, 0);
            if (depthAttachment > 0)
            {
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, depthAttachment);
                GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthAttachment, 0);
            }
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
