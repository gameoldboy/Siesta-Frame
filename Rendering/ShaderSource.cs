using Silk.NET.OpenGL;
using System;
using System.IO;

namespace SiestaFrame.Rendering
{
    public class ShaderSource : Shader
    {
        public ShaderSource(string vertPath, string fragPath)
        {
            uint vert = LoadShader(ShaderType.VertexShader, vertPath);
            uint frag = LoadShader(ShaderType.FragmentShader, fragPath);
            _handle = GraphicsAPI.GL.CreateProgram();
            GraphicsAPI.GL.AttachShader(_handle, vert);
            GraphicsAPI.GL.AttachShader(_handle, frag);
            GraphicsAPI.GL.LinkProgram(_handle);
            GraphicsAPI.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {GraphicsAPI.GL.GetProgramInfoLog(_handle)}");
            }
            GraphicsAPI.GL.DetachShader(_handle, vert);
            GraphicsAPI.GL.DetachShader(_handle, frag);
            GraphicsAPI.GL.DeleteShader(vert);
            GraphicsAPI.GL.DeleteShader(frag);
        }

        uint LoadShader(ShaderType type, string path)
        {
            string src = File.ReadAllText(Path.Combine("Shaders", path));
            uint handle = GraphicsAPI.GL.CreateShader(type);
            GraphicsAPI.GL.ShaderSource(handle, src);
            GraphicsAPI.GL.CompileShader(handle);
            string infoLog = GraphicsAPI.GL.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }
    }
}
