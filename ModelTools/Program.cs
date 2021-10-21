using ImGuiNET;
using ModelTools.Animation;
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
using System.Numerics;
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
            var api = options.API;
            var version = api.Version;
            version.MajorVersion = 4;
            version.MinorVersion = 5;
            api.Version = version;
            options.API = api;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "Siesta Model Tools";
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 0);
            options.PreferredDepthBufferBits = 24;
            options.PreferredStencilBufferBits = 0;
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
        static int linesViewPosLocation;
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
            linesViewPosLocation = linesShader.GetUniformLocation("_ViewPosWS");
            linesColorLocation = linesShader.GetUniformLocation("_BaseColor");

            var debugAxisX = new float3[] { new float3(-500f, 0, 0), new float3(500f, 0, 0) };
            var debugAxisY = new float3[] { new float3(0, -500, 0), new float3(0, 500f, 0) };
            var debugAxisZ = new float3[] { new float3(0, 0, -500f), new float3(0, 0, 500f) };
            var debugGrid = new float3[4000]; // x1000条，y1000条，每条线2个float3，1000 * 2 * 2
            for (int i = 1; i <= 500; i++)
            {
                var axisXPos = new float3[] { debugAxisX[0] + new float3(0, 0, i), debugAxisX[1] + new float3(0, 0, i) };
                var axisXNeg = new float3[] { debugAxisX[0] + new float3(0, 0, -i), debugAxisX[1] + new float3(0, 0, -i) };
                var axisZPos = new float3[] { debugAxisZ[0] + new float3(i, 0, 0), debugAxisZ[1] + new float3(i, 0, 0) };
                var axisZNeg = new float3[] { debugAxisZ[0] + new float3(-i, 0, 0), debugAxisZ[1] + new float3(-i, 0, 0) };
                var index = (i - 1) * 8;
                debugGrid[index] = axisXPos[0];
                debugGrid[index + 1] = axisXPos[1];
                debugGrid[index + 2] = axisXNeg[0];
                debugGrid[index + 3] = axisXNeg[1];
                debugGrid[index + 4] = axisZPos[0];
                debugGrid[index + 5] = axisZPos[1];
                debugGrid[index + 6] = axisZNeg[0];
                debugGrid[index + 7] = axisZNeg[1];
            }
            debugAxisXVAO = new VertexArrayObject();
            debugAxisYVAO = new VertexArrayObject();
            debugAxisZVAO = new VertexArrayObject();
            debugGridVAO = new VertexArrayObject();
            debugAxisXVBO = new BufferObject<float3>(BufferTargetARB.ArrayBuffer);
            debugAxisYVBO = new BufferObject<float3>(BufferTargetARB.ArrayBuffer);
            debugAxisZVBO = new BufferObject<float3>(BufferTargetARB.ArrayBuffer);
            debugGridVBO = new BufferObject<float3>(BufferTargetARB.ArrayBuffer);
            debugAxisXVAO.Bind();
            debugAxisXVBO.Bind();
            debugAxisXVBO.BufferData(debugAxisX);
            debugAxisXVAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, (uint)sizeof(float3), 0);
            debugAxisYVAO.Bind();
            debugAxisYVBO.Bind();
            debugAxisYVBO.BufferData(debugAxisY);
            debugAxisYVAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, (uint)sizeof(float3), 0);
            debugAxisZVAO.Bind();
            debugAxisZVBO.Bind();
            debugAxisZVBO.BufferData(debugAxisZ);
            debugAxisZVAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, (uint)sizeof(float3), 0);
            debugGridVAO.Bind();
            debugGridVBO.Bind();
            debugGridVBO.BufferData(debugGrid);
            debugGridVAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, (uint)sizeof(float3), 0);
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            skinningIndices = new Dictionary<Bone, int>();
            skinningMatrices = new float4x4[0];
            skinningSSBO = new BufferObject<float4x4>(BufferTargetARB.ShaderStorageBuffer);

            // z范围 0-1
            GL.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);
            // 反向z
            GL.ClearDepth(0);
            GL.DepthFunc(DepthFunction.Greater);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.ClearColor(0.4f, 0.5f, 0.6f, 1f);

            ResetCamera();

            var version = GL.GetStringS(GLEnum.Version);
            Console.WriteLine($"OpenGL Version:{version}");
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

        static float2 mousePos;
        static float2 mouseDelta;
        static bool mouseLB;
        static bool mouseMB;
        static bool mouseRB;
        static bool altKey;

        static float4x4 invViewProj;
        static Model model;
        static TimeLine timeLine;
        static Dictionary<Bone, int> skinningIndices;
        static float4x4[] skinningMatrices;
        static float3[] bonesVertices;

        static void Window_Update(double dt)
        {
            var deltatime = (float)dt;
            controller.Update(deltatime);

            var io = ImGui.GetIO();
            if (!io.WantCaptureMouse)
            {
                var lastMousePos = mousePos;
                mousePos = MathHelper.ToFloat2(ImGui.GetMousePos());
                mouseDelta = mousePos - lastMousePos;
                var isMouseLBDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
                var isMouseMBDown = ImGui.IsMouseDown(ImGuiMouseButton.Middle);
                var isMouseRBDown = ImGui.IsMouseDown(ImGuiMouseButton.Right);
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
            }
            else
            {
                mousePos = MathHelper.ToFloat2(ImGui.GetMousePos());
                mouseDelta = float2.zero;
                mouseLB = false;
                mouseMB = false;
                mouseRB = false;
            }
            var isAltKeyDown = ImGui.IsKeyDown((int)Key.AltLeft);
            if (altKey != isAltKeyDown)
            {
                altKey = isAltKeyDown;
            }

            BoundingBox aabb;
            if (model != null)
            {
                aabb = model.AABB;
            }
            else
            {
                aabb = new BoundingBox(10f);
            }

            if (mouseLB)
            {
                cameraPos = aabb.center;
                cameraYaw = (cameraYaw - mouseDelta.x) % 360f;
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
                var forward = mouseDelta.y * 0.002f * cameraDistance;
                cameraDistance = math.max(0.1f, cameraDistance + forward);
                cameraPos = aabb.center;
                cameraPos += (-cameraForward) * cameraDistance;
            }

            if (model != null)
            {
                updateSkinnig(dt);
            }
        }

        static void updateSkinnig(double deltaTime)
        {
            if (skinningMatrices.Length != model.Bones.Length)
            {
                skinningIndices.Clear();
                skinningMatrices = new float4x4[model.Bones.Length];
                for (int i = 0; i < model.Bones.Length; i++)
                {
                    var bone = model.Bones[i];
                    skinningIndices.Add(bone, i);
                }
                bonesVertices = new float3[model.Bones.Length * 2];
            }
            if (isPlaying && timeLine != null)
            {
                timeLine.Update(deltaTime);
            }
            for (int i = 0; i < model.Bones.Length; i++)
            {
                var bone = model.Bones[i];
                if (timeLine != null)
                {
                    skinningMatrices[i] = math.mul(bone.CalculateObjectSpaceMatrix(timeLine.Results), bone.offset);
                }
                else
                {
                    skinningMatrices[i] = math.mul(bone.CalculateObjectSpaceMatrix(), bone.offset);
                }
            }
        }

        static void ResetCamera()
        {
            cameraFOV = 45f;
            float fovRad = 0.5f * cameraFOV * MathHelper.Deg2Rad;
            BoundingBox aabb;
            if (model != null)
            {
                aabb = model.AABB;
            }
            else
            {
                aabb = new BoundingBox(10f);
            }
            float halfWidth = (aabb.right - aabb.left) * 0.5f;
            float halfHight = (aabb.top - aabb.bottom) * 0.5f;
            float halfLength = (aabb.front - aabb.back) * 0.5f;
            float longest = math.max(halfWidth, halfHight);
            cameraDistance = math.max(0.1f, longest / math.tan(fovRad) + halfLength);
            cameraPos = aabb.center;
            cameraPosOffset = float3.zero;
            cameraYaw = 180f;
            cameraPitch = 0;
            cameraPos += (-cameraForward) * cameraDistance;
        }

        static bool viewBones;
        static VertexArrayObject debugAxisXVAO;
        static VertexArrayObject debugAxisYVAO;
        static VertexArrayObject debugAxisZVAO;
        static VertexArrayObject debugGridVAO;
        static BufferObject<float3> debugAxisXVBO;
        static BufferObject<float3> debugAxisYVBO;
        static BufferObject<float3> debugAxisZVBO;
        static BufferObject<float3> debugGridVBO;
        static BufferObject<float4x4> skinningSSBO;

        static void Window_Render(double dt)
        {
            var deltatime = (float)dt;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            var renderingData = createRenderingData();
            GL.Enable(EnableCap.FramebufferSrgb);
            if (model != null)
            {
                renderModel(renderingData);
                if (viewBones)
                {
                    GL.Disable(EnableCap.FramebufferSrgb);
                    drawBone(renderingData);
                }
            }

            GL.Disable(EnableCap.FramebufferSrgb);
            drawDebugAxisGrid(renderingData);

            onGUI(deltatime);
            controller.Render();

            var error = GL.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"GL Error:{error}");
            }
        }

        static RenderingData createRenderingData()
        {
            RenderingData renderingData;
            renderingData.viewMatrix = float4x4.TRS(
                cameraPos + cameraPosOffset,
                MathHelper.FromEuler(cameraPitch * MathHelper.Deg2Rad, cameraYaw * MathHelper.Deg2Rad, 0),
                new float3(-1f, 1f, 1f));
            renderingData.viewMatrix = math.inverse(renderingData.viewMatrix);
            BoundingBox aabb;
            if (model != null)
            {
                aabb = model.AABB;
            }
            else
            {
                aabb = new BoundingBox(10f);
            }
            var nearClip = math.min(0.1f, cameraDistance);
            var farClip =
                math.max(math.max(math.max(
                aabb.right - aabb.left,
                aabb.top - aabb.bottom),
                aabb.front - aabb.back)
                * 0.5f + cameraDistance, 100f);
            //Console.WriteLine($"cameraDistance:{cameraDistance}, nearClip:{nearClip}, farClip:{farClip}");
            //renderingData.projectionMatrix =
            //    MathHelper.PerspectiveFov(cameraFOV, (uint)window.Size.X, (uint)window.Size.Y, 0.1f, 100f);
            renderingData.projectionMatrix =
                MathHelper.ReversedZPerspectiveFov(cameraFOV, (uint)window.Size.X, (uint)window.Size.Y, nearClip, farClip);
            invViewProj = math.inverse(math.mul(renderingData.projectionMatrix, renderingData.viewMatrix));
            renderingData.cameraPosition = cameraPos + cameraPosOffset;
            renderingData.mainLightDirection = -cameraForward;
            return renderingData;
        }

        static void drawDebugAxisGrid(RenderingData renderingData)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            linesShader.Use();
            linesShader.SetMatrix(linesModelMatrixLocation, float4x4.identity);
            linesShader.SetMatrix(linesViewMatrixLocation, renderingData.viewMatrix);
            linesShader.SetMatrix(linesProjectionMatrixLocation, renderingData.projectionMatrix);
            linesShader.SetVector(linesViewPosLocation, renderingData.cameraPosition);
            debugGridVAO.Bind();
            linesShader.SetVector(linesColorLocation, new float4(1f, 1f, 1f, 0.2f));
            GL.DrawArrays(PrimitiveType.Lines, 0, 12000);
            debugAxisXVAO.Bind();
            linesShader.SetVector(linesColorLocation, new float4(1f, 0, 0, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 0, 6);
            debugAxisYVAO.Bind();
            linesShader.SetVector(linesColorLocation, new float4(0, 1f, 0, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 0, 6);
            debugAxisZVAO.Bind();
            linesShader.SetVector(linesColorLocation, new float4(0, 0, 1f, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 0, 6);
            GL.Disable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GL.UseProgram(0);
        }

        static void renderModel(RenderingData renderingData)
        {
            skinningSSBO.Bind();
            skinningSSBO.BufferData(skinningMatrices, BufferUsageARB.DynamicDraw);
            skinningSSBO.BindBufferBase(0);
            for (int i = 0; i < model.Meshes.Length; i++)
            {
                var mesh = model.Meshes[i];
                var material = model.Materials[i % model.Materials.Length];
                DrawData drawData;
                drawData.material = material;
                drawData.modelMatrix = float4x4.identity;
                mesh.Draw(drawData, renderingData);
            }
            GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
            GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, 0);
        }

        static unsafe void drawBone(RenderingData renderingData)
        {
            // 更新骨骼顶点
            for (int i = 0; i < model.Bones.Length; i++)
            {
                var bone = model.Bones[i];
                if (bone.parent == null || bone.parent == model.SkeletonRoot)
                {
                    bonesVertices[i * 2] = new float3(float.NaN);
                    bonesVertices[i * 2 + 1] = new float3(float.NaN);
                    continue;
                }
                float3 pos, posParent;
                if (timeLine != null)
                {
                    pos = math.mul(bone.CalculateObjectSpaceMatrix(timeLine.Results), new float4(0, 0, 0, 1f)).xyz;
                    posParent = math.mul(bone.parent.CalculateObjectSpaceMatrix(timeLine.Results), new float4(0, 0, 0, 1f)).xyz;
                }
                else
                {
                    pos = math.mul(bone.CalculateObjectSpaceMatrix(), new float4(0, 0, 0, 1f)).xyz;
                    posParent = math.mul(bone.parent.CalculateObjectSpaceMatrix(), new float4(0, 0, 0, 1f)).xyz;
                }
                bonesVertices[i * 2] = pos;
                bonesVertices[i * 2 + 1] = posParent;
            }
            // 画骨骼
            if (bonesVertices.Length > 0)
            {
                var vao = new VertexArrayObject();
                vao.Bind();
                var vbo = new BufferObject<float3>(BufferTargetARB.ArrayBuffer);
                vbo.Bind();
                vbo.BufferData(bonesVertices);
                vao.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, (uint)sizeof(float3), 0);
                linesShader.Use();
                linesShader.SetMatrix(linesModelMatrixLocation, float4x4.identity);
                linesShader.SetMatrix(linesViewMatrixLocation, renderingData.viewMatrix);
                linesShader.SetMatrix(linesProjectionMatrixLocation, renderingData.projectionMatrix);
                linesShader.SetVector(linesViewPosLocation, renderingData.cameraPosition);
                linesShader.SetVector(linesColorLocation, new float4(0, 1f, 0, 1f));
                GL.Disable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.DrawArrays(PrimitiveType.Lines, 0, (uint)bonesVertices.Length * 3);
                GL.Enable(EnableCap.DepthTest);
                GL.Disable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
                GL.UseProgram(0);
                vao.Dispose();
                vbo.Dispose();
            }
        }

        static bool isPlaying = true;
        static double adjustFPS;
        static string confirmFunc;

        static void onGUI(float dt)
        {
            ImGui.SetNextWindowSize(new Vector2(200f, 0), ImGuiCond.Once);
            ImGui.Begin("Tools");
            if (ImGui.Button("Open"))
            {
                var ofn = new OPENFILENAME();
                ofn.lStructSize = Marshal.SizeOf(ofn);
                ofn.hwndOwner = window.Native.Win32.Value.Hwnd;
                ofn.lpstrFilter = "Model files (*.obj;*.fbx;*.dae)\0*.obj;*.fbx;*.dae\0\0";
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
                ImGui.PushItemWidth(-110f);
                if (ImGui.DragFloat("Unit Scale Factor", ref scale, 0.001f, 0))
                {
                    model.SkeletonRoot.UnitScaleFactor = math.max(0, scale);
                    model.CalculateAABB();
                }
            }
            else
            {
                float v = 0;
                ImGui.PushItemWidth(-110f);
                ImGui.DragFloat("Unit Scale Factor", ref v);
            }
            ImGui.Checkbox("View Skeleton", ref viewBones);
            ImGui.End();

            ImGui.SetNextWindowSize(new Vector2(window.Size.X - 40f, 160f), ImGuiCond.Once);
            ImGui.SetNextWindowPos(new Vector2(window.Size.X * 0.5f, window.Size.Y - 20f), ImGuiCond.Once, new Vector2(0.5f, 1f));
            ImGui.Begin("Animation");
            ImGui.Checkbox("Play", ref isPlaying);
            if (timeLine != null)
            {
                ImGui.Text($"FPS:{timeLine.Animation.FramesPerSecond}");
                var time = (float)timeLine.Position;
                var duration = (float)timeLine.Animation.Duration - 0.0001f;
                ImGui.PushItemWidth(-40f);
                if (ImGui.SliderFloat("Time", ref time, 0, duration))
                {
                    timeLine.Seek(math.clamp(time, 0, duration));
                }
                var speed = timeLine.Speed;
                ImGui.PushItemWidth(100f);
                if (ImGui.DragFloat("Speed", ref speed, 0.001f))
                {
                    timeLine.Speed = speed;
                }
                var currentFrames = (int)timeLine.CurrentFrames;
                ImGui.SameLine();
                ImGui.PushItemWidth(-40f);
                if (ImGui.DragInt("Frames", ref currentFrames, 0.1f))
                {
                    timeLine.SeekFrames(currentFrames);
                }
                ImGui.PushItemWidth(0);
                if (ImGui.Button("Adjust FPS"))
                {
                    adjustFPS = timeLine.Animation.FramesPerSecond;
                    ImGui.OpenPopup("Adjust FPS");
                }
                ImGui.SameLine();
                if (ImGui.Button("Align all keys by Integer"))
                {
                    confirmFunc = "AlignAllKeysByInteger";
                    ImGui.OpenPopup("confirm?");
                }
            }

            var open = true;
            ImGui.SetNextWindowSize(new Vector2(160f, 0), ImGuiCond.Once);
            if (ImGui.BeginPopupModal("Adjust FPS", ref open))
            {
                ImGui.PushItemWidth(-80f);
                ImGui.InputDouble("Adjusted FPS", ref adjustFPS);
                if (ImGui.Button("Adjust"))
                {
                    timeLine.Animation.AdjustFramesPerSecond(adjustFPS);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal("confirm?", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (ImGui.Button("OK"))
                {
                    switch (confirmFunc)
                    {
                        case "AlignAllKeysByInteger":
                            timeLine.Animation.AlignAllKeysByInteger();
                            break;
                    }
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.End();
        }

        static void load(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".fbx":
                    model = Model.Load(path, "fbx", 0.01f);
                    break;
                default:
                    model = Model.Load(path, "obj");
                    break;
            }
            if (model != null)
            {
                if (model.Animations.Length > 0)
                {
                    timeLine = new TimeLine(model.Animations[0]);
                }
                else
                {
                    timeLine = null;
                }
                ResetCamera();
                updateSkinnig(0);
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
            debugAxisXVAO.Dispose();
            debugAxisYVAO.Dispose();
            debugAxisZVAO.Dispose();
            debugGridVAO.Dispose();
            debugAxisXVBO.Dispose();
            debugAxisYVBO.Dispose();
            debugAxisZVBO.Dispose();
            debugGridVBO.Dispose();
            skinningSSBO.Dispose();
        }
    }
}
