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
        Shader instancedShader;

        int matrixModelLocation;
        int matrixViewLocation;
        int matrixProjectionLocation;
        int baseColorLocation;
        int baseMapLocation;
        int tilingOffsetLocation;
        int alphaTestLocation;
        int screenSizeLocation;
        int matrixViewInstancedLocation;
        int matrixProjectionInstancedLocation;
        int baseMapInstancedLocation;
        int alphaTestInstancedLocation;
        int screenSizeInstancedLocation;

        public ShadowMap(uint width, uint height)
        {
            Alloc(width, height);

            shader = SceneManager.AddCommonShader("ShadowMapVert.glsl", "ShadowMapFrag.glsl");
            instancedShader = SceneManager.AddCommonShader("ShadowMapInstancedVert.glsl", "ShadowMapInstancedFrag.glsl");

            matrixModelLocation = shader.GetUniformLocation("MatrixModel");
            matrixViewLocation = shader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = shader.GetUniformLocation("MatrixProjection");
            baseColorLocation = shader.GetUniformLocation("_BaseColor");
            baseMapLocation = shader.GetUniformLocation("_BaseMap");
            tilingOffsetLocation = shader.GetUniformLocation("_TilingOffset");
            alphaTestLocation = shader.GetUniformLocation("_AlphaTest");
            screenSizeLocation = shader.GetUniformLocation("_ScreenSize");
            matrixViewInstancedLocation = instancedShader.GetUniformLocation("MatrixView");
            matrixProjectionInstancedLocation = instancedShader.GetUniformLocation("MatrixProjection");
            baseMapInstancedLocation = instancedShader.GetUniformLocation("_BaseMap");
            alphaTestInstancedLocation = instancedShader.GetUniformLocation("_AlphaTest");
            screenSizeInstancedLocation = instancedShader.GetUniformLocation("_ScreenSize");
        }

        public unsafe void Alloc(uint width, uint height)
        {
            if (frameBuffer > 0)
            {
                GraphicsAPI.GL.DeleteFramebuffer(frameBuffer);
            }
            if (shadowMap > 0)
            {
                GraphicsAPI.GL.DeleteTexture(shadowMap);
            }
            Width = width;
            Height = height;
            frameBuffer = GraphicsAPI.GL.GenFramebuffer();
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            shadowMap = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, shadowMap);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent32f, Width, Height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 0f, 0f, 0f, 1f });
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)GLEnum.Lequal);
            GraphicsAPI.GL.DrawBuffer(DrawBufferMode.None);
            GraphicsAPI.GL.ReadBuffer(ReadBufferMode.None);
            GraphicsAPI.GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMap, 0);
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
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
            else
            {
                throw new NotImplementedException("TO DO: Non-Directional Light");
            }
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            GraphicsAPI.GL.Clear(ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, Width, Height);

            for (int i = 0; i < scene.Entites.Count; i++)
            {
                var entity = scene.Entites[i];

                if (entity.DrawType == Mesh.DrawType.Direct)
                {
                    for (int j = 0; j < entity.Meshes.Length; j++)
                    {
                        var mesh = entity.Meshes[j];
                        var material = entity.Materials[j % entity.Materials.Length];

                        mesh.VAO.Bind();
                        shader.Use();
                        shader.SetMatrix(matrixModelLocation, entity.Transform.ModelMatrix);
                        shader.SetMatrix(matrixViewLocation, scene.MainLight.ViewMatrix);
                        shader.SetMatrix(matrixProjectionLocation, scene.MainLight.ProjectionMatrix);
                        shader.SetVector(baseColorLocation, material.BaseColor);
                        material.BaseMap.Bind(TextureUnit.Texture0);
                        shader.SetInt(baseMapLocation, 0);
                        shader.SetVector(tilingOffsetLocation, material.TilingOffset);
                        shader.SetBool(alphaTestLocation, material.Mode != Material.BlendMode.None ? true : false);
                        shader.SetVector(screenSizeLocation, new float2(Width, Height));
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
                        GraphicsAPI.GL.FrontFace(FrontFaceDirection.Ccw);
                    }
                }
            }

            for (int i = 0; i < scene.InstancedList.Count; i++)
            {
                var entities = scene.InstancedList[i];
                var mesh = scene.InstancedDictionaryIndex[i];
                var material = entities[0].Materials[0];

                mesh.VAO.Bind();
                instancedShader.Use();
                instancedShader.SetMatrix(matrixViewInstancedLocation, scene.MainLight.ViewMatrix);
                instancedShader.SetMatrix(matrixProjectionInstancedLocation, scene.MainLight.ProjectionMatrix);
                material.BaseMap.Bind(TextureUnit.Texture0);
                instancedShader.SetInt(baseMapInstancedLocation, 0);
                instancedShader.SetBool(alphaTestInstancedLocation, material.Mode != Material.BlendMode.None ? true : false);
                instancedShader.SetVector(screenSizeInstancedLocation, new float2(Width, Height));
                GraphicsAPI.GL.DrawElementsInstanced(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null, (uint)entities.Count);
                GraphicsAPI.GL.BindVertexArray(0);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }

            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteFramebuffer(frameBuffer);
            GraphicsAPI.GL.DeleteTexture(shadowMap);
            shader.Dispose();
            instancedShader.Dispose();
        }
    }
}
