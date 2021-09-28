using Silk.NET.OpenGL;
using System;

namespace SiestaFrame.Rendering
{
    public class VertexArrayObject : IDisposable
    {
        private uint _handle;

        public VertexArrayObject()
        {
            _handle = GraphicsAPI.GL.GenVertexArray();
        }

        public unsafe void VertexAttributePointer<TVertexType>(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
            where TVertexType : unmanaged
        {
            GraphicsAPI.GL.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexType), (void*)(offSet * sizeof(TVertexType)));
            GraphicsAPI.GL.EnableVertexAttribArray(index);
        }

        public void Bind()
        {
            GraphicsAPI.GL.BindVertexArray(_handle);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteVertexArray(_handle);
        }
    }
}
