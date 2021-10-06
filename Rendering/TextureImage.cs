using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace SiestaFrame.Rendering
{
    public class TextureImage : Texture
    {
        public TextureImage(string path)
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
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();
            using (var img = (Image<Rgba32>)Image.Load(texturePath))
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));

                Load(img.GetPixelRowSpan(0), (uint)img.Width, (uint)img.Height);
            }
            Setup();
        }

        public TextureImage(byte[] buffer)
        {
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();
            using (var img = (Image<Rgba32>)Image.Load(buffer))
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));

                Load(img.GetPixelRowSpan(0), (uint)img.Width, (uint)img.Height);
            }
            Setup();
        }

        public TextureImage(Image<Rgba32> img)
        {
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();
            Load(img.GetPixelRowSpan(0), (uint)img.Width, (uint)img.Height);
            Setup();
        }

        public TextureImage(Span<byte> data, uint width, uint height)
        {
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();
            Load(data, width, height);
            Setup();
        }

        unsafe void Load(Span<byte> data, uint width, uint height)
        {
            fixed (void* d = &data[0])
            {
                GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.SrgbAlpha, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            }
        }

        unsafe void Load(Span<Rgba32> data, uint width, uint height)
        {
            fixed (void* d = &data[0])
            {
                GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.SrgbAlpha, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
            }
        }

        void Setup()
        {
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxAnisotropy, maxAnisotropy);
            GraphicsAPI.GL.GenerateMipmap(TextureTarget.Texture2D);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
