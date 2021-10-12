using Silk.NET.OpenGL;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Mathematics;

namespace TextureTools
{
    public class Shader : IDisposable
    {
        public static Shader Default { get; }

        static Shader()
        {
            var assembly = Assembly.GetExecutingAssembly();

            string vert, frag;
            using (Stream stream = assembly.GetManifestResourceStream("TextureTools.Shaders.vert.glsl"))
            using (StreamReader reader = new StreamReader(stream))
            {
                vert = reader.ReadToEnd();
            }
            using (Stream stream = assembly.GetManifestResourceStream("TextureTools.Shaders.frag.glsl"))
            using (StreamReader reader = new StreamReader(stream))
            {
                frag = reader.ReadToEnd();
            }
            Default = new Shader(vert, frag);
        }

        uint _handle;

        public Shader(string vertStr, string fragStr)
        {
            uint vert = LoadShader(ShaderType.VertexShader, vertStr);
            uint frag = LoadShader(ShaderType.FragmentShader, fragStr);
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
            Console.WriteLine($"length:{buffer.Length}, binaryFormat:{binaryFormat}");
            _handle = Program.GL.CreateProgram();
            fixed (void* b = buffer)
            {
                Program.GL.ProgramBinary(_handle, binaryFormat, b, (uint)buffer.Length);
            }
            Program.GL.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {Program.GL.GetProgramInfoLog(_handle)}");
            }
        }

        public void Use()
        {
            Program.GL.UseProgram(_handle);
        }

        public void SetBool(string name, bool value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.Uniform1(location, value ? 1 : 0);
        }

        public void SetInt(string name, int value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.Uniform1(location, value);
        }

        public void SetVector(string name, float2 value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.Uniform2(location, value.x, value.y);
        }

        public void SetVector(string name, float3 value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.Uniform3(location, value.x, value.y, value.z);
        }

        public void SetVector(string name, float4 value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.Uniform4(location, value.x, value.y, value.z, value.w);
        }

        public unsafe void SetMatrix(string name, float4x4 value)
        {
            int location = Program.GL.GetUniformLocation(_handle, name);
            //if (location == -1)
            //{
            //    throw new Exception($"{name} uniform not found on shader.");
            //}
            Program.GL.UniformMatrix4(location, 1, false, (float*)&value);
        }

        public void Dispose()
        {
            Program.GL.DeleteProgram(_handle);
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

        public unsafe void SaveShaderBinary(string path)
        {
            byte[] buffer = new byte[0x1000000];
            uint length;
            GLEnum binaryFormat;

            fixed (void* b = buffer)
            {
                Program.GL.GetProgramBinary(_handle, 0x1000000, out length, out binaryFormat, b);
            }

            Console.WriteLine($"length:{length}, binaryFormat:{binaryFormat}");

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
