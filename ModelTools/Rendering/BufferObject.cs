using Silk.NET.OpenGL;
using System;

namespace ModelTools.Rendering
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        uint _handle;
        BufferTargetARB _bufferType;

        public BufferObject(BufferTargetARB bufferType)
        {
            _bufferType = bufferType;
            _handle = Program.GL.GenBuffer();
        }

        public void Bind()
        {
            Program.GL.BindBuffer(_bufferType, _handle);
        }

        public void BindBufferBase(uint index)
        {
            Program.GL.BindBufferBase(_bufferType, index, _handle);
        }

        public void BindBufferRange(uint index, int offset, uint size)
        {
            Program.GL.BindBufferRange(_bufferType, index, _handle, offset, size);
        }

        public unsafe void BufferData(Span<TDataType> data, BufferUsageARB bufferUsage = BufferUsageARB.StaticDraw)
        {
            fixed (void* d = &data[0])
            {
                Program.GL.BufferData(_bufferType, (nuint)(data.Length * sizeof(TDataType)), d, bufferUsage);
            }
        }

        public unsafe void Dispose()
        {
            Program.GL.DeleteBuffer(_handle);
        }
    }
}
