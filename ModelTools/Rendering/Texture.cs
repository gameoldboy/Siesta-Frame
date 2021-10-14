using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace ModelTools.Rendering
{
    public class Texture : IDisposable
    {
        public static Texture White { get; }
        public static Texture Black { get; }
        public static Texture Normal { get; }
        public static Texture Gray { get; }
        public static Texture Red { get; }

        protected const int maxAnisotropy = 16;

        static Texture()
        {
            using (Image<Rgba32> img = new Image<Rgba32>(4, 4))
            {
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        img[i, j] = new Rgba32(1f, 1f, 1f, 1f);
                    }
                }
                White = new TextureImage(img);
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        img[i, j] = new Rgba32(0f, 0f, 0f, 0f);
                    }
                }
                Black = new TextureImage(img);
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        img[i, j] = new Rgba32(0.735357f, 0.735357f, 1f, 1f);
                    }
                }
                Normal = new TextureImage(img);
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        img[i, j] = new Rgba32(0.735357f, 0.735357f, 0.735357f, 1f);
                    }
                }
                Gray = new TextureImage(img);
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        img[i, j] = new Rgba32(1f, 0f, 0f, 1f);
                    }
                }
                Red = new TextureImage(img);
            }
        }

        protected uint _handle;

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
