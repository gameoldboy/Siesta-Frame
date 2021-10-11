using Silk.NET.OpenGL;
using System;

namespace TextureTools
{
    public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        uint _handle;

        public VertexArrayObject(BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
        {
            _handle = Program.GL.GenVertexArray();
            Bind();
            vbo.Bind();
            ebo.Bind();
        }

        public unsafe void VertexAttributePointer<TVertexBaseType>(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
            where TVertexBaseType : unmanaged
        {
            Program.GL.EnableVertexAttribArray(index);
            Program.GL.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexBaseType), (void*)(offSet * sizeof(TVertexBaseType)));
        }

        public void Bind()
        {
            Program.GL.BindVertexArray(_handle);
        }

        public void Dispose()
        {
            Program.GL.DeleteVertexArray(_handle);
        }
    }
}
