using Silk.NET.OpenGL;
using System;
using System.IO;

namespace ModelTools.Rendering
{
    public class ShaderSource : Shader
    {
        public ShaderSource(string vertPath, string fragPath)
        {
            uint vert = LoadShader(ShaderType.VertexShader, vertPath);
            uint frag = LoadShader(ShaderType.FragmentShader, fragPath);
            _handle = Program.GL.CreateProgram();
            Program.GL.AttachShader(_handle, vert);
            Program.GL.AttachShader(_handle, frag);
            Program.GL.LinkProgram(_handle);
            Program.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {Program.GL.GetProgramInfoLog(_handle)}");
            }
            Program.GL.DetachShader(_handle, vert);
            Program.GL.DetachShader(_handle, frag);
            Program.GL.DeleteShader(vert);
            Program.GL.DeleteShader(frag);
        }

        uint LoadShader(ShaderType type, string src)
        {
            uint handle = Program.GL.CreateShader(type);
            Program.GL.ShaderSource(handle, src);
            Program.GL.CompileShader(handle);
            string infoLog = Program.GL.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }
    }
}
