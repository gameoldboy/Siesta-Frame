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

        public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            GraphicsAPI.GL.EnableVertexAttribArray(index);
            GraphicsAPI.GL.VertexAttribPointer(index, count, type, false, vertexSize, (void*)offSet);
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
