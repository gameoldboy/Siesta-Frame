using Silk.NET.OpenGL;
using System;

namespace ModelTools.Rendering
{
    public class VertexArrayObject : IDisposable
    {
        private uint _handle;

        public VertexArrayObject()
        {
            _handle = Program.GL.GenVertexArray();
        }

        public unsafe void VertexAttribPointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            Program.GL.EnableVertexAttribArray(index);
            Program.GL.VertexAttribPointer(index, count, type, false, vertexSize, (void*)offSet);
        }

        public unsafe void VertexAttribIPointer(uint index, int count, VertexAttribIType type, uint vertexSize, int offSet)
        {
            Program.GL.EnableVertexAttribArray(index);
            Program.GL.VertexAttribIPointer(index, count, type, vertexSize, (void*)offSet);
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
