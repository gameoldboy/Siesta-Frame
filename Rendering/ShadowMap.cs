﻿using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class ShadowMap : IDisposable
    {
        uint frameBuffer;
        uint shadowMap;

        uint Width;
        uint Height;

        Shader shader;

        int matrixModelLocation { get; }
        int matrixViewLocation { get; }
        int matrixProjectionLocation { get; }
        int baseMapLocation { get; }
        int tilingOffsetLocation { get; }
        int alphaTest { get; }

        unsafe public ShadowMap(int width, int height)
        {
            Width = (uint)width;
            Height = (uint)height;
            frameBuffer = GraphicsAPI.GL.GenFramebuffer();
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            shadowMap = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, shadowMap);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, Width, Height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)GLEnum.Lequal);
            GraphicsAPI.GL.DrawBuffer(GLEnum.None);
            GraphicsAPI.GL.ReadBuffer(GLEnum.None);
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMap, 0);
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);

            shader = Shader.ShadowMap;
            matrixModelLocation = shader.GetUniformLocation("MatrixModel");
            matrixViewLocation = shader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = shader.GetUniformLocation("MatrixProjection");
            baseMapLocation = shader.GetUniformLocation("_BaseMap");
            tilingOffsetLocation = shader.GetUniformLocation("_TilingOffset");
            alphaTest = shader.GetUniformLocation("_AlphaTest");
            GraphicsAPI.GL.UseProgram(0);
        }

        public void BindShadowMap(TextureUnit unit = TextureUnit.Texture0)
        {
            GraphicsAPI.GL.ActiveTexture(unit);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, shadowMap);
        }

        public unsafe void RenderShadowMap(Scene scene)
        {
            if (scene.MainLight.Type == Light.LightType.Directional)
            {
                scene.MainLight.ShadowFitToCamera(scene.MainCamera);
            }
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, Width, Height);
            for (int i = 0; i < scene.Entites.Count; i++)
            {
                var entity = scene.Entites[i];
                for (int j = 0; j < scene.Entites[i].Meshes.Length; j++)
                {
                    var mesh = entity.Meshes[j];
                    var material = entity.Materials[j % entity.Materials.Length];

                    mesh.VAO.Bind();
                    shader.Use();

                    shader.SetMatrix(matrixModelLocation, entity.Transform.ModelMatrix);
                    shader.SetMatrix(matrixViewLocation, scene.MainLight.ViewMatrix);
                    shader.SetMatrix(matrixProjectionLocation, scene.MainLight.ProjectionMatrix);
                    material.BaseMap.Bind(TextureUnit.Texture0);
                    shader.SetInt(baseMapLocation, 0);
                    shader.SetVector(tilingOffsetLocation, material.TilingOffset);
                    shader.SetBool(alphaTest, material.Mode != Material.BlendMode.None ? true : false);
                    if (math.sign(entity.Transform.Scale.x) * math.sign(entity.Transform.Scale.y) * math.sign(entity.Transform.Scale.z) < 0)
                    {
                        GraphicsAPI.GL.FrontFace(FrontFaceDirection.CW);
                    }
                    else
                    {
                        GraphicsAPI.GL.FrontFace(FrontFaceDirection.Ccw);
                    }
                    GraphicsAPI.GL.DrawElements(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
                    GraphicsAPI.GL.BindVertexArray(0);
                    GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                    GraphicsAPI.GL.UseProgram(0);
                }
            }
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteFramebuffer(frameBuffer);
            GraphicsAPI.GL.DeleteTexture(shadowMap);
        }
    }
}
