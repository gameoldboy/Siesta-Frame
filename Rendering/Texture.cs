using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Runtime.InteropServices;

namespace SiestaFrame.Rendering
{
    class Texture : IDisposable
    {
        private uint _handle;

        public unsafe Texture(string path)
        {
            Image<Rgba32> img = (Image<Rgba32>)Image.Load(path);
            img.Mutate(x => x.Flip(FlipMode.Vertical));

            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
            {
                Load(data, (uint)img.Width, (uint)img.Height);
            }

            img.Dispose();
        }

        public unsafe Texture(byte[] buffer)
        {
            Image<Rgba32> img = (Image<Rgba32>)Image.Load(buffer);
            img.Mutate(x => x.Flip(FlipMode.Vertical));

            fixed (void* data = &MemoryMarshal.GetReference(img.GetPixelRowSpan(0)))
            {
                Load(data, (uint)img.Width, (uint)img.Height);
            }

            img.Dispose();
        }


        public unsafe Texture(Span<byte> data, uint width, uint height)
        {
            fixed (void* d = &data[0])
            {
                Load(d, width, height);
            }
        }

        private unsafe void Load(void* data, uint width, uint height)
        {
            _handle = Graphics.GL.GenTexture();
            Bind();

            Graphics.GL.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            Graphics.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            Graphics.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            Graphics.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            Graphics.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            Graphics.GL.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            Graphics.GL.ActiveTexture(textureSlot);
            Graphics.GL.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            Graphics.GL.DeleteTexture(_handle);
        }
    }
}
