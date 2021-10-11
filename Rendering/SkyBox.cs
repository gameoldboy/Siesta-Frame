using SiestaFrame.Object;
using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class SkyBox : IDisposable
    {
        uint skyboxMap;
        Texture skyboxMapHDR;

        bool isCubeMap;

        Shader shaderCubeMap;
        Shader shaderHDR;

        int skyboxMapLocation;
        int skyboxColorLocation;
        int modelMatrixLocation;
        int viewMatrixLocation;
        int projectionMatrixLocation;
        int prevModelMatrixLocation;
        int prevViewMatrixLocation;
        int prevProjectionMatrixLocation;

        Entity skybox;

        public struct SkyBoxFaces
        {
            public string right;
            public string left;
            public string top;
            public string bottom;
            public string front;
            public string back;
        }

        public SkyBox()
        {
            skybox = Utilities.LoadModel("skybox.obj");

            shaderCubeMap = SceneManager.AddCommonShader("SkyBoxVert.glsl", "SkyBoxFrag.glsl");
            shaderHDR = SceneManager.AddCommonShader("SkyBoxVert.glsl", "SkyBoxHDRFrag.glsl");
        }

        public unsafe void Load(SkyBoxFaces faces)
        {
            if (skyboxMap > 0)
            {
                GraphicsAPI.GL.DeleteTexture(skyboxMap);
            }
            skyboxMapHDR?.Dispose();

            skyboxMap = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.TextureCubeMap, skyboxMap);

            using (var img = (Image<Rgba32>)Image.Load(Path.Combine("Assets", "Textures", "SkyBox", faces.right)))
            {
                fixed (void* data = &img.GetPixelRowSpan(0)[0])
                {
                    GraphicsAPI.GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX, 0, InternalFormat.SrgbAlpha, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }
            using (var img = (Image<Rgba32>)Image.Load(Path.Combine("Assets", "Textures", "SkyBox", faces.left)))
            {
                fixed (void* data = &img.GetPixelRowSpan(0)[0])
                {
                    GraphicsAPI.GL.TexImage2D(TextureTarget.TextureCubeMapNegativeX, 0, InternalFormat.SrgbAlpha, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }
            using (var img = (Image<Rgba32>)Image.Load(Path.Combine("Assets", "Textures", "SkyBox", faces.top)))
            {
                fixed (void* data = &img.GetPixelRowSpan(0)[0])
                {
                    GraphicsAPI.GL.TexImage2D(TextureTarget.TextureCubeMapPositiveY, 0, InternalFormat.SrgbAlpha, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }
            using (var img = (Image<Rgba32>)Image.Load(Path.Combine("Assets", "Textures", "SkyBox", faces.bottom)))
            {
                fixed (void* data = &img.GetPixelRowSpan(0)[0])
                {
                    GraphicsAPI.GL.TexImage2D(TextureTarget.TextureCubeMapNegativeY, 0, InternalFormat.SrgbAlpha, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }
            using (var img = (Image<Rgba32>)Image.Load(Path.Combine("Assets", "Textures", "SkyBox", faces.front)))
            {
                fixed (void* data = &img.GetPixelRowSpan(0)[0])
                {
                    GraphicsAPI.GL.TexImage2D(TextureTarget.TextureCubeMapPositiveZ, 0, InternalFormat.SrgbAlpha, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }
            using (var img = (Image<Rgba32>)Image.Load(Path.Combine("Assets", "Textures", "SkyBox", faces.back)))
            {
                fixed (void* data = &img.GetPixelRowSpan(0)[0])
                {
                    GraphicsAPI.GL.TexImage2D(TextureTarget.TextureCubeMapNegativeZ, 0, InternalFormat.SrgbAlpha, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }

            GraphicsAPI.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);

            GraphicsAPI.GL.BindTexture(TextureTarget.TextureCubeMap, 0);

            skyboxMapLocation = shaderCubeMap.GetUniformLocation("_BaseMap");
            skyboxColorLocation = shaderCubeMap.GetUniformLocation("_SkyboxColor");
            modelMatrixLocation = shaderHDR.GetUniformLocation("MatrixModel");
            viewMatrixLocation = shaderCubeMap.GetUniformLocation("MatrixView");
            projectionMatrixLocation = shaderCubeMap.GetUniformLocation("MatrixProjection");
            prevModelMatrixLocation = shaderCubeMap.GetUniformLocation("PrevMatrixModel");
            prevViewMatrixLocation = shaderCubeMap.GetUniformLocation("PrevMatrixView");
            prevProjectionMatrixLocation = shaderCubeMap.GetUniformLocation("PrevMatrixProjection");

            isCubeMap = true;
        }

        public unsafe void Load(string path)
        {
            if (skyboxMap > 0)
            {
                GraphicsAPI.GL.DeleteTexture(skyboxMap);
            }
            skyboxMapHDR?.Dispose();

            skyboxMapHDR = new TextureGOB(Path.Combine("SkyBox", path));
            skyboxMapHDR.Bind();

            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);

            skyboxMapLocation = shaderHDR.GetUniformLocation("_BaseMap");
            skyboxColorLocation = shaderHDR.GetUniformLocation("_SkyboxColor");
            modelMatrixLocation = shaderHDR.GetUniformLocation("MatrixModel");
            viewMatrixLocation = shaderHDR.GetUniformLocation("MatrixView");
            projectionMatrixLocation = shaderHDR.GetUniformLocation("MatrixProjection");
            prevModelMatrixLocation = shaderCubeMap.GetUniformLocation("PrevMatrixModel");
            prevViewMatrixLocation = shaderHDR.GetUniformLocation("PrevMatrixView");
            prevProjectionMatrixLocation = shaderHDR.GetUniformLocation("PrevMatrixProjection");

            isCubeMap = false;
        }

        public unsafe void Draw(Scene.RenderingData renderingData, float3 color, float angle)
        {
            var mesh = skybox.Meshes[0];
            mesh.VAO.Bind();
            var shader = isCubeMap ? shaderCubeMap : shaderHDR;
            shader.Use();
            if (isCubeMap)
            {
                GraphicsAPI.GL.ActiveTexture(TextureUnit.Texture0);
                GraphicsAPI.GL.BindTexture(TextureTarget.TextureCubeMap, skyboxMap);
            }
            else
            {
                skyboxMapHDR.Bind(TextureUnit.Texture0);
            }
            shader.SetInt(skyboxMapLocation, 0);
            shader.SetVector(skyboxColorLocation, color);
            shader.SetMatrix(modelMatrixLocation, float4x4.RotateY(angle * MathHelper.Deg2Rad));
            shader.SetMatrix(viewMatrixLocation, MathHelper.RemoveTranslation(renderingData.viewMatrix));
            shader.SetMatrix(projectionMatrixLocation, renderingData.projectionMatrix);
            shader.SetMatrix(prevModelMatrixLocation, float4x4.RotateY(angle * MathHelper.Deg2Rad));
            shader.SetMatrix(prevViewMatrixLocation, MathHelper.RemoveTranslation(renderingData.prevViewMatrix));
            shader.SetMatrix(prevProjectionMatrixLocation, renderingData.prevProjectionMatrix);
            GraphicsAPI.GL.DepthMask(false);
            GraphicsAPI.GL.DepthFunc(DepthFunction.Lequal);
            GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
            GraphicsAPI.GL.DepthFunc(DepthFunction.Less);
            GraphicsAPI.GL.DepthMask(true);
            GraphicsAPI.GL.BindVertexArray(0);
            if (isCubeMap)
            {
                GraphicsAPI.GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            }
            else
            {
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
            }
            GraphicsAPI.GL.UseProgram(0);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteTexture(skyboxMap);
            skyboxMapHDR.Dispose();
            skybox.Dispose();
            shaderCubeMap.Dispose();
            shaderHDR.Dispose();
        }
    }
}
