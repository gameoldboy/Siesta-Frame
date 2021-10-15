using ImGuiNET;
using ModelTools.Rendering;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using static ModelTools.Rendering.Mesh;
using Shader = ModelTools.Rendering.Shader;

namespace ModelTools
{
    class Program
    {
        public static IWindow window { get; private set; }
        public static GL GL { get; private set; }

        static IInputContext inputContext;
        static ImGuiController controller;

        struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrFilter;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrFile;
            public int nMaxFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrFileTitle;
            public int nMaxFileTitle;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrInitialDir;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetOpenFileNameW(ref OPENFILENAME ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetSaveFileNameW(ref OPENFILENAME lpofn);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int MessageBoxW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string text, [MarshalAs(UnmanagedType.LPWStr)] string caption, uint type);

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
            Silk.NET.Input.Sdl.SdlInput.RegisterPlatform();

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "Siesta Model Tools";
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 0);
            options.PreferredDepthBufferBits = 32;
            options.PreferredStencilBufferBits = 0;
            options.IsEventDriven = true;
            window = Window.Create(options);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.FramebufferResize += Window_FramebufferResize; ;
            window.Closing += Window_Closing;

            window.Run();
        }

        static Shader linesShader;
        static int linesModelMatrixLocation;
        static int linesViewMatrixLocation;
        static int linesProjectionMatrixLocation;
        static int linesColorLocation;

        static unsafe void Window_Load()
        {
            GlfwProvider.GLFW.Value
                .SetDropCallback((WindowHandle*)window.Handle, Window_FileDrop);

            var assembly = Assembly.GetExecutingAssembly();

            ImGuiBinaryFontConfig imGuiBinaryFontConfig;
            using (Stream stream = assembly.GetManifestResourceStream("ModelTools.simhei.ttf"))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                imGuiBinaryFontConfig = new ImGuiBinaryFontConfig(reader.ReadBytes((int)stream.Length), 12);
            }
            controller = new ImGuiController(
                GL = GL.GetApi(window),
                window,
                inputContext = window.CreateInput(),
                imGuiBinaryFontConfig);
            ImGui.GetIO().NativePtr->IniFilename = null;
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }
            foreach (var mouese in inputContext.Mice)
            {
                mouese.Scroll += Mouese_Scroll;
            }

            string vert, frag;
            using (Stream stream = assembly.GetManifestResourceStream("ModelTools.Shaders.linesvert.glsl"))
            using (StreamReader reader = new StreamReader(stream))
            {
                vert = reader.ReadToEnd();
            }
            using (Stream stream = assembly.GetManifestResourceStream("ModelTools.Shaders.linesfrag.glsl"))
            using (StreamReader reader = new StreamReader(stream))
            {
                frag = reader.ReadToEnd();
            }
            linesShader = new ShaderSource(vert, frag);
            linesModelMatrixLocation = linesShader.GetUniformLocation("MatrixModel");
            linesViewMatrixLocation = linesShader.GetUniformLocation("MatrixView");
            linesProjectionMatrixLocation = linesShader.GetUniformLocation("MatrixProjection");
            linesColorLocation = linesShader.GetUniformLocation("_BaseColor");
            bonesVertices = new List<float3>();

            GL.ClearColor(0.4f, 0.5f, 0.6f, 1f);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
        }

        static float3 cameraPos;
        static float3 cameraPosOffset;
        static float cameraFOV = 45f;
        static float cameraYaw = 180f;
        static float cameraPitch;
        static float cameraDistance;
        static float3 cameraRight
        {
            get
            {
                var rotation = MathHelper.FromEuler(
                    new float3(
                        cameraPitch * MathHelper.Deg2Rad,
                        cameraYaw * MathHelper.Deg2Rad,
                        0));
                return math.rotate(rotation, math.right());
            }
        }
        static float3 cameraUp
        {
            get
            {
                var rotation = MathHelper.FromEuler(
                    new float3(
                        cameraPitch * MathHelper.Deg2Rad,
                        cameraYaw * MathHelper.Deg2Rad,
                        0));
                return math.rotate(rotation, math.up());
            }
        }
        static float3 cameraForward
        {
            get
            {
                var rotation = MathHelper.FromEuler(
                    new float3(
                        cameraPitch * MathHelper.Deg2Rad,
                        cameraYaw * MathHelper.Deg2Rad,
                        0));
                return math.rotate(rotation, math.forward());
            }
        }
        static float4x4 invViewProj;
        static Model model;

        static void Window_Update(double dt)
        {
            if (model != null)
            {
                if (mouseLB)
                {
                    cameraPos = model.AABB.center;
                    cameraYaw = (cameraYaw + mouseDelta.x) % 360f;
                    cameraPitch = math.clamp(cameraPitch + mouseDelta.y, -89.999f, 89.999f);
                    cameraPos += (-cameraForward) * cameraDistance;
                }
                if (mouseMB)
                {
                    var delta = math.mul(invViewProj, new float4(
                        new float2(-mouseDelta.x, mouseDelta.y) / new float2(window.Size.X, window.Size.Y) * 2f, 0, 0f)).xyz;
                    cameraPosOffset += delta * cameraDistance;
                }
                if (mouseRB)
                {
                    var forward = mouseDelta.y / window.Size.Y * cameraDistance;
                    cameraDistance += forward;
                    cameraPos = model.AABB.center;
                    cameraPos += (-cameraForward) * cameraDistance;
                }
            }
        }

        static void ResetCamera()
        {
            cameraFOV = 45f;
            float fovRad = 0.5f * cameraFOV * MathHelper.Deg2Rad;
            float halfWidth = (model.AABB.right - model.AABB.left) * 0.5f;
            float halfHight = (model.AABB.top - model.AABB.bottom) * 0.5f;
            float halfLength = (model.AABB.front - model.AABB.back) * 0.5f;
            float longest = math.max(halfWidth, halfHight);
            cameraDistance = math.cos(fovRad) * longest / math.sin(fovRad) + halfLength;
            cameraPos = model.AABB.center;
            cameraPosOffset = float3.zero;
            cameraYaw = 180f;
            cameraPitch = 0;
            cameraPos += (-cameraForward) * cameraDistance;
        }

        static bool viewBones;
        static List<float3> bonesVertices;

        static void Window_Render(double dt)
        {
            var deltatime = (float)dt;
            controller.Update(deltatime);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.FramebufferSrgb);
            if (model != null)
            {
                RenderingData renderingData;
                renderingData.viewMatrix = MathHelper.LookAt(
                    cameraPos + cameraPosOffset,
                    cameraPos + cameraPosOffset + cameraForward, math.up());
                var farClip = math.max(math.max(
                    model.AABB.right - model.AABB.left,
                    model.AABB.top - model.AABB.bottom),
                    model.AABB.front - model.AABB.back) * 0.5f + cameraDistance;
                //Console.WriteLine($"cameraDistance:{cameraDistance}, farClip:{farClip}");
                renderingData.projectionMatrix =
                    MathHelper.PerspectiveFov(cameraFOV * MathHelper.Deg2Rad,
                    (uint)window.Size.X, (uint)window.Size.Y, 0.0001f, farClip);
                invViewProj = math.inverse(math.mul(renderingData.projectionMatrix, renderingData.viewMatrix));
                renderingData.cameraPosition = cameraPos + cameraPosOffset;
                renderingData.mainLightDirection = -cameraForward;
                for (int i = 0; i < model.Meshes.Length; i++)
                {
                    var mesh = model.Meshes[i];
                    var material = model.Materials[i % model.Materials.Length];
                    DrawData drawData;
                    drawData.material = material;
                    var matrix = float4x4.identity;
                    matrix.c0 *= -1;
                    drawData.modelMatrix = matrix;

                    mesh.Draw(drawData, renderingData);
                }

                if (viewBones)
                {
                    for (int i = 0; i < model.Bones.Length; i++)
                    {
                        var bone = model.Bones[i];
                        if (bone.parent == null || bone.parent == model.SkeletonRoot)
                        {
                            continue;
                        }
                        var pos = math.mul(bone.CalculateObjectSpaceMatrix(), new float4(0, 0, 0, 1f)).xyz;
                        var posParent = math.mul(bone.parent.CalculateObjectSpaceMatrix(), new float4(0, 0, 0, 1f)).xyz;
                        bonesVertices.Add(pos);
                        bonesVertices.Add(posParent);
                    }

                    if (bonesVertices.Count > 0)
                    {
                        unsafe
                        {
                            var vao = GL.GenVertexArray();
                            GL.BindVertexArray(vao);
                            var vbo = GL.GenBuffer();
                            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                            fixed (void* d = &bonesVertices.ToArray()[0])
                            {
                                GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float3) * bonesVertices.Count), d, BufferUsageARB.StaticDraw);
                            }
                            GL.EnableVertexAttribArray(0);
                            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(float3), (void*)0);
                            linesShader.Use();
                            var matrix = float4x4.identity;
                            matrix.c0 *= -1f;
                            linesShader.SetMatrix(linesModelMatrixLocation, matrix);
                            linesShader.SetMatrix(linesViewMatrixLocation, renderingData.viewMatrix);
                            linesShader.SetMatrix(linesProjectionMatrixLocation, renderingData.projectionMatrix);
                            linesShader.SetVector(linesColorLocation, new float4(0, 1f, 0, 1f));
                            GL.Disable(EnableCap.FramebufferSrgb);
                            GL.Disable(EnableCap.DepthTest);
                            GL.Enable(EnableCap.Blend);
                            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                            GL.DrawArrays(PrimitiveType.Lines, 0, (uint)bonesVertices.Count * 3);
                            GL.Enable(EnableCap.FramebufferSrgb);
                            GL.Enable(EnableCap.DepthTest);
                            GL.Disable(EnableCap.Blend);
                            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
                            GL.BindVertexArray(0);
                            GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
                            GL.UseProgram(0);
                            GL.DeleteVertexArray(vao);
                            GL.DeleteBuffer(vbo);
                            bonesVertices.Clear();
                        }
                    }
                }
            }

            onGUI(deltatime);
            GL.Disable(EnableCap.FramebufferSrgb);
            controller.Render();

            var error = GL.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine(error.ToString());
            }
        }

        static float2 mousePos;
        static float2 mouseDelta;
        static bool mouseLB;
        static bool mouseMB;
        static bool mouseRB;
        static bool altKey;

        static void onGUI(float dt)
        {
            var lastMousePos = mousePos;
            mousePos = MathHelper.ToFloat2(ImGui.GetMousePos());
            mouseDelta = mousePos - lastMousePos;

            var isMouseLBDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            var isMouseMBDown = ImGui.IsMouseDown(ImGuiMouseButton.Middle);
            var isMouseRBDown = ImGui.IsMouseDown(ImGuiMouseButton.Right);
            var isAltKeyDown = ImGui.IsKeyDown((int)Key.AltLeft);
            if (mouseLB != isMouseLBDown)
            {
                if (isMouseLBDown)
                {
                    onMouseDown(ImGuiMouseButton.Left);
                }
                else
                {
                    onMouseUp(ImGuiMouseButton.Left);
                }
                mouseLB = isMouseLBDown;
            }
            if (mouseMB != isMouseMBDown)
            {
                if (isMouseMBDown)
                {
                    onMouseDown(ImGuiMouseButton.Middle);
                }
                else
                {
                    onMouseUp(ImGuiMouseButton.Middle);
                }
                mouseMB = isMouseMBDown;
            }
            if (mouseRB != isMouseRBDown)
            {
                if (isMouseRBDown)
                {
                    onMouseDown(ImGuiMouseButton.Right);
                }
                else
                {
                    onMouseUp(ImGuiMouseButton.Right);
                }
                mouseRB = isMouseRBDown;
            }
            if (altKey != isAltKeyDown)
            {
                altKey = isAltKeyDown;
            }

            ImGui.Begin("Tools");
            if (ImGui.Button("Open"))
            {
                var ofn = new OPENFILENAME();
                ofn.lStructSize = Marshal.SizeOf(ofn);
                ofn.hwndOwner = window.Native.Win32.Value.Hwnd;
                ofn.lpstrFilter = "Model files (*.obj;*.fbx)\0*.obj;*.fbx\0\0";
                ofn.lpstrFile = new string(new char[256]);
                ofn.nMaxFile = ofn.lpstrFile.Length;
                ofn.lpstrFileTitle = new string(new char[64]);
                ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
                if (GetOpenFileNameW(ref ofn))
                {
                    var modelPath = ofn.lpstrFile;
                    if (!string.IsNullOrWhiteSpace(modelPath))
                    {
                        Console.WriteLine($"Open:{modelPath}");
                        model?.Dispose();
                        load(modelPath);
                    }
                }
            }
            if (model != null)
            {
                var scale = model.SkeletonRoot.UnitScaleFactor;
                if (ImGui.DragFloat("Unit Scale Factor", ref scale))
                {
                    model.SkeletonRoot.UnitScaleFactor = math.max(0, scale);
                    model.CalculateAABB();
                }
            }
            else
            {
                float v = float.NegativeInfinity;
                ImGui.DragFloat("Unit Scale Factor", ref v);
            }
            ImGui.Checkbox("View Skeleton", ref viewBones);
            ImGui.End();
        }

        static void load(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".fbx":
                    model = Model.Load(path, 0.01f);
                    break;
                default:
                    model = Model.Load(path);
                    break;
            }
            if (model != null)
            {
                ResetCamera();
            }
        }

        static void Window_FramebufferResize(Vector2D<int> size)
        {
            GL.Viewport(size);
        }

        static unsafe void Window_FileDrop(WindowHandle* window, int count, nint paths)
        {
            var arrayOfPaths = new string[count];
            if (count == 0 || paths == 0)
            {
                return;
            }
            for (var i = 0; i < count; i++)
            {
                var p = Marshal.ReadIntPtr(paths, i * IntPtr.Size);
                arrayOfPaths[i] = Marshal.PtrToStringUTF8(p);
            }
            if (arrayOfPaths.Length > 1)
            {
                return;
            }
            else
            {
                load(arrayOfPaths[0]);
            }
        }

        static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.F)
            {
                ResetCamera();
            }
        }

        static void onMouseDown(ImGuiMouseButton mouseButton)
        {
        }

        private static void onMouseUp(ImGuiMouseButton mouseButton)
        {
        }

        static void Mouese_Scroll(IMouse mouse, ScrollWheel scrollWheel)
        {
            cameraFOV = math.clamp(cameraFOV - scrollWheel.Y, 1f, 90f);
        }

        static void Window_Closing()
        {
            inputContext.Dispose();
            controller.Dispose();
            linesShader.Dispose();
            model?.Dispose();
        }
    }
}
