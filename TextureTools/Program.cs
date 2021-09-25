using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using GlfwProvider = Silk.NET.GLFW.GlfwProvider;
using WindowHandle = Silk.NET.GLFW.WindowHandle;

namespace TextureTools
{
    class Program
    {
        static IWindow window;

        public static GL GL { get; set; }

        static IInputContext inputContext;
        static ImGuiController controller;

        static BufferObject<float> VBO;
        static BufferObject<uint> EBO;
        static VertexArrayObject<float, uint> VAO;

        static readonly float[] vertices =
        {
            -1.0f, -1.0f,  0.0f,  0.0f,  0.0f,
             1.0f, -1.0f,  0.0f,  1.0f,  0.0f,
            -1.0f,  1.0f,  0.0f,  0.0f,  1.0f,
             1.0f,  1.0f,  0.0f,  1.0f,  1.0f,
        };

        static readonly uint[] indices =
        {
            0, 1, 2,
            1, 3, 2
        };

        static bool srgbOutput = false;

        static string workPath;
        static string[] args;

        //[DllImport("kernel32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool AllocConsole();

        struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetOpenFileNameA(ref OPENFILENAME ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetSaveFileNameA(ref OPENFILENAME lpofn);

        static unsafe void Main(string[] args)
        {
            workPath = AppDomain.CurrentDomain.BaseDirectory;
            Program.args = args;

            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
            Silk.NET.Input.Sdl.SdlInput.RegisterPlatform();

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(768, 768);
            options.Title = "Siesta Texture Tools";
            options.WindowBorder = WindowBorder.Fixed;
            options.IsEventDriven = true;
            window = Window.Create(options);
            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;
            window.Run();
        }

        enum GOBTextureFormat
        {
            BC7,
            BC6H,
            RGTC2,
            RGTC1,
            DXT5,
            DXT3,
            DXT1,
        }

        static string[] outputFormats = {
            "BC7 (RGBA)",
            "BC6H (RGB HDR)",
            "BC5 (RG)",
            "BC4 (R)",
            "DXT5 (RGBA)",
            "DXT3 (RGBA)",
            "DXT1 (RGB)"
        };

        static Texture texture;
        static string texturePath = "";
        static InternalFormat textureFormat;
        static bool switchR = true;
        static bool switchG = true;
        static bool switchB = true;
        static bool switchAlpha = true;
        static bool toLinear = false;
        static bool flipY = false;
        static bool Tonemap = false;
        static GOBTextureFormat outputFormat = GOBTextureFormat.BC7;
        static string defaultExt = ".gobt";

        static Texture checkerboard;
        static Shader checkerboardShader;

        static unsafe void Window_Load()
        {
            GlfwProvider.GLFW.Value
                .SetDropCallback((WindowHandle*)window.Handle, Window_FileDrop);

            var imGuiFontConfig = new ImGuiFontConfig(Resource.simhei, 12);
            controller = new ImGuiController(
                GL = GL.GetApi(window),
                window,
                inputContext = window.CreateInput(),
                imGuiFontConfig);

            var io = ImGui.GetIO();
            io.NativePtr->IniFilename = null;
            if (File.Exists(Path.Combine(workPath, "TextureTools.ini")))
            {
                var iniLines = File.ReadAllLines(Path.Combine(workPath, "TextureTools.ini"));
                foreach (var line in iniLines)
                {
                    var ini = line.Split("=", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    switch (ini[0])
                    {
                        case "R":
                            switchR = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "G":
                            switchG = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "B":
                            switchB = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "Alpha":
                            switchAlpha = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "Linear":
                            toLinear = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "sRGB":
                            srgbOutput = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "FlipY":
                            flipY = Convert.ToBoolean(Convert.ToInt32(ini[1]));
                            break;
                        case "Format":
                            Enum.TryParse(ini[1], out outputFormat);
                            break;
                        case "Ext":
                            defaultExt = ini[1];
                            break;
                    }
                }
            }

            GL.Enable(EnableCap.CullFace);

            EBO = new BufferObject<uint>(indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(VBO, EBO);
            VAO.VertexAttributePointer<float>(0, 3, VertexAttribPointerType.Float, 5, 0);
            VAO.VertexAttributePointer<float>(1, 2, VertexAttribPointerType.Float, 5, 3);
            GL.BindVertexArray(0);

            checkerboard = new Texture(Path.Combine(workPath, "checkerboard.gobt"));
            var vert = System.Text.Encoding.UTF8.GetString(Resource.vert);
            var frag = System.Text.Encoding.UTF8.GetString(Resource.frag_checkerboard);
            checkerboardShader = new Shader(vert, frag);

            if (args.Length == 1)
            {
                if (Directory.Exists(args[0]))
                {
                    recurseFolders(args[0]);
                }
                else
                {
                    if (LoadTexture(args[0]))
                    {
                        texturePath = args[0];
                        Console.WriteLine($"Open:{args[0]}");
                    }
                }
            }
            if (args.Length > 1)
            {
                foreach (var path in args)
                {
                    recurseFolders(path);
                }
            }
        }

        static void recurseFolders(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    recurseFolders(Path.Combine(path, dir));
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    recurseFolders(Path.Combine(path, file));
                }
            }
            else
            {
                if (LoadTexture(path))
                {
                    Console.WriteLine($"Open:{path}");
                    SaveCompressed(Path.ChangeExtension(path, defaultExt));
                }
            }
        }

        static void Window_Update(double deltaTime)
        {
        }

        static int FrameCount = 0;
        static string framesPerSecond = "?";
        static double FrameTimer = 0;

        static void Window_Render(double deltaTime)
        {
            var deltaTimef = (float)deltaTime;
            controller.Update(deltaTimef);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            RenderCheckerboard();

            if (texture != null)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                texture.Bind();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                RenderTexture();
                GL.Disable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            }

            GUI(deltaTimef);
            controller.Render();
        }

        static unsafe void RenderCheckerboard()
        {
            VAO.Bind();
            checkerboardShader.Use();
            checkerboard.Bind(TextureUnit.Texture0);
            checkerboardShader.SetInt("_BaseMap", 0);
            checkerboardShader.SetVector("_TexCoordScale", new float2(
                (float)window.Size.X / checkerboard.Width,
                (float)window.Size.Y / checkerboard.Height));
            GL.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
            GL.BindVertexArray(0);
        }

        static unsafe void RenderTexture()
        {
            VAO.Bind();
            var shader = Shader.Default;
            shader.Use();
            texture.Bind(TextureUnit.Texture0);
            shader.SetInt("_BaseMap", 0);
            shader.SetVector("_Switch", new float4(
                switchR ? 1f : 0f, switchG ? 1f : 0f,
                switchB ? 1f : 0f, switchAlpha ? 1f : 0f));
            shader.SetBool("_FlipY", flipY);
            shader.SetBool("_Tonemap", Tonemap);
            shader.SetBool("_ToLinear", toLinear);
            shader.SetBool("_sRGBOutput", srgbOutput);
            GL.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
            GL.BindVertexArray(0);
        }

        static void GUI(float deltaTime)
        {
            FrameTimer += deltaTime;
            FrameCount++;
            if (FrameTimer > 1)
            {
                FrameTimer -= 1;
                framesPerSecond = FrameCount.ToString();
                FrameCount = 0;
            }
            ImGui.Begin("Tools");
            //ImGui.Text($"FPS:{framesPerSecond}");
            ImGui.Text($"Format:{textureFormat}");
            ImGui.Text($"Width:{texture?.Width}, Height:{texture?.Height}");
            ImGui.Checkbox("Convert texture to linear space (图片转换到线性空间)", ref toLinear);
            ImGui.Checkbox("sRGB output (sRGB输出)", ref srgbOutput);
            if (ImGui.Button("Load"))
            {
                var ofn = new OPENFILENAME();
                ofn.lStructSize = Marshal.SizeOf(ofn);
                ofn.hwndOwner = window.Native.Win32.Value.Hwnd;
                ofn.lpstrFilter = "Texture files (*.png;*.bmp;*.jpg;*.tga;*.dds;*.exr;*.gobt)\0*.png;*.bmp;*.jpg;*.tga;*.dds;*.exr;*.gobt\0\0";
                ofn.lpstrFile = new string(new char[256]);
                ofn.nMaxFile = ofn.lpstrFile.Length;
                ofn.lpstrFileTitle = new string(new char[64]);
                ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
                if (GetOpenFileNameA(ref ofn))
                {
                    if (LoadTexture(ofn.lpstrFile))
                    {
                        texturePath = ofn.lpstrFile;
                        Console.WriteLine($"Open:{texturePath}");
                    }
                }
            }
            ImGui.SameLine();
            ImGui.Text(texturePath);
            if (ImGui.Button("Save as") && texture != null)
            {
                var lpofn = new OPENFILENAME();
                var initPath = new char[256];
                var str = Path.ChangeExtension(texturePath, ".gobt");
                str.CopyTo(0, initPath, 0, str.Length);

                lpofn.lStructSize = Marshal.SizeOf(lpofn);
                lpofn.hwndOwner = window.Native.Win32.Value.Hwnd;
                lpofn.lpstrFilter = "GOB BC7 Texture (*.gobt)\0*.gobt\0TGA Texture (*.tga)\0*.tga\0\0";
                lpofn.lpstrFile = new string(initPath);
                lpofn.nMaxFile = lpofn.lpstrFile.Length;
                lpofn.lpstrFileTitle = new string(new char[64]);
                lpofn.nMaxFileTitle = lpofn.lpstrFileTitle.Length;
                lpofn.Flags = 2;
                if (GetSaveFileNameA(ref lpofn))
                {
                    string filePath;
                    if (Path.HasExtension(lpofn.lpstrFile))
                    {
                        filePath = lpofn.lpstrFile;
                    }
                    else
                    {
                        switch (lpofn.nFilterIndex)
                        {
                            case 1:
                                filePath = Path.ChangeExtension(lpofn.lpstrFile, ".gobt");
                                break;
                            case 2:
                                filePath = Path.ChangeExtension(lpofn.lpstrFile, ".tga");
                                break;
                            default:
                                filePath = lpofn.lpstrFile;
                                break;
                        }
                    }
                    switch (lpofn.nFilterIndex)
                    {
                        case 1:
                            SaveCompressed(filePath);
                            break;
                        case 2:
                        default:
                            Save(filePath);
                            break;
                    }
                }
            }
            ImGui.Checkbox("Flip Y (垂直翻转)", ref flipY);
            ImGui.Checkbox("R", ref switchR); ImGui.SameLine();
            ImGui.Checkbox("G", ref switchG); ImGui.SameLine();
            ImGui.Checkbox("B", ref switchB); ImGui.SameLine();
            ImGui.Checkbox("Alpha", ref switchAlpha); ImGui.SameLine();
            ImGui.Checkbox("HDR Tone Map", ref Tonemap);
            if (ImGui.BeginCombo("GOBT format (GOBT格式)", outputFormats[(int)outputFormat]))
            {
                for (int i = 0; i < outputFormats.Length; i++)
                {
                    var format = outputFormats[i];
                    if (ImGui.Selectable(format))
                    {
                        outputFormat = (GOBTextureFormat)i;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.TextDisabled("Powered by DarkFaMaster/GameOldBoy");
            ImGui.End();
        }

        static unsafe bool LoadTexture(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            try
            {
                uint width, height;
                Texture tempTex;
                switch (ext)
                {
                    case ".gobt":
                        tempTex = new Texture(path);
                        textureFormat = tempTex.Format;
                        break;
                    case ".dds":
                        var config = new Pfim.PfimConfig(decompress: false);
                        using (Pfim.CompressedDds dds = (Pfim.CompressedDds)Pfim.Pfim
                            .FromFile(path, config))
                        {
                            InternalFormat internalFormat;
                            int bytesPerBlock;
                            switch (dds.Header.PixelFormat.FourCC)
                            {
                                case Pfim.CompressionAlgorithm.D3DFMT_DXT1:
                                    internalFormat = InternalFormat.CompressedRgbS3TCDxt1Ext;
                                    bytesPerBlock = 8;
                                    break;
                                case Pfim.CompressionAlgorithm.D3DFMT_DXT2:
                                case Pfim.CompressionAlgorithm.D3DFMT_DXT3:
                                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt3Ext;
                                    bytesPerBlock = 16;
                                    break;
                                case Pfim.CompressionAlgorithm.D3DFMT_DXT4:
                                case Pfim.CompressionAlgorithm.D3DFMT_DXT5:
                                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt5Ext;
                                    bytesPerBlock = 16;
                                    break;
                                case Pfim.CompressionAlgorithm.ATI1:
                                case Pfim.CompressionAlgorithm.BC4U:
                                    internalFormat = InternalFormat.CompressedRedRgtc1;
                                    bytesPerBlock = 8;
                                    break;
                                case Pfim.CompressionAlgorithm.BC4S:
                                    internalFormat = InternalFormat.CompressedSignedRedRgtc1;
                                    bytesPerBlock = 8;
                                    break;
                                case Pfim.CompressionAlgorithm.ATI2:
                                case Pfim.CompressionAlgorithm.BC5U:
                                    internalFormat = InternalFormat.CompressedRGRgtc2;
                                    bytesPerBlock = 16;
                                    break;
                                case Pfim.CompressionAlgorithm.BC5S:
                                    internalFormat = InternalFormat.CompressedSignedRGRgtc2;
                                    bytesPerBlock = 16;
                                    break;
                                case Pfim.CompressionAlgorithm.DX10:
                                    switch (dds.Header10.DxgiFormat)
                                    {
                                        case Pfim.DxgiFormat.BC1_UNORM:
                                            internalFormat = InternalFormat.CompressedRgbaS3TCDxt1Ext;
                                            bytesPerBlock = 8;
                                            break;
                                        case Pfim.DxgiFormat.BC1_UNORM_SRGB:
                                            internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext;
                                            bytesPerBlock = 8;
                                            break;
                                        case Pfim.DxgiFormat.BC2_UNORM:
                                            internalFormat = InternalFormat.CompressedRgbaS3TCDxt3Ext;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC2_UNORM_SRGB:
                                            internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC3_UNORM:
                                            internalFormat = InternalFormat.CompressedRgbaS3TCDxt5Ext;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC3_UNORM_SRGB:
                                            internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC4_UNORM:
                                            internalFormat = InternalFormat.CompressedRedRgtc1;
                                            bytesPerBlock = 8;
                                            break;
                                        case Pfim.DxgiFormat.BC4_SNORM:
                                            internalFormat = InternalFormat.CompressedSignedRedRgtc1;
                                            bytesPerBlock = 8;
                                            break;
                                        case Pfim.DxgiFormat.BC5_UNORM:
                                            internalFormat = InternalFormat.CompressedRGRgtc2;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC5_SNORM:
                                            internalFormat = InternalFormat.CompressedSignedRGRgtc2;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC6H_UF16:
                                            internalFormat = InternalFormat.CompressedRgbBptcUnsignedFloat;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC6H_SF16:
                                            internalFormat = InternalFormat.CompressedRgbBptcSignedFloat;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC7_UNORM:
                                            internalFormat = InternalFormat.CompressedRgbaBptcUnorm;
                                            bytesPerBlock = 16;
                                            break;
                                        case Pfim.DxgiFormat.BC7_UNORM_SRGB:
                                            internalFormat = InternalFormat.CompressedSrgbAlphaBptcUnorm;
                                            bytesPerBlock = 16;
                                            break;
                                        default:
                                            Console.WriteLine($"Unacceptable format: Header10 > {dds.Header10.DxgiFormat}");
                                            return false;
                                    }
                                    break;
                                default:
                                    Console.WriteLine($"Unacceptable format: Header > {dds.Header.PixelFormat.FourCC}");
                                    return false;
                            }
                            tempTex = new Texture(
                                dds.Data, (uint)dds.Width, (uint)dds.Height,
                                internalFormat, (int)dds.Header.MipMapCount, bytesPerBlock);
                            textureFormat = tempTex.Format;
                        }
                        break;
                    case ".png":
                    case ".bmp":
                    case ".jpg":
                    case ".tga":
                        using (var img = (Image<Rgba32>)Image.Load(path))
                        {
                            img.Mutate(x => x.Flip(FlipMode.Vertical));
                            width = (uint)img.Width;
                            height = (uint)img.Height;
                            textureFormat = InternalFormat.Rgba;
                            if (img.TryGetSinglePixelSpan(out var pixelSpan))
                            {
                                tempTex = new Texture(MemoryMarshal.AsBytes(pixelSpan).ToArray(), width, height);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case ".exr":
                        var exrFile = SharpEXR.EXRFile.FromFile(path);
                        var part = exrFile.Parts[0];
                        width = (uint)part.DataWindow.Width;
                        height = (uint)part.DataWindow.Height;
                        textureFormat = InternalFormat.Rgb32f;
                        part.OpenParallel(path);
                        tempTex = new Texture(
                            part.GetFloats(SharpEXR.ChannelConfiguration.RGB, false,
                                SharpEXR.GammaEncoding.Linear, false), width, height);
                        part.Close();
                        break;
                    default:
                        return false;
                }
                texture?.Dispose();
                texture = tempTex;
                var aspect = (float)texture.Width / texture.Height;
                height = 768;
                width = (uint)(aspect * height);
                window.Size = new Vector2D<int>((int)width, (int)height);
                GL.Viewport(0, 0, width, height);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        static unsafe void GetTextureData(out byte[] data)
        {
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureWidth, out int texWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureHeight, out int texHeight);
            data = new byte[texWidth * texHeight * 4];
            fixed (void* d = &data[0])
            {
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        static unsafe void GetTextureData(out float[] data)
        {
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureWidth, out int texWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GLEnum.TextureHeight, out int texHeight);
            data = new float[texWidth * texHeight * 3];
            fixed (void* d = &data[0])
            {
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.Float, d);
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        static unsafe void Save(string filePath)
        {
            Console.WriteLine("Saving...");
            if (texture.Format == InternalFormat.Rgb32f)
            {
                Console.WriteLine("TGA cannot save HDR colors");
                return;
            }

            var width = (uint)texture.Width;
            var height = (uint)texture.Height;

            var _rb = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rb);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Rgba32f, width, height);

            var _fb = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fb);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _rb);

            GL.Viewport(0, 0, width, height);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            var hdr = Tonemap;
            Tonemap = false;
            RenderTexture();
            Tonemap = hdr;
            GL.Viewport(0, 0, (uint)window.Size.X, (uint)window.Size.Y);

            var _tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            GL.CopyTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, 0, 0, width, height, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.DeleteRenderbuffer(_rb);
            GL.DeleteFramebuffer(_fb);

            var textureData = new byte[width * height * 4];
            fixed (void* d = &textureData[0])
            {
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DeleteTexture(_tex);

            using (var img = Image.LoadPixelData<Rgba32>(
                 textureData, texture.Width, texture.Height))
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));
                img.Save(Path.ChangeExtension(filePath, ".tga"), new TgaEncoder()
                {
                    BitsPerPixel = TgaBitsPerPixel.Pixel32,
                    Compression = TgaCompression.RunLength
                });
            }

            Console.WriteLine("Save Finish");
        }

        static unsafe void SaveCompressed(string filePath)
        {
            Console.WriteLine("Saving...");

            var width = (uint)texture.Width;
            var height = (uint)texture.Height;

            InternalFormat compressedFormat;
            switch (outputFormat)
            {
                default:
                case GOBTextureFormat.BC7:
                    compressedFormat = InternalFormat.CompressedRgbaBptcUnorm;
                    break;
                case GOBTextureFormat.BC6H:
                    compressedFormat = InternalFormat.CompressedRgbBptcUnsignedFloat;
                    break;
                case GOBTextureFormat.RGTC2:
                    compressedFormat = InternalFormat.CompressedRGRgtc2;
                    break;
                case GOBTextureFormat.RGTC1:
                    compressedFormat = InternalFormat.CompressedRedRgtc1;
                    break;
                case GOBTextureFormat.DXT5:
                    compressedFormat = InternalFormat.CompressedRgbaS3TCDxt5Ext;
                    break;
                case GOBTextureFormat.DXT3:
                    compressedFormat = InternalFormat.CompressedRgbaS3TCDxt3Ext;
                    break;
                case GOBTextureFormat.DXT1:
                    compressedFormat = InternalFormat.CompressedRgbS3TCDxt1Ext;
                    break;
            }

            var _rb = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rb);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Rgba32f, width, height);

            var _fb = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fb);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _rb);

            GL.Viewport(0, 0, width, height);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            var hdr = Tonemap;
            Tonemap = false;
            RenderTexture();
            Tonemap = hdr;
            GL.Viewport(0, 0, (uint)window.Size.X, (uint)window.Size.Y);

            var _tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _tex);
            if (outputFormat == GOBTextureFormat.BC6H)
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, compressedFormat,
                    width, height, 0, PixelFormat.Rgb, PixelType.Float, null);
            }
            else
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, compressedFormat,
                    width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            }
            GL.CopyTexImage2D(TextureTarget.Texture2D, 0, compressedFormat, 0, 0, width, height, 0);
            GL.GenerateMipmap(TextureTarget.Texture2D);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.DeleteRenderbuffer(_rb);
            GL.DeleteFramebuffer(_fb);

            using (var texStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(texStream))
            {
                GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureInternalFormat, out int format);
                Console.WriteLine($"format:{(GLEnum)format}");
                // 魔法数
                writer.Write(0x54424F47);
                // 格式
                writer.Write(format);
                // num of mipmap
                writer.Write(0);

                int maxLevel = 0;
                for (int i = 0; i < 1000; i++)
                {
                    maxLevel = i;
                    GL.GetTexLevelParameter(TextureTarget.Texture2D, i, GLEnum.TextureWidth, out int texWidth);
                    GL.GetTexLevelParameter(TextureTarget.Texture2D, i, GLEnum.TextureHeight, out int texHeight);
                    GL.GetTexLevelParameter(TextureTarget.Texture2D, i, GLEnum.TextureCompressedImageSize, out int compressed_size);
                    if (texWidth == 0 || texHeight == 0)
                    {
                        break;
                    }
                    Console.WriteLine($"level:{i}, textureSize:{texWidth},{texHeight}, compressed_size:{compressed_size}");
                    // 宽
                    writer.Write(texWidth);
                    // 高
                    writer.Write(texHeight);
                    // 数据长度
                    writer.Write(compressed_size);
                    // 字节数组
                    byte[] data = new byte[compressed_size];
                    fixed (void* d = &data[0])
                    {
                        GL.GetCompressedTexImage(TextureTarget.Texture2D, i, d);
                    }
                    writer.Write(data);
                }

                texStream.Position = 8;
                writer.Write(maxLevel);
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DeleteTexture(_tex);

            Console.WriteLine("Save Finish");
        }

        static void Window_Closing()
        {
            inputContext.Dispose();
            controller.Dispose();
            VBO.Dispose();
            EBO.Dispose();
            VAO.Dispose();
            texture?.Dispose();
            Shader.Default.Dispose();

            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendLine($"R = {Convert.ToInt32(switchR)}");
            stringBuilder.AppendLine($"G = {Convert.ToInt32(switchG)}");
            stringBuilder.AppendLine($"B = {Convert.ToInt32(switchB)}");
            stringBuilder.AppendLine($"Alpha = {Convert.ToInt32(switchAlpha)}");
            stringBuilder.AppendLine($"Linear = {Convert.ToInt32(toLinear)}");
            stringBuilder.AppendLine($"sRGB = {Convert.ToInt32(srgbOutput)}");
            stringBuilder.AppendLine($"FlipY = {Convert.ToInt32(flipY)}");
            stringBuilder.AppendLine($"Format = {outputFormat}");
            stringBuilder.AppendLine($"Ext = {defaultExt}");
            File.WriteAllText(Path.Combine(workPath, "TextureTools.ini"), stringBuilder.ToString());
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
                if (LoadTexture(arrayOfPaths[0]))
                {
                    texturePath = arrayOfPaths[0];
                    Console.WriteLine($"Open:{arrayOfPaths[0]}");
                }
            }
        }
    }
}