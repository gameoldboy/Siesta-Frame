using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using System.IO;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Shader : IDisposable
    {
        public static Shader Default { get; }

        static Shader()
        {
            Default = new Shader("vert.glsl", "frag.glsl");
            SceneManager.AddOrUpdateCommonTexture(Default, "vert.glsl", "frag.glsl");
        }

        uint _handle;

        public Shader(string vertPath, string fragPath)
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

        public unsafe Shader(string binPath)
        {
            byte[] buffer;
            GLEnum binaryFormat;
            using (var stream = new FileStream(Path.Combine("Shaders", binPath), FileMode.Open))
            using (var reader = new BinaryReader(stream))
            {
                binaryFormat = (GLEnum)reader.ReadUInt32();
                buffer = reader.ReadBytes((int)(stream.Length - 4));
            }
            System.Diagnostics.Debug.WriteLine($"length:{buffer.Length}, binaryFormat:{binaryFormat}");
            _handle = GraphicsAPI.GL.CreateProgram();
            fixed (void* b = buffer)
            {
                GraphicsAPI.GL.ProgramBinary(_handle, binaryFormat, b, (uint)buffer.Length);
            }
            GraphicsAPI.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {GraphicsAPI.GL.GetProgramInfoLog(_handle)}");
            }
        }

        public void Use()
        {
            GraphicsAPI.GL.UseProgram(_handle);
        }

        public void SetBool(string name, bool value)
        {
            int location = GraphicsAPI.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            GraphicsAPI.GL.Uniform1(location, value ? 1 : 0);
        }

        public int GetUniformLocation(string name)
        {
            int location = GraphicsAPI.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            return location;
        }

        public void SetInt(int location, int value)
        {
            GraphicsAPI.GL.Uniform1(location, value);
        }

        public void SetFloat(int location, float value)
        {
            GraphicsAPI.GL.Uniform1(location, value);
        }

        public void SetVector(int location, float3 value)
        {
            GraphicsAPI.GL.Uniform3(location, value.x, value.y, value.z);
        }

        public void SetVector(int location, float4 value)
        {
            GraphicsAPI.GL.Uniform4(location, value.x, value.y, value.z, value.w);
        }

        public unsafe void SetMatrix(int location, float4x4 value)
        {
            GraphicsAPI.GL.UniformMatrix4(location, 1, false, (float*)&value);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteProgram(_handle);
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

        public unsafe void SaveShaderBinary(string path)
        {
            byte[] buffer = new byte[0x1000000];
            uint length;
            GLEnum binaryFormat;

            fixed (void* b = buffer)
            {
                GraphicsAPI.GL.GetProgramBinary(_handle, 0x1000000, out length, out binaryFormat, b);
            }

            System.Diagnostics.Debug.WriteLine($"length:{length}, binaryFormat:{binaryFormat}");

            using (var stream = new FileStream(Path.Combine("Shaders", path), FileMode.OpenOrCreate))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((uint)binaryFormat);
                writer.Write(buffer, 0, (int)length);
            }
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Shader t = (Shader)obj;
                return _handle == t._handle;
            }
        }

        public override int GetHashCode()
        {
            return _handle.GetHashCode();
        }
    }
}
