using Silk.NET.OpenGL;
using System;
using System.IO;

namespace SiestaFrame.Rendering
{
    public class TextureGOB : Texture
    {
        public TextureGOB(string path)
        {
            string texturePath;
            if (Path.IsPathFullyQualified(path))
            {
                texturePath = path;
            }
            else
            {
                texturePath = Path.Combine("Assets", "Textures", path);
            }
            _handle = GraphicsAPI.GL.GenTexture();
            Bind();
            using (var fileStream = new FileStream(texturePath, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
            {
                if (reader.ReadInt32() != 0x54424F47)
                {
                    throw new Exception("GOB Texture format magic number incorrect");
                }
                var internalFormat = (InternalFormat)reader.ReadInt32();
                switch (internalFormat)
                {
                    case InternalFormat.CompressedRgbaBptcUnorm:
                        internalFormat = InternalFormat.CompressedSrgbAlphaBptcUnorm;
                        break;
                    case InternalFormat.CompressedRgbaS3TCDxt5Ext:
                        internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext;
                        break;
                    case InternalFormat.CompressedRgbaS3TCDxt3Ext:
                        internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext;
                        break;
                    case InternalFormat.CompressedRgbS3TCDxt1Ext:
                        internalFormat = InternalFormat.CompressedSrgbS3TCDxt1Ext;
                        break;
                }
                //Console.WriteLine($"InternalFormat:{internalFormat}");
                var maxLevel = reader.ReadInt32();
                for (int i = 0; i < maxLevel; i++)
                {
                    var width = (uint)reader.ReadInt32();
                    var height = (uint)reader.ReadInt32();
                    var size = (uint)reader.ReadInt32();
                    var data = reader.ReadBytes((int)size);
                    //Console.WriteLine($"level:{i}, width:{width}, height:{height}, size:{size}");
                    Load(data, i, internalFormat, width, height, size);
                }
            }
            Setup();
        }

        unsafe void Load(Span<byte> data, int level, InternalFormat internalFormat, uint width, uint height, uint size)
        {
            fixed (void* d = &data[0])
            {
                GraphicsAPI.GL.CompressedTexImage2D(TextureTarget.Texture2D, level, internalFormat, width, height, 0, size, d);
            }
        }

        void Setup()
        {
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMaxAnisotropy, maxAnisotropy);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
