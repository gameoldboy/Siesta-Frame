using Silk.NET.OpenGL;
using System;

namespace SiestaFrame.Rendering
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        uint _handle;
        BufferTargetARB _bufferType;

        public BufferObject(BufferTargetARB bufferType)
        {
            _bufferType = bufferType;
            _handle = GraphicsAPI.GL.GenBuffer();
        }

        public void Bind()
        {
            GraphicsAPI.GL.BindBuffer(_bufferType, _handle);
        }

        public unsafe void BufferData(Span<TDataType> data)
        {
            fixed (void* d = &data[0])
            {
                //Console.WriteLine($"sizeof:{sizeof(TDataType)}, length:{data.Length}");
                GraphicsAPI.GL.BufferData(_bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteBuffer(_handle);
        }
    }
}
