using Silk.NET.OpenGL;
using System;

namespace SiestaFrame.Rendering
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        uint _handle;
        BufferTargetARB _bufferType;

        public unsafe BufferObject(Span<TDataType> data, BufferTargetARB bufferType)
        {
            _bufferType = bufferType;

            _handle = Graphics.GL.GenBuffer();
            Bind();
            fixed (void* d = data)
            {
                Graphics.GL.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        public void Bind()
        {
            Graphics.GL.BindBuffer(_bufferType, _handle);
        }

        public void Dispose()
        {
            Graphics.GL.DeleteBuffer(_handle);
        }
    }
}
