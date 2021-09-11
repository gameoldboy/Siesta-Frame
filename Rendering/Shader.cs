using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Numerics;

namespace SiestaFrame.Rendering
{
    public class Shader : IDisposable
    {
        uint _handle;

        public Shader(string vertPath, string fragPath)
        {
            uint vert = LoadShader(ShaderType.VertexShader, vertPath);
            uint frag = LoadShader(ShaderType.FragmentShader, fragPath);
            _handle = Graphics.GL.CreateProgram();
            Graphics.GL.AttachShader(_handle, vert);
            Graphics.GL.AttachShader(_handle, frag);
            Graphics.GL.LinkProgram(_handle);
            Graphics.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {Graphics.GL.GetProgramInfoLog(_handle)}");
            }
            Graphics.GL.DetachShader(_handle, vert);
            Graphics.GL.DetachShader(_handle, frag);
            Graphics.GL.DeleteShader(vert);
            Graphics.GL.DeleteShader(frag);
        }

        public void Use()
        {
            Graphics.GL.UseProgram(_handle);
        }

        public void SetBool(string name, bool value)
        {
            int location = Graphics.GL.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            Graphics.GL.Uniform1(location, value ? 1 : 0);
        }

        public void SetInt(string name, int value)
        {
            int location = Graphics.GL.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            Graphics.GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            int location = Graphics.GL.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            Graphics.GL.Uniform1(location, value);
        }

        public unsafe void SetMatrix(string name, Matrix4x4 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = Graphics.GL.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            Graphics.GL.UniformMatrix4(location, 1, false, (float*)&value);
        }

        public void Dispose()
        {
            Graphics.GL.DeleteProgram(_handle);
        }

        uint LoadShader(ShaderType type, string name)
        {
            string src = File.ReadAllText(Path.Combine("Shaders", name));
            uint handle = Graphics.GL.CreateShader(type);
            Graphics.GL.ShaderSource(handle, src);
            Graphics.GL.CompileShader(handle);
            string infoLog = Graphics.GL.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }
    }
}
