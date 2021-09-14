using Silk.NET.OpenGL;
using System;

namespace SiestaFrame.Rendering
{
    public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        private uint _handle;

        public VertexArrayObject(BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
        {
            _handle = Graphics.GL.GenVertexArray();
            Bind();
            vbo.Bind();
            ebo.Bind();
        }

        public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            Graphics.GL.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexType), (void*)(offSet * sizeof(TVertexType)));
            Graphics.GL.EnableVertexAttribArray(index);
        }

        public void Bind()
        {
            Graphics.GL.BindVertexArray(_handle);
        }

        public void Dispose()
        {
            Graphics.GL.DeleteVertexArray(_handle);
        }
    }
}
