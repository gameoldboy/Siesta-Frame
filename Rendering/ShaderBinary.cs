using Silk.NET.OpenGL;
using System;
using System.IO;

namespace SiestaFrame.Rendering
{
    public class ShaderBinary : Shader
    {
        public unsafe ShaderBinary(string path)
        {
            byte[] buffer;
            GLEnum binaryFormat;
            using (var stream = new FileStream(Path.Combine("Shaders", path), FileMode.Open))
            using (var reader = new BinaryReader(stream))
            {
                binaryFormat = (GLEnum)reader.ReadUInt32();
                buffer = reader.ReadBytes((int)(stream.Length - 4));
            }
            //Console.WriteLine($"length:{buffer.Length}, binaryFormat:{binaryFormat}");
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

        public unsafe void Save(string path)
        {
            byte[] buffer = new byte[0x1000000];
            uint length;
            GLEnum binaryFormat;

            fixed (void* b = buffer)
            {
                GraphicsAPI.GL.GetProgramBinary(_handle, 0x1000000, out length, out binaryFormat, b);
            }

            Console.WriteLine($"Save Shader Binary:{path}, length:{length}, binaryFormat:{binaryFormat}");

            using (var stream = new FileStream(Path.Combine("Shaders", path), FileMode.OpenOrCreate))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((uint)binaryFormat);
                writer.Write(buffer, 0, (int)length);
            }
        }
    }
}
