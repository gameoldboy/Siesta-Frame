using SiestaFrame.Object;
using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class MotionVector : IDisposable
    {
        uint motionVector;

        Shader shader;
        Shader instancedShader;

        int matrixModelLocation;
        int matrixViewLocation;
        int matrixProjectionLocation;
        int baseColorLocation;
        int baseMapLocation;
        int tilingOffsetLocation;
        int alphaTestLocation;
        int prevMatrixModelLocation;
        int prevMatrixViewLocation;
        int prevMatrixProjectionLocation;
        int matrixViewInstancedLocation;
        int matrixProjectionInstancedLocation;
        int baseMapInstancedLocation;
        int alphaTestInstancedLocation;
        int prevMatrixViewInstancedLocation;
        int prevMatrixProjectionInstancedLocation;

        public MotionVector()
        {
            Alloc();

            shader = SceneManager.AddCommonShader("MotionVectorVert.glsl", "MotionVectorFrag.glsl");
            instancedShader = SceneManager.AddCommonShader("MotionVectorInstancedVert.glsl", "MotionVectorInstancedFrag.glsl");

            matrixModelLocation = shader.GetUniformLocation("MatrixModel");
            matrixViewLocation = shader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = shader.GetUniformLocation("MatrixProjection");
            baseColorLocation = shader.GetUniformLocation("_BaseColor");
            baseMapLocation = shader.GetUniformLocation("_BaseMap");
            tilingOffsetLocation = shader.GetUniformLocation("_TilingOffset");
            alphaTestLocation = shader.GetUniformLocation("_AlphaTest");
            prevMatrixModelLocation = shader.GetUniformLocation("PrevMatrixModel");
            prevMatrixViewLocation = shader.GetUniformLocation("PrevMatrixView");
            prevMatrixProjectionLocation = shader.GetUniformLocation("PrevMatrixProjection");
            matrixViewInstancedLocation = instancedShader.GetUniformLocation("MatrixView");
            matrixProjectionInstancedLocation = instancedShader.GetUniformLocation("MatrixProjection");
            baseMapInstancedLocation = instancedShader.GetUniformLocation("_BaseMap");
            alphaTestInstancedLocation = instancedShader.GetUniformLocation("_AlphaTest");
            prevMatrixViewInstancedLocation = instancedShader.GetUniformLocation("PrevMatrixView");
            prevMatrixProjectionInstancedLocation = instancedShader.GetUniformLocation("PrevMatrixProjection");
        }

        public unsafe void Alloc()
        {
            if (motionVector > 0)
            {
                GraphicsAPI.GL.DeleteTexture(motionVector);
            }
            motionVector = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, motionVector);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.RG32f, App.Instance.MainWindow.Width, App.Instance.MainWindow.Height, 0, PixelFormat.RG, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void BindMotionVector(TextureUnit unit = TextureUnit.Texture0)
        {
            GraphicsAPI.GL.ActiveTexture(unit);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, motionVector);
        }

        public unsafe void RenderMotionVector(Scene scene)
        {
            var width = App.Instance.MainWindow.Width;
            var height = App.Instance.MainWindow.Height;
            App.Instance.MainWindow.BindFrameBuffer(motionVector, App.Instance.MainWindow.TempDepthAttachment);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, width, height);

            var viewMatrix = scene.MainCamera.ViewMatrix;
            var projectionMatrix = scene.MainCamera.JitterProjectionMatrix;

            for (int i = 0; i < scene.Entites.Count; i++)
            {
                var entity = scene.Entites[i];
                if (entity.DrawType == Mesh.DrawType.Direct)
                {
                    var modelMatrix = entity.Transform.ModelMatrix;
                    for (int j = 0; j < entity.Meshes.Length; j++)
                    {
                        var mesh = entity.Meshes[j];
                        var material = entity.Materials[j % entity.Materials.Length];

                        mesh.VAO.Bind();
                        shader.Use();

                        shader.SetMatrix(matrixModelLocation, modelMatrix);
                        shader.SetMatrix(matrixViewLocation, viewMatrix);
                        shader.SetMatrix(matrixProjectionLocation, projectionMatrix);
                        shader.SetMatrix(prevMatrixModelLocation, entity.Transform.PrevModelMatrix);
                        shader.SetMatrix(prevMatrixViewLocation, scene.MainCamera.PrevViewMatrix);
                        shader.SetMatrix(prevMatrixProjectionLocation, scene.MainCamera.PrevProjectionMatrix);
                        shader.SetVector(baseColorLocation, material.BaseColor);
                        material.BaseMap.Bind(TextureUnit.Texture0);
                        shader.SetInt(baseMapLocation, 0);
                        shader.SetVector(tilingOffsetLocation, material.TilingOffset);
                        shader.SetBool(alphaTestLocation, material.Mode != Material.BlendMode.None ? true : false);
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
                    entity.Transform.PrevModelMatrix = modelMatrix;
                }
            }

            for (int i = 0; i < scene.InstancedList.Count; i++)
            {
                var entities = scene.InstancedList[i];
                var mesh = scene.InstancedDictionaryIndex[i];
                var material = entities[0].Materials[0];
                mesh.VAO.Bind();
                instancedShader.Use();
                instancedShader.SetMatrix(matrixViewInstancedLocation, viewMatrix);
                instancedShader.SetMatrix(matrixProjectionInstancedLocation, projectionMatrix);
                instancedShader.SetMatrix(prevMatrixViewInstancedLocation, scene.MainCamera.PrevViewMatrix);
                instancedShader.SetMatrix(prevMatrixProjectionInstancedLocation, scene.MainCamera.PrevProjectionMatrix);
                material.BaseMap.Bind(TextureUnit.Texture0);
                instancedShader.SetInt(baseMapInstancedLocation, 0);
                instancedShader.SetBool(alphaTestInstancedLocation, material.Mode != Material.BlendMode.None ? true : false);
                GraphicsAPI.GL.DrawElementsInstanced(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null, (uint)entities.Count);
                GraphicsAPI.GL.BindVertexArray(0);
                GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);
                GraphicsAPI.GL.UseProgram(0);
            }

            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            scene.MainCamera.PrevViewMatrix = viewMatrix;
            scene.MainCamera.PrevProjectionMatrix = projectionMatrix;
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteTexture(motionVector);
            shader.Dispose();
            instancedShader.Dispose();
        }
    }
}
