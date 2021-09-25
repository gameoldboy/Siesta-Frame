using Silk.NET.OpenGL;
using System;

namespace TextureTools
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        uint _handle;
        BufferTargetARB _bufferType;

        public unsafe BufferObject(Span<TDataType> data, BufferTargetARB bufferType)
        {
            _bufferType = bufferType;

            _handle = Program.GL.GenBuffer();
            Bind();
            fixed (void* d = &data[0])
            {
                //Console.WriteLine($"sizeof:{sizeof(TDataType)}, length:{data.Length}");
                Program.GL.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        public void Bind()
        {
            Program.GL.BindBuffer(_bufferType, _handle);
        }

        public void Dispose()
        {
            Program.GL.DeleteBuffer(_handle);
        }
    }
}
