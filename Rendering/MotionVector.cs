using SiestaFrame.SceneManagement;
using Silk.NET.OpenGL;
using System;
using Unity.Mathematics;

namespace SiestaFrame.Rendering
{
    public class MotionVector : IDisposable
    {
        uint motionVector;

        int matrixModelLocation;
        int matrixViewLocation;
        int matrixProjectionLocation;
        int baseMapLocation;
        int tilingOffsetLocation;
        int alphaTest;
        int prevMatrixModelLocation;
        int prevMatrixViewLocation;
        int prevMatrixProjectionLocation;

        Shader shader;

        public unsafe MotionVector()
        {
            motionVector = GraphicsAPI.GL.GenTexture();
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, motionVector);
            GraphicsAPI.GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.RG32f, (uint)App.Instance.MainWindow.Width, (uint)App.Instance.MainWindow.Height, 0, PixelFormat.RG, PixelType.Float, null);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, 0);

            shader = SceneManager.AddCommonShader("MotionVectorVert.glsl", "MotionVectorFrag.glsl");

            matrixModelLocation = shader.GetUniformLocation("MatrixModel");
            matrixViewLocation = shader.GetUniformLocation("MatrixView");
            matrixProjectionLocation = shader.GetUniformLocation("MatrixProjection");
            baseMapLocation = shader.GetUniformLocation("_BaseMap");
            tilingOffsetLocation = shader.GetUniformLocation("_TilingOffset");
            alphaTest = shader.GetUniformLocation("_AlphaTest");
            prevMatrixModelLocation = shader.GetUniformLocation("PrevMatrixModel");
            prevMatrixViewLocation = shader.GetUniformLocation("PrevMatrixView");
            prevMatrixProjectionLocation = shader.GetUniformLocation("PrevMatrixProjection");
        }

        public void BindMotionVector(TextureUnit unit = TextureUnit.Texture0)
        {
            GraphicsAPI.GL.ActiveTexture(unit);
            GraphicsAPI.GL.BindTexture(TextureTarget.Texture2D, motionVector);
        }

        public unsafe void RenderMotionVector(Scene scene)
        {
            App.Instance.MainWindow.BindFrameBuffer(motionVector);
            GraphicsAPI.GL.ClearColor(0f, 0f, 0f, 0f);
            GraphicsAPI.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GraphicsAPI.GL.Enable(EnableCap.DepthTest);
            GraphicsAPI.GL.Viewport(0, 0, (uint)App.Instance.MainWindow.Width, (uint)App.Instance.MainWindow.Height);

            var viewMatrix = scene.MainCamera.ViewMatrix;
            var projectionMatrix = scene.MainCamera.JitterProjectionMatrix;

            for (int i = 0; i < scene.Entites.Count; i++)
            {
                var entity = scene.Entites[i];

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
                entity.Transform.PrevModelMatrix = modelMatrix;
            }
            GraphicsAPI.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            scene.MainCamera.PrevViewMatrix = viewMatrix;
            scene.MainCamera.PrevProjectionMatrix = projectionMatrix;
        }

        public void Dispose()
        {
            GraphicsAPI.GL.DeleteTexture(motionVector);
        }
    }
}
