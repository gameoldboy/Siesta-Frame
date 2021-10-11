using Silk.NET.OpenGL;
using System;

namespace SiestaFrame.Rendering
{
    public class GBuffer : IDisposable
    {
        public uint NormalTexture { get; private set; }
        public uint MotionVectors { get; private set; }

        public GBuffer()
        {
            Alloc();
        }

        public unsafe void Alloc()
        {
            if (NormalTexture > 0)
            {
                GraphicsAPI.GL.DeleteTexture(NormalTexture);
            }
            if (MotionVectors > 0)
            {
                GraphicsAPI.GL.DeleteTexture(MotionVectors);
            }
            NormalTexture = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, NormalTexture);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f, App.Instance.MainWindow.Width, App.Instance.MainWindow.Height, 0, PixelFormat.Rgb, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            MotionVectors = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, MotionVectors);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.RG16f, App.Instance.MainWindow.Width, App.Instance.MainWindow.Height, 0, PixelFormat.RG, GLEnum.HalfFloat, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteTexture(NormalTexture);
            GraphicsAPI.GL.DeleteTexture(MotionVectors);
        }
    }
}
