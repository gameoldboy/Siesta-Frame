using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SiestaFrame.Rendering
{
    public class Texture : IDisposable
    {
        public static Texture White { get; }
        public static Texture Black { get; }
        public static Texture Normal { get; }
        public static Texture Gray { get; }
        public static Texture Red { get; }

        const int maxAnisotropy = 16;

        static Texture()
        {
            Image<Rgba32> img = new Image<Rgba32>(4, 4);
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    img[i, j] = new Rgba32(1f, 1f, 1f, 1f);
                }
            }
            White = new Texture(img);
            SceneManager.AddOrUpdateCommonTexture(White, "White");
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    img[i, j] = new Rgba32(0f, 0f, 0f, 0f);
                }
            }
            Black = new Texture(img);
            SceneManager.AddOrUpdateCommonTexture(Black, "Black");
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    img[i, j] = new Rgba32(0.735357f, 0.735357f, 1f, 1f);
                }
            }
            Normal = new Texture(img);
            SceneManager.AddOrUpdateCommonTexture(Normal, "Normal");
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    img[i, j] = new Rgba32(0.735357f, 0.735357f, 0.735357f, 1f);
                }
            }
            Gray = new Texture(img);
            SceneManager.AddOrUpdateCommonTexture(Gray, "Gray");
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    img[i, j] = new Rgba32(1f, 0f, 0f, 1f);
                }
            }
            Red = new Texture(img);
            SceneManager.AddOrUpdateCommonTexture(Red, "Red");
            img.Dispose();
        }

        uint _handle;

        public unsafe Texture(string path)
        {
            string texturePath;
            if (Path.IsPathFullyQualified(path))
            {
                texturePath = path;
            }
            else
            {
                texturePath = Path.Combine("Assets", "Textures", path);
            }
            var ext = Path.GetExtension(texturePath).ToLower();
            switch (ext)
            {
                case ".gobt":
                    using (var fileStream = new FileStream(texturePath, FileMode.Open))
                    {
                        Load(fileStream);
                    }
                    break;
                default:
                    var img = (Image<Rgba32>)Image.Load(texturePath);
                    img.Mutate(x => x.Flip(FlipMode.Vertical));

                    fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
                    {
                        Load(data, (uint)img.Width, (uint)img.Height);
                    }
                    img.Dispose();
                    break;
            }
        }

        public unsafe Texture(byte[] buffer)
        {
            var img = (Image<Rgba32>)Image.Load(buffer);
            img.Mutate(x => x.Flip(FlipMode.Vertical));

            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
            {
                Load(data, (uint)img.Width, (uint)img.Height);
            }
            img.Dispose();
        }

        public unsafe Texture(Image<Rgba32> img)
        {
            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
            {
                Load(data, (uint)img.Width, (uint)img.Height);
            }
        }

        public unsafe Texture(Span<byte> data, uint width, uint height)
        {
            fixed (void* d = &data[0])
            {
                Load(d, width, height);
            }
        }

        unsafe void Load(void* data, uint width, uint height)
        {
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();

            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.SrgbAlpha, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxAnisotropy, maxAnisotropy);
            GraphicsAPI.GL.GenerateMipmap(TextureTarget.Texture2D);
        }

        unsafe void Load(FileStream fileStream)
        {
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();

            using (var reader = new BinaryReader(fileStream))
            {
                if (reader.ReadInt32() != 0x54424F47)
                {
                    throw new Exception("GOB Texture format magic number incorrect");
                }
                var internalFormat = (InternalFormat)reader.ReadInt32();
                switch (internalFormat)
                {
                    case InternalFormat.CompressedRgbaBptcUnorm:
                        internalFormat = InternalFormat.CompressedSrgbAlphaBptcUnorm;
                        break;
                    case InternalFormat.CompressedRgbaS3TCDxt5Ext:
                        internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext;
                        break;
                    case InternalFormat.CompressedRgbaS3TCDxt3Ext:
                        internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext;
                        break;
                    case InternalFormat.CompressedRgbS3TCDxt1Ext:
                        internalFormat = InternalFormat.CompressedSrgbS3TCDxt1Ext;
                        break;
                }
                //Console.WriteLine($"InternalFormat:{internalFormat}");
                var maxLevel = reader.ReadInt32();
                for (int i = 0; i < maxLevel; i++)
                {
                    var width = (uint)reader.ReadInt32();
                    var height = (uint)reader.ReadInt32();
                    var size = (uint)reader.ReadInt32();
                    var data = reader.ReadBytes((int)size);
                    //Console.WriteLine($"level:{i}, width:{width}, height:{height}, size:{size}");
                    fixed (void* d = &data[0])
                    {
                        GraphicsAPI.GL.CompressedTexImage2D(TextureTarget.Texture2D, i, internalFormat, width, height, 0, size, d);
                    }
                }
            }

            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxAnisotropy, maxAnisotropy);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            GraphicsAPI.GL.ActiveTexture(textureSlot);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteTexture(_handle);
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
