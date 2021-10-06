using SiestaFrame.SceneManagement;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class Shader : IDisposable
    {
        public static Shader Default { get; }

        static Shader()
        {
            Default = SceneManager.AddCommonShader("DefaultVert.glsl", "DefaultFrag.glsl");
        }

        protected uint _handle;

        public void Use()
        {
            GraphicsAPI.GL.UseProgram(_handle);
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

        public void SetBool(int location, bool value)
        {
            GraphicsAPI.GL.Uniform1(location, value ? 1 : 0);
        }

        public void SetInt(int location, int value)
        {
            GraphicsAPI.GL.Uniform1(location, value);
        }

        public void SetFloat(int location, float value)
        {
            GraphicsAPI.GL.Uniform1(location, value);
        }

        public void SetVector(int location, float2 value)
        {
            GraphicsAPI.GL.Uniform2(location, value.x, value.y);
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
