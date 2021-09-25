using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Buffer = System.Buffer;

namespace TextureTools
{
    public class Texture : IDisposable
    {
        uint _handle;

        const int maxAnisotropy = 16;

        public InternalFormat Format { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public unsafe Texture(string path, InternalFormat internalFormat = InternalFormat.Rgba)
        {
            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".gobt":
                    var surfaces = readGOBTexture(path);
                    Load(surfaces, Format);
                    return;

                default:
                    var img = (Image<Rgba32>)Image.Load(path);
                    Width = img.Width;
                    Height = img.Height;
                    Format = internalFormat;

                    fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                    {
                        Load(data, (uint)img.Width, (uint)img.Height, internalFormat);
                    }
                    img.Dispose();
                    return;
            }
        }

        public unsafe Texture(byte[] buffer, InternalFormat internalFormat = InternalFormat.Rgba)
        {
            var img = (Image<Rgba32>)Image.Load(buffer);
            Width = img.Width;
            Height = img.Height;
            Format = internalFormat;

            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
            {
                Load(data, (uint)img.Width, (uint)img.Height, internalFormat);
            }
            img.Dispose();
        }

        public unsafe Texture(Span<byte> data, uint width, uint height, InternalFormat internalFormat = InternalFormat.Rgba)
        {
            Width = (int)width;
            Height = (int)height;
            Format = internalFormat;

            fixed (void* d = &data[0])
            {
                Load(d, width, height, internalFormat);
            }
        }

        public unsafe Texture(Span<byte> data, uint width, uint height, InternalFormat internalFormat, int maxLevel, int bytesPerBlock)
        {
            Width = (int)width;
            Height = (int)height;
            Format = internalFormat;

            var surfaces = readDDSTexture(data, maxLevel, bytesPerBlock);
            Load(surfaces, Format);
        }

        public unsafe Texture(Span<float> data, uint width, uint height)
        {
            Width = (int)width;
            Height = (int)height;
            Format = InternalFormat.Rgb32f;

            fixed (void* d = &data[0])
            {
                Load(d, width, height);
            }
        }

        struct Surface
        {
            public int width;
            public int height;
            public byte[] data;
        }

        List<Surface> readDDSTexture(Span<byte> data, int maxLevel, int bytesPerBlock)
        {
            var w = Width;
            var h = Height;
            var offset = 0;
            if (
                Format == InternalFormat.CompressedRgbaS3TCDxt1Ext ||
                Format == InternalFormat.CompressedRgbS3TCDxt1Ext ||
                Format == InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext ||
                Format == InternalFormat.CompressedSrgbS3TCDxt1Ext ||
                Format == InternalFormat.CompressedRedRgtc1 ||
                Format == InternalFormat.CompressedSignedRedRgtc1)
            {
                bytesPerBlock = 8;
            }
            List<Surface> surfaces = new List<Surface>();
            for (int i = 0; i < maxLevel; i++)
            {
                Surface surface;
                surface.width = w;
                surface.height = h;
                if (i == 0)
                {
                    Width = surface.width;
                    Height = surface.height;
                }
                var dataSize = Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4) * bytesPerBlock;
                surface.data = data.Slice(offset, dataSize).ToArray();
                surfaces.Add(surface);

                w = w / 2;
                h = h / 2;
                offset += dataSize;
            }
            return surfaces;
        }

        List<Surface> readGOBTexture(string path)
        {
            List<Surface> surfaces = new List<Surface>();
            using (var texStream = new FileStream(path, FileMode.Open))
            using (var reader = new BinaryReader(texStream))
            {
                if (reader.ReadInt32() != 0x54424F47)
                {
                    throw new Exception("GOB Texture format magic number incorrect");
                }
                Format = (InternalFormat)reader.ReadInt32();
                int maxLevel = reader.ReadInt32();
                for (int i = 0; i < maxLevel; i++)
                {
                    Surface surface;
                    surface.width = reader.ReadInt32();
                    surface.height = reader.ReadInt32();
                    if (i == 0)
                    {
                        Width = surface.width;
                        Height = surface.height;
                    }
                    var dataSize = reader.ReadInt32();
                    surface.data = reader.ReadBytes(dataSize);
                    surfaces.Add(surface);
                }
            }
            return surfaces;
        }

        unsafe void Load(List<Surface> surfaces, InternalFormat internalFormat)
        {
            _handle = Program.GL.GenTexture();
            Bind();

            for (int i = 0; i < surfaces.Count; i++)
            {
                var surface = surfaces[i];
                fixed (void* d = &surface.data[0])
                {
                    Program.GL.CompressedTexImage2D(TextureTarget.Texture2D, i, internalFormat,
                        (uint)surface.width, (uint)surface.height, 0, (uint)surfaces[i].data.Length, d);
                }
            }
        }

        unsafe void Load(void* data, uint width, uint height)
        {
            _handle = Program.GL.GenTexture();
            Bind();

            Program.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb32f,
                width, height, 0, PixelFormat.Rgb, PixelType.Float, data);
            Program.GL.GenerateMipmap(TextureTarget.Texture2D);
        }

        unsafe void Load(void* data, uint width, uint height, InternalFormat internalFormat)
        {
            _handle = Program.GL.GenTexture();
            Bind();

            Program.GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            //Program.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            //Program.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            //Program.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            //Program.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            //Program.GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxAnisotropy, maxAnisotropy);
            Program.GL.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            Program.GL.ActiveTexture(textureSlot);
            Program.GL.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            Program.GL.DeleteTexture(_handle);
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Texture t = (Texture)obj;
                return _handle == t._handle;
            }
        }

        public override int GetHashCode()
        {
            return _handle.GetHashCode();
        }
    }
}