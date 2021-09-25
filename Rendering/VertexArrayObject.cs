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
            _handle = GraphicsAPI.GL.GenVertexArray();
            Bind();
            vbo.Bind();
            ebo.Bind();
        }

        public unsafe void VertexAttributePointer<TVertexBaseType>(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
            where TVertexBaseType : unmanaged
        {
            GraphicsAPI.GL.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexBaseType), (void*)(offSet * sizeof(TVertexBaseType)));
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
